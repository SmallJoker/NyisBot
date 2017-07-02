//#define USE_ACC_FOR_NICKSERV
#define LINUX

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MAIN
{
	class G
	{
		#if !LINUX
		[DllImport("kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

		private delegate bool EventHandler(int sig);
		static EventHandler _handler;
		#endif

		public static Dictionary<string, string> settings;

		static void Main(string[] args)
		{
			if (!System.IO.File.Exists("config.example.txt")) {
				Console.WriteLine("[ERROR] File 'config.example.txt' not found - no future for this bot.");
				Console.WriteLine("Press any key to continue");
				Console.ReadKey(false);
				return;
			}

			settings = new Dictionary<string, string>();
			ReadConfig("config.example.txt");
			ReadConfig("config.txt");

			E e = new E();

			e.Start();
			e.OnBotReady += delegate() {
				string[] chans = settings["channels"].Split(' ');
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i].Length < 2 || chans[i][0] != '#')
						continue;

					e.Join(chans[i]);
				}
			};

			#if !LINUX
			_handler += new EventHandler(e.Stop);
			SetConsoleCtrlHandler(_handler, true);
			#endif

			while (true) {
				ConsoleKeyInfo i = Console.ReadKey(true);
				if (i.Key == ConsoleKey.Escape) {
					e.Stop();
					break;
				}
			}
		}

		static void ReadConfig(string file)
		{
			if (!System.IO.File.Exists(file)) {
				Console.WriteLine("[ERROR] File '{0}' not found", file);
				return;
			}

			string text = System.IO.File.ReadAllText(file);
			bool read_key = true; // false = value
			bool snap = false;
			bool is_comment = false;
			int start_pos = 0;
			string key = "";

			for (int i = 0; i < text.Length; i++) {
				char cur = text[i];
				bool is_valid = false;

				if (read_key)
					is_valid = cur > ' ' && cur != '=';
				else
					is_valid = cur > ' ' || (cur == ' ' && snap);

				// Ignore comments
				if (i + 1 < text.Length && cur == '/' && text[i + 1] == '*') {
					is_comment = true;
					continue;
				}

				if (is_comment) {
					if (cur == '/' && text[i - 1] == '*')
						is_comment = false;
					continue;
				}

				if (!snap && is_valid) {
					// Begin reading
					start_pos = i;
					snap = true;
				}
				if (snap && !is_valid) {
					// Stop reading
					string val = text.Substring(start_pos, i - start_pos);

					if (read_key)
						key = val;
					else
						settings[key] = (val == ".") ? "" : val;

					read_key = true;
					snap = false;
				}
				if (read_key && cur == '=')
					read_key = false;
			}
			if (!read_key)
				settings[key] = text.Substring(start_pos, text.Length - start_pos);
		}
	}

	class Channel
	{
		public string name;
		// <nick, hostmask> (user init!)
		public Dictionary<string, string> nicks;

		public Channel(string channel_name)
		{
			name = channel_name;
			nicks = new Dictionary<string, string>();
		}
	}

	class E
	{
		#region Constants
		const string RANK_CHAR = "~&@%+";
		const int MSG_BUFFER = 1024;
		const int CHANNEL_MAX = 20;
		public static bool running;
		#endregion

		m_Tell tell_module;
		m_lGame lgame_module;
		m_Lua lua_module;
		m_GitHub github_module;

		#region Other variables
		static Socket sk;
		Thread parser;
		string address;
		int port;
		int last_ping;
		bool nickname_sent,
			identified,
			ready_sent;

		public static System.Text.Encoding enc;
		public static Channel[] chans;
		static Random rand;
		#endregion

		#region Callbacks
		public Action OnBotReady;

		public delegate void OnUserDel(string nick, string hostmask, string channel);
		public static OnUserDel OnUserJoin;
		public static OnUserDel OnUserLeave;
		public delegate void OnUserRenameDel(string nick, string hostmask, string old_nick);
		public static OnUserRenameDel OnUserRename;
		public delegate void OnUserSayDel(string nick, string hostmask, string channel, string message,
				int length, int channel_id, ref string[] args);
		public static OnUserSayDel OnUserSay;

		public delegate void OnLoadDel(string nick, string hostmask, string old_nick);
		public static Action OnExit, OnPong;
		public delegate void OnChannelJoinDel(ref Channel chan);
		public static OnChannelJoinDel OnChannelJoin;
		#endregion

		#region Init functions
		public E()
		{
			enc = System.Text.Encoding.UTF8;
			rand = new Random((int)DateTime.UtcNow.Ticks);

			address = G.settings["address"].ToLower();
			port = getInt(G.settings["port"]);

			Initialize();
		}

		void Initialize()
		{
			chans = new Channel[CHANNEL_MAX];

			nickname_sent = false;
			identified = false;
			ready_sent = false;

			IPAddress ip = Dns.GetHostEntry(address).AddressList[0];
			Log("Connecting to IP " + ip.ToString() + " & Port " + port);

			sk = new Socket(
				ip.ToString().Contains(":") ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
				SocketType.Stream,
				ProtocolType.Tcp);

			sk.Connect(ip, port);
			Log("Connected to server");

			OnBotReady += delegate() {
				ready_sent = true;
			};
		}

		// ASCII format 3 erase: 15
		void LoopThread()
		{
			char[] chat_buffer = new char[MSG_BUFFER];
			int buffer_used = 0;

			while (sk.Connected && running) {
				int free_chars = chat_buffer.Length - buffer_used;
				bool short_sleep = sk.Available > MSG_BUFFER;

				#region Read incoming packets
				if (sk.Available > 1 && free_chars >= 100) {
					int buf_size = (free_chars < sk.Available) ? free_chars : sk.Available;
					byte[] buffer = new byte[buf_size];
					sk.Receive(buffer);
					char[] textReceived = enc.GetChars(buffer);

					// Filter packet into buffer
					int pos = 0;
					for (int i = 0; i < textReceived.Length; i++) {
						char cur = textReceived[i];
						if (cur == 0 || cur == '\r')
							continue;

						chat_buffer[buffer_used + pos] = cur;
						pos++;
					}
					buffer_used += pos;
					last_ping = 360; // Refill last ping
				}
				#endregion
				Thread.Sleep(short_sleep ? 20 : 100);

				// Search for newline in buffer
				int found_line = -1;
				for (int i = 0; i < buffer_used; i++) {
					if (chat_buffer[i] == '\n') {
						found_line = i + 1;
						break;
					}
				}

				if (found_line < 0) {
					if (buffer_used + 100 >= chat_buffer.Length) {
						Log("Line too long, reset.", true);
						chat_buffer = new char[MSG_BUFFER];
						buffer_used = 0;
					}
					continue;
				}

				// Add message to the chat buffer, remove '\n' and beginning ':'
				int offset = (chat_buffer[0] == ':') ? 1 : 0;

				char[] msg = new char[found_line - 1 - offset];
				for (int i = 0; i < msg.Length; i++)
					msg[i] = chat_buffer[i + offset];

				// Shift back in array
				for (int i = 0; i < buffer_used - found_line; i++)
					chat_buffer[i] = chat_buffer[found_line + i];

				buffer_used -= found_line;

				string query = new string(msg);
				try {
					FetchChat(query);
				} catch (Exception e) {
					Console.WriteLine(query);
					Console.WriteLine(e.ToString());
				}
			}
			Log("Disconneted", true);
		}

		void TimeoutThread()
		{
			while (sk.Connected) {
				Thread.Sleep(1000);
				if ((--last_ping) == 0) {
					running = false;
					Thread.Sleep(1000);
					new Thread(Restart).Start();
					return;
				}
			}
		}

		void Restart()
		{
			Log("Restarting client");
			Initialize();
			Start(true);
		}

		public void Start(bool is_restart = false)
		{
			running = true;
			last_ping = 360;

			tell_module = new m_Tell(is_restart);
			lgame_module = new m_lGame(is_restart);
			lua_module = new m_Lua();
			github_module = new m_GitHub();

			parser = new Thread(LoopThread);
			parser.Start();

			new Thread(TimeoutThread).Start();
		}

		public bool Stop(int _ = 0)
		{
			OnExit();

			if (!running)
				return false;

			running = false;

			Log("<< QUIT");
			send("QUIT :" + G.settings["identifier"]);
			Thread.Sleep(600);

			if (parser.ThreadState == ThreadState.Running)
				parser.Abort();

			Thread.Sleep(600);
			sk.Disconnect(false);

			for (int i = 0; i < chans.Length; i++)
				chans[i] = null;

			return false;
		}
		#endregion

		void OnChatMessage(string nick, string hostmask, string channel, string message)
		{
			if (message.Length < 4)
				return;

			#region HEAD
			#region CTCP
			string message_l = message.ToLower();
			if (message_l == "\x01version\x01") {
				Notice(nick, "\x01VERSION " + G.settings["identifier"] + "\0x01");
				Log(nick + "\t << VERSION");
				return;
			}
			if (message_l == "\x01time\x01") {
				Notice(nick, "\x01TIME " + DateTime.UtcNow.ToString("s") + "\0x01");
				Log(nick + "\t << TIME");
				return;
			}
			#endregion

			bool is_private = (channel[0] != '#');
			#region Args
			string[] args = message.Split(' ');
			int length = 0;

			for (int i = 0; i < args.Length; i++) {
				if (i != length)
					args[length] = args[i];

				if (args[i] != "")
					length++;
			}

			if (args.Length < 10)
				Array.Resize(ref args, 10);

			for (int i = length; i < args.Length; i++)
				args[i] = "";
			#endregion
			#region Get and update channel
			int channel_id = 0;
			if (!is_private) {
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i] != null && chans[i].name == channel) {
						channel_id = i;
						break;
					}
				}

				if (channel_id < 0) {
					Log("Error: Channel not added yet: " + channel);
					return;
				}

				chans[channel_id].nicks[nick] = hostmask;
			} else {
				channel = nick;
			}
			#endregion

			Log(channel + "\t <" + nick + "> " + message);
			#endregion

			#region Pending NickServ requests
			if (nick == "NickServ" && length >= 3) {
#if USE_ACC_FOR_NICKSERV
				if (args[1] == "ACC") {
					lua_module.userstatus_queue[args[0]] = args[2][0] - '0';
					return;
				}
#else
				if (args[0] == "STATUS") {
					lua_module.userstatus_queue[args[1]] = args[2][0] - '0';
					return;
				}
#endif
				return;
			}
			#endregion


			if (args[0] == ".bots") {
				Say(channel, G.settings["identifier"]);
				return;
			}

			#region CMD detection
			if (args[0][0] == '$') {
				// Do nothing :)
			} else if (length > 1 &&
				args[0].ToLower() == G.settings["nickname"].ToLower() + ":") {

				for (int i = 1; i < length; i++)
					args[i - 1] = args[i];
				length--;
				if (args[0][0] != '$')
					args[0] = '$' + args[0];
				args[length] = "";
			} else {
				return;
			}

			#endregion

			OnUserSay(nick, hostmask, channel, message, length, channel_id, ref args);

			switch (args[0]) {
			#region MISC
			case "$help":
				if (args[1].ToLower() == "lgame") {
					Say(channel, nick + ": $ljoin, $lleave, $lstart, $ladd, $lcheck, $lcards. " +
						"You can find a tutorial in the help file.");
					return;
				}
				Say(channel, nick + ": $info, [$lua/$] <text../help()>, $rev <text..>, " +
					"$c <text..>, $tell <nick> <text..>, $help lgame, $updghp. See also: " +
					"https://github.com/SmallJoker/NyisBot/blob/master/HELP.txt");
				break;
			case "$info":
			case "$about":
				Say(channel, nick + ": " + G.settings["identifier"]);
				break;
			case "$join":
				if (hostmask != G.settings["owner_hostmask"]) {
					Say(channel, nick + ": who are you?");
					return;
				}
				if (args[1] != "")
					Join(args[1]);
				break;
			case "$part":
				if (hostmask != G.settings["owner_hostmask"]) {
					Say(channel, nick + ": who are you?");
					return;
				}
				if (args[1] != "")
					Part(args[1]);
				break;
			#endregion
			#region Reverse
			case "$rev": {
					if (length == 1) {
						Say(channel, nick + ": Expected arguments: <text ..>");
						return;
					}

					string[] msg_parts = new string[length - 1];
					string special_chars = "<>()\\/[]{}";

					for (int p = 0; p < length - 1; p++) {
						char[] word = args[p + 1].ToCharArray();
						char[] revword = new char[word.Length];

						for (int i = 0; i < word.Length; i++) {
							char cur = word[word.Length - i - 1];

							if (char.IsUpper(word[i]))
								cur = char.ToUpper(cur);
							else
								cur = char.ToLower(cur);

							for (int r = 0; r < special_chars.Length; r += 2) {
								if (cur == special_chars[r]) {
									cur = special_chars[r + 1];
									break;
								} else if (cur == special_chars[r + 1]) {
									cur = special_chars[r];
									break;
								}
							}
							revword[i] = cur;
						}
						msg_parts[p] = new string(revword);
					}

					string reversed = "";
					foreach (string s in msg_parts)
						reversed += s + ' ';

					Say(channel, nick + ": " + reversed);
				}
				break;
			#endregion
			#region Colorize
			case "$c": {
					string str = "";
					for (int i = 1; i < length; i++) {
						if (i + 1 < length)
							str += args[i] + ' ';
						else
							str += args[i];
					}
					System.Text.StringBuilder colorized = new System.Text.StringBuilder(str.Length * 7);
					for (int i = 0; i < str.Length; i++) {
						colorized.Append((char)3);
						if ((i & 1) == 0)
							colorized.Append("04,09");
						else
							colorized.Append("09,04");
						colorized.Append(str[i]);
					}

					Say(channel, nick + ": " + colorized.ToString());
				}
				break;
			#endregion
			}
		}

		void OnServerMessage(string status, string destination, string content)
		{
			Log('[' + status + "] " + content);

			if (content.StartsWith("*** Checking Ident"))
				status = "001";

			switch (status) {
			#region Nicklist
			case "353": {
					int id = -1;
					for (int i = 0; i < chans.Length; i++) {
						if (chans[i] != null && chans[i].name == destination) {
							id = i;
							break;
						}
					}
					if (id < 0) {
						Log("Error: Channel not added yet: " + destination);
						return;
					}

					int pos = 0;
					while (pos < content.Length) {
						int end_pos = content.IndexOf(' ', pos);
						if (end_pos < pos)
							end_pos = content.Length;

						if (RANK_CHAR.Contains(content[pos].ToString()))
							pos++;

						string nick = content.Substring(pos, end_pos - pos);
						chans[id].nicks[nick] = "?";

						pos = end_pos + 1;

					}
					if (OnChannelJoin != null)
						OnChannelJoin(ref chans[id]);
				}
				break;
			#endregion
			case "376": // End of MOTD
				if (G.settings["password"].Length <= 1 && !ready_sent) {
					OnBotReady(); // Join channels without identification
				}
				break;
			case "396": // Hostmask changed
				if (!ready_sent)
					OnBotReady();
				break;
			case "001": // Welcome
			case "002": // Your host
			case "003": // Server creation date
			case "439": // ??
				NickAuth(G.settings["nickname"]);
				break;
			case "MODE":
				if (destination == G.settings["nickname"]) {
					int pw_len = G.settings["password"].Length;

					if (!identified) {
						if (pw_len > 1)
							Say("NickServ", "identify " + G.settings["password"]);
						identified = true;
					}
					if ((pw_len <= 1 || isYes(G.settings["hostserv"]) == 0)
							&& !ready_sent)
						OnBotReady();
				}
				break;
			case "INVITE":
				Join(content);
				break;
			}
		}

		void OnUserEvent(string nick, string hostmask, string status, string channel)
		{
			Log('[' + status + "] " + channel + "\t : " + nick);

			#region Join
			if (status == "JOIN") {
				int id = -1,
					free = -1;
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i] != null) {
						if (chans[i].name == channel) {
							id = i;
							break;
						}
					} else if (free < 0)
						free = i;
				}
				if (id < 0)
					id = free;

				if (chans[id] == null)
					chans[id] = new Channel(channel);

				chans[id].nicks[nick] = hostmask;
				if (OnUserJoin != null)
					OnUserJoin(nick, hostmask, channel);
				return;
			}
			#endregion
			#region Leave
			if (status == "PART" || status == "KICK") {
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i] != null &&
						chans[i].name == channel) {

						if (nick == G.settings["nickname"])
							chans[i] = null;
						else
							chans[i].nicks.Remove(nick);

						if (OnUserLeave != null)
							OnUserLeave(nick, hostmask, channel);
						return;
					}
				}
				Log("Unknown channel for " + status + ": " + channel, true);
				return;
			}
			#endregion
			#region Gone
			if (status == "NICK" || status == "QUIT") {
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i] != null) {
						if (chans[i].nicks.ContainsKey(nick)) {
							chans[i].nicks.Remove(nick);
							if (status == "NICK")
								chans[i].nicks[channel] = hostmask;
							else if (OnUserLeave != null)
								OnUserLeave(nick, hostmask, chans[i].name);
						}
					}
				}
				if (status == "NICK" && OnUserRename != null)
					OnUserRename(channel, hostmask, nick);
				return;
			}
			#endregion
		}

		// Status codes with text only
		string[] TEXT_STATUS = {
								"NOTICE", "PRIVMSG", "TOPIC", "INVITE", "MODE",
								"001", "002", "003", "251", "255",
								"372", "375", "376", "408", "439", "492"
								};

		void FetchChat(string line)
		{
			//<host> 353 <NICKNAME> = #nimg_lobby :<nick> @<nick> +<nick>
			string[] args = line.Split(new char[] { ' ' }, 6);

			#region Nickname and Hostmask
			int read_pos = args[0].IndexOf('!');

			string nick = "";
			if (read_pos > 0)
				nick = args[0].Substring(0, read_pos++);
			else
				read_pos = 0;

			string hostmask = args[0].Substring(read_pos);
			#endregion

			string status = args[1].ToUpper();

			#region Ping Pong
			if (hostmask == "PING") {
				status = status.ToLower();
				if (status[0] == ':')
					status = status.Substring(1);

				Log("PONG to " + status);
				send("PONG " + status);

				if (OnPong != null)
					OnPong();
				return;
			}
			if (hostmask == "PONG")
				return;
			#endregion

			if (status == "QUIT") {
				OnUserEvent(nick, hostmask, status, "");
				return;
			}

			// <nick!host> <status> <destination> ...
			string destination = args[2];

			#region JOIN NICK PART
			if (status == "JOIN" || status == "NICK" || status == "PART") {
				// <nick!host> PART <channel>
				// <nick!host> JOIN :<channel>

				if (destination[0] == ':')
					destination = destination.Substring(1);

				OnUserEvent(nick, hostmask, status, destination);
				return;
			}
			#endregion

			#region Normal Text-based
			// Check if it's possible to get the text right now
			bool is_text_only = false;
			for (int i = 0; i < TEXT_STATUS.Length; i++) {
				if (TEXT_STATUS[i] == status) {
					is_text_only = true;
					break;
				}
			}

			if (is_text_only) {
				// <nick!host> <status> <destination> :text text
				int min_start = args[0].Length + args[1].Length + args[2].Length + 3;

				read_pos = line.IndexOf(':', min_start) + 1;
				if (read_pos < 0) {
					Console.WriteLine(line);
					return;
				}

				string message = line.Substring(read_pos);

				if (status == "PONG") {
					Log(">> PONG " + hostmask);
					return;
				}

				if ((status == "PRIVMSG" || status == "NOTICE")
						&& destination.ToUpper() != "AUTH"
						&& destination != "*"
						&& nick != "")
					new Thread(delegate() {
						OnChatMessage(nick, hostmask, destination, message);
					}).Start();
				else // numbers, AUTH request
					OnServerMessage(status, destination, message);
				return;
			}
			#endregion

			#region User Events
			// Userlist
			if (status == "332" ||
				status == "353" ||
				status == "366" ||
				status == "396") {

				// <nick!host> 353 <destination> = <channel> :Bottybot figther212 @boots +Trivia foobar
				// <nick!host> <status> <destination> <channel> :Text text

				int min_start = 0;
				int text_start = (status == "353") ? 5 : 4;

				for (int i = 0; i < text_start; i++)
					min_start += args[i].Length + 1;

				read_pos = line.IndexOf(':', min_start) + 1;
				if (read_pos < 0) {
					Console.WriteLine(line);
					return;
				}

				string message = line.Substring(read_pos);

				OnServerMessage(status, args[text_start - 1], message);
				return;
			}

			if (status == "KICK") {
				// <nick!host> KICK <channel> <nick> :Kick reason

				OnUserEvent(args[3], "", status, destination);
				return;
			}
			#endregion

			if (status == "333" ||
				status == "252" ||
				status == "254" ||
				status == "265" ||
				status == "266") {
				
				// Ignore stuff that's not supported yet
				return;
			}

			Console.WriteLine(line);
		}

		#region MISC Functions
		public void Join(string room)
		{
			send("JOIN " + room);
			Log("<< JOIN " + room);
		}

		public void Part(string room)
		{
			send("PART " + room);
			Log("<< PART " + room);
		}

		void NickAuth(string name)
		{
			if (nickname_sent)
				return;

			if (name.Length < 3)
				name += "_user";
			else if (name.Length > 14)
				name = name.Remove(14);

			send("USER " + name + " 8 * :Testy bot");
			send("NICK " + name);
			Log("NICK\t " + name);
			nickname_sent = true;
		}

		public static void Say(string destination, string text)
		{
			send("PRIVMSG " + destination + " :" + text);
		}

		public static void Notice(string room_player, string text, bool raw = false)
		{
			if (raw) {
				send("NOTICE " + room_player + ' ' + text);
				return;
			}
			send("NOTICE " + room_player + " :" + text);
		}

		void Action(string room, string text)
		{
			send("ACTION " + room + " :" + text);
		}

		void Mode(string room, string name, string mode)
		{
			send("MODE " + room + ' ' + mode + ' ' + name);
		}

		void Kick(string room, string name)
		{
			send("KICK " + room + ' ' + name);
			Log("KICK\t " + name);
		}

		static void send(string s)
		{
			if (!sk.Connected) {
				Log("Tried to send packed - not connected.", true);
				return;
			}

			try {
				sk.Send(enc.GetBytes(s + '\n'));
			} catch (Exception ex) {
				Log(ex.Message, true);
			}
		}

		public static string colorize(string text, int color)
		{
			string start = "" + (char)0x03;

			if (color < 10)
				start += '0' + color.ToString();
			else
				start += color.ToString();

			return start + text + (char)0x0F;
		}

		double getDouble(string inp)
		{
			double ret = 0;
			double.TryParse(inp, out ret);
			return ret;
		}

		public static int getInt(string inp)
		{
			int ret = -1;
			int.TryParse(inp, out ret);
			return ret;
		}

		sbyte isYes(string r)
		{
			switch (r.ToLower()) {
			case "yes":
			case "true":
			case "1":
			case "on":
			case "enable":
			case "allow":
				return 1;
			case "no":
			case "false":
			case "0":
			case "off":
			case "disable":
			case "disallow":
				return 0;
			}
			return -1;
		}

		#endregion

		public static void Log(string s, bool error = false)
		{
			if (s == null)
				s = "Can not log NULL string.";

			byte[] s_raw = System.Text.Encoding.ASCII.GetBytes(s);

			System.Text.StringBuilder sb = new System.Text.StringBuilder(s_raw.Length);

			sb.Append(DateTime.Now.ToString("T"));
			sb.Append(' ');
			if (error)
				sb.Append("ERROR: ");

			for (int i = 0; i < s.Length; i++) {
				byte cur = s_raw[i];
				if (cur < 32 && cur != 9) {
					sb.Append('{');
					sb.Append(cur);
					sb.Append('}');
				} else {
					sb.Append((char)cur);
				}
			}

			Console.WriteLine(sb.ToString());
		}

		[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
		private extern static bool GetDevicePowerState(IntPtr hDevice, out bool fOn);

		public static bool HDDisON()
		{
			System.IO.FileStream[] files = System.Reflection.Assembly.
				GetExecutingAssembly().GetFiles();

			if (files.Length > 0) {
				IntPtr hFile = files[0].SafeFileHandle.DangerousGetHandle();
				bool is_on = false;
				bool result = GetDevicePowerState(hFile, out is_on);
				return result && is_on;
			}
			return false;
		}

		public static void Shuffle<T>(ref List<T> list)
		{
			int n = list.Count;
			while (n > 1) {
				n--;
				int k = rand.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static int LevenshteinDistance(string s, string t)
		{
			if (string.IsNullOrEmpty(s)) {
				if (string.IsNullOrEmpty(t))
					return 0;
				return t.Length;
			}

			if (string.IsNullOrEmpty(t))
				return s.Length;

			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];

			// initialize the top and right of the table to 0, 1, 2, ...
			for (int i = 0; i <= n; d[i, 0] = i++) ;
			for (int j = 1; j <= m; d[0, j] = j++) ;

			for (int i = 1; i <= n; i++) {
				for (int j = 1; j <= m; j++) {
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
					int min1 = d[i - 1, j] + 1;
					int min2 = d[i, j - 1] + 1;
					int min3 = d[i - 1, j - 1] + cost;
					d[i, j] = Math.Min(Math.Min(min1, min2), min3);
				}
			}
			return d[n, m];
		}
	}
}