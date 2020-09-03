using System;
using System.Collections.Generic;
using Thread = System.Threading.Thread;

namespace MAIN
{
	// BASS Class for all modules
	public class Module
	{
		string m_name;
		protected Manager p_manager;

		public Module(string name, Manager manager)
		{
			m_name = name;
			p_manager = manager;
		}

		~Module()
		{
			CleanStage();
		}

		#region PUBLIC FUNCTIONS
		/// <returns>Module name</returns>
		public string GetName()
		{
			return m_name;
		}

		/// <summary>Called for warm restarts. Clear module cache here.</summary>
		public virtual void CleanStage() { }

		/// <param name="nick">Nickname of user</param>
		public virtual void OnUserJoin(string nick) { }

		/// <param name="nick">Nickname of user</param>
		public virtual void OnUserLeave(string nick) { }

		/// <param name="nick">New nickname</param>
		/// <param name="old_nick">Previous nickname</param>
		public virtual void OnUserRename(string nick, string old_nick) { }

		/// <param name="nick">Nickname of speaker</param>
		/// <param name="message">Entire chat message</param>
		/// <param name="length">Length of 'args'</param>
		/// <param name="args">Message splitted by words, value is never empty for
		/// [index < 'length']. This array has a length of at least 10.</param>
		public virtual void OnUserSay(string nick, string message,
				int length, ref string[] args) { }
		#endregion

		#region PROTECTED FUNCTIONS
		protected void Log(string what, bool error = false)
		{
			L.Log("[" + GetName() + "@" + p_manager.GetChannel().GetName() + "] " + what, error);
		}
		#endregion

		#region PRIVATE FUNCTIONS
		#endregion
	}

	public class UserData
	{
		public string hostmask;
		public Chatcommand cmd_scope;

		public UserData(string hostmask)
		{
			this.hostmask = hostmask;
			cmd_scope = null;
		}
	}

	public class Channel
	{
		readonly string m_name;
		public Dictionary<string, UserData> users;

		public Channel(string channel_name)
		{
			m_name = channel_name;
			users = new Dictionary<string, UserData>();
		}

		#region PUBLIC FUNCTIONS
		public string GetName()
		{
			return m_name;
		}

		public bool IsPrivate()
		{
			return IsPrivate(m_name);
		}

		public static bool IsPrivate(string channel_name)
		{
			return channel_name[0] != '#';
		}

		/// <returns>User class object or null (not found)</returns>
		public UserData GetUserData(string nickname)
		{
			return users.ContainsKey(nickname) ? users[nickname] : null;
		}

		/// <summary>Try to find a similar nickname within the channel.</summary>
		/// <param name="nickname">Nickname from erroneous user input</param>
		public string FindNickname(string nickname, bool use_levenshtein = true)
		{
			string nickname_l = nickname.ToLower().Trim();
			foreach (var user in users) {
				if (user.Key.ToLower() == nickname_l)
					return user.Key;

				if (!use_levenshtein)
					continue;

				double sensivity = Math.Min(user.Key.Length, nickname.Length) / 4.0;
				double distance = Utils.LevenshteinDistance(user.Key, nickname);

				if (distance <= sensivity)
					return user.Key;
			}
			return null;
		}

		public void Say(string text)
		{
			E.Say(m_name, text);
		}
		#endregion
	}

	// Each server gets its own manager
	public class Manager
	{
		readonly string m_name;
		List<Module> m_modules;
		List<Channel> m_channels;
		Chatcommand m_chatcommands;
		Dictionary<int, Channel> m_active_channel;
		Dictionary<string, int> m_userstatus_cache;

		public Manager(string name)
		{
			m_name = name;
			m_modules = new List<Module>();
			m_channels = new List<Channel>();
			m_chatcommands = new Chatcommand();
			// May also contain temporary channels
			m_active_channel = new Dictionary<int, Channel>();
			m_userstatus_cache = new Dictionary<string, int>();
		}

		public string GetName()
		{
			return m_name;
		}

		#region MODULES
		public void AddModule(Module module)
		{
			if (GetModule(module.GetName()) != null)
				return;

			m_modules.Add(module);
		}

		public void RenewModules()
		{
			if (m_modules.Count > 0) {
				foreach (Module module in m_modules)
					module.CleanStage();

				return;
			}

			// Add modules here
			m_modules.Add(new m_Builtin(this));
			m_modules.Add(new m_GitHub(this));
			m_modules.Add(new m_lGame(this));
			m_modules.Add(new m_Lua(this));
			m_modules.Add(new m_SuperiorUno(this));
			m_modules.Add(new m_Tell(this));
			m_modules.Add(new m_TimeBomb(this));
		}

		public Module GetModule(string name)
		{
			return m_modules.Find(item => item.GetName() == name);
		}

		public void ClearModules()
		{
			foreach (Module module in m_modules)
				module.CleanStage();

			m_modules.Clear();
		}
		#endregion

		#region CHANNELS
		public ref List<Channel> UnsafeGetChannels()
		{
			return ref m_channels;
		}

		public Channel GetChannel(string name)
		{
			var channel = m_channels.Find(item => item.GetName() == name);
			if (channel != null)
				return channel;

			// Recycle temporary channels
			foreach (var item in m_active_channel) {
				if (item.Value.GetName() == name)
					return item.Value;
			}
			return null;
		}

		/// <returns>The currently active channel</returns>
		public Channel GetChannel()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			Channel channel;
			if (m_active_channel.TryGetValue(tid, out channel))
				return channel;

			return null;
		}

		public void ClearChannels()
		{
			m_active_channel.Clear();
			m_channels.Clear();
		}

		public void QuitChannel()
		{
			Channel channel = GetChannel();
			if (channel == null)
				return;

			var nicks_copy = new List<string>(channel.users.Count);
			foreach (var user in channel.users)
				nicks_copy.Add(user.Key);

			foreach (string nick in nicks_copy)
				OnUserLeave(nick);

			// Clean up running threads assigned to this channel
			var proc = System.Diagnostics.Process.GetCurrentProcess();
			foreach (Thread th in proc.Threads) {
				Channel chan;
				if (th == null)
					continue;

				int tid = th.ManagedThreadId;
				if (!m_active_channel.TryGetValue(tid, out chan))
					continue;

				if (chan == channel) {
					if (!th.Join(500))
						th.Abort(); // Force
					m_active_channel.Remove(tid);
				}
			}
			m_channels.Remove(channel);
		}

		/// <summary>Changes the per-thread channel</summary>
		/// <param name="channel_name">Channel name: null clears garbage</param>
		public void SetActiveChannel(string channel_name)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (channel_name == null) {
				m_active_channel.Remove(tid);
				return;
			}

			Channel channel = GetChannel(channel_name);
			if (channel == null) {
				channel = new Channel(channel_name);
				if (!channel.IsPrivate()) {
					L.Log("Manager::SetActiveChannel channel '" + channel_name +
						"' not found! Creating dummy channel.");
				}
			}
			m_active_channel[tid] = channel;
		}

		/// <summary>Creates a new thread with exception handling</summary>
		public Thread Fork(string log_id, string log_trace, Action func)
		{
			Channel channel = GetChannel();
			var th = new Thread(delegate() {
				SetActiveChannel(channel.GetName());
				try {
					func();
				} catch (Exception e) {
					L.Dump("Thread " + log_id, log_trace + " @" + channel.GetName(), e.ToString());
				}
				SetActiveChannel(null);
			});
			th.Start();
			return th;
		}
		#endregion

		#region CHATCOMMANDS
		public Chatcommand GetChatcommand()
		{
			return m_chatcommands;
		}
		#endregion

		#region CALLBACKS
		public void OnUserJoin(string nick, string hostmask)
		{
			Channel chan = GetChannel();
			if (chan == null)
				return;

			chan.users[nick] = new UserData(hostmask);

			foreach (Module module in m_modules)
				module.OnUserJoin(nick);
		}

		public void OnUserLeave(string nick)
		{
			Channel chan = GetChannel();
			if (chan == null)
				return;

			foreach (Module module in m_modules)
				module.OnUserLeave(nick);

			chan.users.Remove(nick);
			m_userstatus_cache.Remove(nick);
		}

		public void OnUserRename(string nick, string hostmask, string old_nick)
		{
			foreach (Channel channel in m_channels) {
				UserData user = channel.users[old_nick];
				user.hostmask = hostmask;

				channel.users[nick] = user;
				channel.users.Remove(old_nick);
			}

			m_userstatus_cache.Remove(old_nick);
			foreach (Module module in m_modules)
				module.OnUserRename(nick, old_nick);
		}

		public void OnUserSay(string nick, string message,
				int length, ref string[] args)
		{
			foreach (Module module in m_modules)
				module.OnUserSay(nick, message, length, ref args);

			Channel chan = GetChannel();
			UserData user = chan != null ? chan.GetUserData(nick) : null;
			if (user == null)
				return; // Left channel or kicked player

			// Command shortcuts: "$uno p" becomes "$p" unless escaped using "$$"
			if (user.cmd_scope != null && message[1] != '$') {
				if (user.cmd_scope.Run(nick, message.Substring(1)))
					return;
			}

			if (message[1] == '$') {
				// Unescape
				message = message.Substring(1);
			}

			m_chatcommands.Run(nick, message);
		}
		#endregion

		/// <summary>
		/// Indicates the authentication level of the nick
		/// </summary>
		/// <returns>
		/// 3 = logged in
		/// 2 = recognized (ACCESS)
		/// 1 = not logged in
		/// 0 = no such account / error
		/// </returns>
		public int GetUserStatus(string nick)
		{
			if (!m_userstatus_cache.ContainsKey(nick)) {
				// Send information request
				// See also: Program.cs -> OnUserSay()
				if (Utils.isYes(G.settings["nickserv_acc"]) == 1)
					E.Say("NickServ", "ACC " + nick);
				else
					E.Say("NickServ", "STATUS " + nick);
			}

			int status = 0;

			for (int n = 0; n < 15; ++n) {
				if (m_userstatus_cache.TryGetValue(nick, out status))
					break;

				Thread.Sleep(100);
			}
			return status;
		}

		public void ReceivedUserStatus(string nick, int val)
		{
			m_userstatus_cache[nick] = val;
		}
	}
}
