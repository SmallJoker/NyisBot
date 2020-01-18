#define LINUX

using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MAIN
{
	class E
	{
		#region Constants
		const string RANK_CHAR = "~&@%+";
		const int MSG_BUFFER = 1024;
		const int CHANNEL_MAX = 20;
		public static System.Text.Encoding enc = System.Text.Encoding.UTF8;
		#endregion

		#region Other variables
		static TcpClient cli;
		static System.Net.Security.SslStream stream;
		Thread parser;
		string address;
		int port;
		int last_ping;
		bool nickname_sent,
			identified,
			ready_sent;

		public static bool running;
		public static Manager manager;
		public static Action OnPong, OnBotReady;
		#endregion

		#region Init functions
		public E()
		{
			manager = new Manager();

			address = G.settings["address"].ToLower();
			port = Utils.toInt(G.settings["port"]);

			Initialize();
		}

		void Initialize()
		{
			manager.ClearChannels();
			manager.RenewModules();

			nickname_sent = false;
			identified = false;
			ready_sent = false;

			L.Log("E::Initialize, connecting to IP " + address + " & Port " + port);
			cli = new TcpClient(address, port);
			stream = new System.Net.Security.SslStream(cli.GetStream(), false);
			stream.AuthenticateAsClient(address);

			L.Log("E::Initialize, connected to server");

			OnBotReady += delegate() {
				ready_sent = true;
			};
		}

		// ASCII format 3 erase: 15
		void LoopThread()
		{
			byte[] chat_buffer = new byte[MSG_BUFFER];
			int buffer_used = 0;

			while (cli.Connected && running) {
				int free_chars = chat_buffer.Length - buffer_used;
				int rec_len = 0;
				if (cli.Available > 0 && free_chars > 0) {
					rec_len = stream.Read(chat_buffer, buffer_used, free_chars);
					buffer_used += rec_len;
				}

				Thread.Sleep(50);
				if (rec_len > 1)
					last_ping = 360; // Refill last ping

				while (true) {
					// Search for newline in buffer
					int found_line = Array.IndexOf(chat_buffer, (byte)'\n', 0, buffer_used);

					if (found_line == -1) {
						// Reset buffer, discard.
						if (buffer_used == chat_buffer.Length) {
							L.Log("E::LoopThread, Line too long, reset.", true);
							chat_buffer = new byte[MSG_BUFFER];
							buffer_used = 0;
						}
						break;
					}
					found_line++;

					// Add message to the chat buffer, remove '\n' and beginning ':'
					int offset = (chat_buffer[0] == ':') ? 1 : 0;
					int length = found_line - 2 - offset;

					string query = enc.GetString(chat_buffer, offset, length);

					// Shift back in array
					for (int i = 0; i < buffer_used - found_line; i++)
						chat_buffer[i] = chat_buffer[found_line + i];

					buffer_used -= found_line;

					try {
						FetchChat(query);
					} catch (Exception e) {
						Console.WriteLine(query);
						Console.WriteLine(e.ToString());
						L.Dump("E::FetchChat", query, e.ToString());
					}
					manager.SetActiveChannel(null);
				}
			}
			L.Log("E::LoopThread, Disconneted", true);
		}

		void TimeoutThread()
		{
			while (cli.Connected) {
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
			L.Log("E::Restart called");
			Initialize();
			Start();
		}

		public void Start()
		{
			running = true;
			last_ping = 360;

			parser = new Thread(LoopThread);
			parser.Start();

			new Thread(TimeoutThread).Start();
		}

		public bool Stop()
		{
			manager.ClearModules();

			if (!running)
				return false;

			running = false;

			L.Log("E::Stop called");
			send("QUIT :" + G.settings["identifier"]);
			Thread.Sleep(600);

			if (parser.ThreadState == ThreadState.Running)
				parser.Abort();

			Thread.Sleep(600);
			stream.Flush();
			stream.Close();
			cli.Close();

			manager.ClearChannels();
			return true;
		}
		#endregion

		void OnUserSay(string nick, string message)
		{
			if (message.Length < 4)
				return;

			#region CTCP
			string message_l = message.ToLower();
			if (message_l == "\x01version\x01") {
				Notice(nick, "\x01VERSION " + G.settings["identifier"] + "\0x01");
				L.Log("E::OnChatMessage, sending version to " + nick);
				return;
			}
			if (message_l == "\x01time\x01") {
				Notice(nick, "\x01TIME " + DateTime.UtcNow.ToString("s") + "\0x01");
				L.Log("E::OnChatMessage, sending time to " + nick);
				return;
			}
			#endregion

			Channel chan = manager.GetChannel();
			L.Log(chan.GetName() + "\t <" + nick + "> " + message);

			#region Args
			{
				string to_replace = G.settings["nickname"] + ": ";
				if (message.StartsWith(to_replace, StringComparison.OrdinalIgnoreCase)) {
					message = message.Substring(to_replace.Length).Trim();
					if (message.Length > 0 && message[0] != '$')
						message = '$' + message;
				}
			}

			string[] args = Chatcommand.Split(message);
			int length = args.Length;

			if (args.Length < 10)
				Array.Resize(ref args, 10);

			for (int i = length; i < args.Length; i++)
				args[i] = "";
			#endregion

			#region Handle NickServ + return
			if (nick == "NickServ" && length >= 3) {
				// NickServ can send different kinds of answers
				m_Lua module = (m_Lua)manager.GetModule("Lua");

				if (Utils.isYes(G.settings["nickserv_acc"]) == 1) {
					if (args[1] == "ACC" && module != null) {
						module.userstatus_queue[args[0]] = args[2][0] - '0';
						return;
					}
				} else {
					if (args[0] == "STATUS" && module != null) {
							module.userstatus_queue[args[1]] = args[2][0] - '0';
						return;
					}
				}
				// Add more here?

				return;
			}
			#endregion

			if (args[0] == ".bots") {
				chan.Say(G.settings["identifier"]);
				return;
			}

			if (message.Length < 4 || message[0] != '$')
				return;

			manager.OnUserSay(nick, message, length, ref args);
		}

		void OnServerMessage(string status, string destination, string content)
		{
			L.Log('[' + status + "] " + content);

			if (content == "*** Checking Ident")
				status = "001";

			switch (status) {
			case "001": // Welcome
			case "002": // Your host
			case "003": // Server creation date
			case "439": // ??
				NickAuth(G.settings["nickname"]);
				break;
			#region Nicklist
			case "353": {
					manager.SetActiveChannel(destination);
					Channel chan = manager.GetChannel();

					if (chan == null) {
						L.Log("E::OnServerMessage, Channel not added yet: " + destination);
						return;
					}

					for (int i = 0; i < content.Length; ++i) {
						int end_pos = content.IndexOf(' ', i);
						if (end_pos == -1)
							end_pos = content.Length;

						if (RANK_CHAR.Contains(content[i].ToString()))
							i++; // Skip rank characters

						string nick = content.Substring(i, end_pos - i);
						manager.OnUserJoin(nick, "?");

						i = end_pos;
					}
				}
				break;
			#endregion
			case "376": // End of MOTD
				if (G.settings["password"].Length <= 1 && !ready_sent) {
					OnBotReady(); // Join channels without identification
				}
				break;
			case "396": // Hostmask changed
				// Requires HostServ to be available and set up
				if (!ready_sent)
					OnBotReady();
				break;
			case "MODE":
				if (destination == G.settings["nickname"]) {
					int pw_len = G.settings["password"].Length;

					if (!identified) {
						if (pw_len > 1)
							Say("NickServ", "identify " + G.settings["password"]);
						identified = true;
					}

					if ((pw_len <= 1 || Utils.isYes(G.settings["hostserv"]) == 0)
							&& content == "+r" && !ready_sent)
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
			L.Log('[' + status + "] " + channel + "\t : " + nick);
			manager.SetActiveChannel(channel);

			if (status == "JOIN") {
				Channel chan = manager.GetChannel(channel);
				if (chan == null) {
					// In case it's the bot who joined
					chan = new Channel(channel);
					manager.UnsafeGetChannels().Add(chan);
				}
				manager.OnUserJoin(nick, hostmask);
				return;
			}

			if (status == "PART" || status == "KICK") {
				if (nick == G.settings["nickname"]) {
					// Bot leaves
					manager.QuitChannel(channel);
					return;
				}

				manager.OnUserLeave(nick);
				return;
			}

			if (status == "QUIT") {
				if (nick == G.settings["nickname"]) {
					Stop();
					return;
				}

				// Regular user leaves
				foreach (Channel chan in manager.UnsafeGetChannels()) {
					if (chan.GetHostmask(nick) == null)
						continue;

					manager.SetActiveChannel(chan.GetName());
					manager.OnUserLeave(nick);
				}
				return;
			}

			if (status == "NICK") {
				// User renamed
				manager.OnUserRename(channel, hostmask, nick);
				return;
			}
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

				L.Log("PONG to " + status);
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
					L.Log(">> PONG " + hostmask);
					return;
				}

				if ((status == "PRIVMSG" || status == "NOTICE")
						&& destination.ToUpper() != "AUTH"
						&& destination != "*"
						&& nick != "") {

					if (Channel.IsPrivate(destination))
						manager.SetActiveChannel(nick); // The tables have turned
					else
						manager.SetActiveChannel(destination);

					Channel channel = manager.GetChannel();
					channel.nicks[nick] = hostmask;

					OnUserSay(nick, message);
				} else { // numbers, AUTH request
					OnServerMessage(status, destination, message);
				}
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
					L.Log("FetchChat() " + line, true);
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

			if (status == "004" ||
				status == "005" ||
				status == "252" ||
				status == "254" ||
				status == "265" ||
				status == "266" ||
				status == "333") {
				
				// Ignore stuff that's not supported yet
				return;
			}

			Console.WriteLine(line);
		}

		#region MISC Functions
		public static void Join(string room)
		{
			send("JOIN " + room);
			L.Log("<< JOIN " + room);
		}

		public static void Part(string room)
		{
			send("PART " + room);
			L.Log("<< PART " + room);
			manager.QuitChannel(room);
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
			L.Log("NICK\t " + name);
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
			L.Log("KICK\t " + name);
		}

		public static void send(string s)
		{
			if (string.IsNullOrEmpty(s))
				return;

			if (!cli.Connected) {
				L.Log("E::send, can not send: disconnected", true);
				return;
			}

			try {
				stream.Write(enc.GetBytes(s + '\n'));
			} catch (Exception ex) {
				L.Dump("E::send", "", ex.ToString());
			}
		}
		#endregion
	}
}