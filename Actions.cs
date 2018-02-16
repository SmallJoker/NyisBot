using System;
using System.Threading;

namespace MAIN
{
	class Actions
	{
		#region BOT ACTIONS
		/// <param name="chan">Joined channel object</param>
		public delegate void OnChannelJoinDel(ref Channel chan);
		public static OnChannelJoinDel OnChannelJoin;
		public static Action OnExit, OnPong, OnBotReady;
		#endregion

		#region USER ACTIONS
		/// <param name="nick">Nickname of user</param>
		/// <param name="hostmask">Hostmask of user</param>
		/// <param name="channel">Affected channel name</param>
		public delegate void OnUserDel(string nick, string hostmask, string channel);

		/// <param name="nick">New nickname</param>
		/// <param name="hostmask">Hostmask of name changing user</param>
		/// <param name="old_nick">Previous nickname</param>
		public delegate void OnUserRenameDel(string nick, string hostmask, string old_nick);

		/// <param name="nick">Nickname of speaker</param>
		/// <param name="channel">Affected channel object. This is a temporary object in PMs.</param>
		/// <param name="message">Entire chat message</param>
		/// <param name="length">Length of 'args'</param>
		/// <param name="args">Message splitted by words, value is never empty for
		/// [index < 'length']. This array has a length of at least 10.</param>
		public delegate void OnUserSayDel(string nick, ref Channel chan, string message,
				int length, ref string[] args);

		public static OnUserDel OnUserJoin;
		public static OnUserDel OnUserLeave;
		public static OnUserRenameDel OnUserRename;
		public static OnUserSayDel OnUserSay;
		#endregion

		public static void ResetActions()
		{
			// Bot actions
			OnChannelJoin = delegate {};
			OnExit = delegate {};
			OnPong = delegate {};
			OnBotReady = delegate {};

			// User actions
			OnUserJoin = delegate {};
			OnUserLeave = delegate {};
			OnUserRename = delegate {};
			OnUserSay = delegate {};
		}
	}
}