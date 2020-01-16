using System;
using System.Collections.Generic;

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

	public class Channel
	{
		string m_name;
		// <nick, hostmask> (user init!)
		public Dictionary<string, string> nicks;

		public Channel(string channel_name)
		{
			m_name = channel_name;
			nicks = new Dictionary<string, string>();
		}

		#region PUBLIC FUNCTIONS
		public string GetName()
		{
			return m_name;
		}

		public bool IsPrivate()
		{
			return m_name[0] != '#';
		}

		/// <returns>Hostmask of the user or null (not found)</returns>
		public string GetHostmask(string nickname)
		{
			return nicks.ContainsKey(nickname) ? nicks[nickname] : null;
		}

		/// <summary>Try to find a similar nickname within the channel.</summary>
		/// <param name="nickname">Nickname from erroneous user input</param>
		public string FindNickname(string nickname, bool use_levenshtein = true)
		{
			string nickname_l = nickname.ToLower().Trim();
			foreach (KeyValuePair<string, string> user in nicks) {
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

	public class Manager
	{
		List<Module> m_modules;
		List<Channel> m_channels;
		Chatcommand m_chatcommands;
		string m_active_channel;

		public Manager()
		{
			m_modules = new List<Module>();
			m_channels = new List<Channel>();
			m_chatcommands = new Chatcommand();
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
			return m_channels.Find(item => item.GetName() == name);
		}

		/// <returns>The currently active channel</returns>
		public Channel GetChannel()
		{
			if (m_active_channel == null)
				return null;

			Channel channel = GetChannel(m_active_channel);
			if (channel == null) {
				channel = new Channel(m_active_channel);
				if (!channel.IsPrivate()) {
					L.Log("Manager::GetChannel() Channel not found: " + m_active_channel +
						". Creating temporary object.", true);
				}
			}
			// Temporary channel for private messages
			return channel != null ? channel : new Channel(m_active_channel);
		}

		public void ClearChannels()
		{
			m_channels.Clear();
		}

		public void QuitChannel(string channel_name)
		{
			Channel channel = GetChannel(channel_name);
			if (channel == null)
				return;

			var nicks_copy = new List<string>(channel.nicks.Count);
			foreach (KeyValuePair<string, string> user in channel.nicks)
				nicks_copy.Add(user.Key);

			foreach (string nick in nicks_copy)
				OnUserLeave(nick);

			m_channels.Remove(channel);
		}

		public void SetActiveChannel(string channel_name)
		{
			m_active_channel = channel_name;
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

			chan.nicks[nick] = hostmask;

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

			chan.nicks.Remove(nick);
		}

		public void OnUserRename(string nick, string hostmask, string old_nick)
		{
			foreach (Channel channel in m_channels) {
				channel.nicks[nick] = hostmask;
				channel.nicks.Remove(old_nick);
			}

			foreach (Module module in m_modules)
				module.OnUserRename(nick, old_nick);
		}

		public void OnUserSay(string nick, string message,
				int length, ref string[] args)
		{
			foreach (Module module in m_modules)
				module.OnUserSay(nick, message, length, ref args);

			m_chatcommands.Run(nick, message);
		}
		#endregion
	}
}
