using System;
namespace MAIN
{
	public class m_Builtin : Module
	{
		public m_Builtin(Manager manager) : base("Builtin", manager)
		{
			p_manager.GetChatcommand().Add("$c", Cmd_c);
		}

		public override void OnUserSay(string nick, string message,
				int length, ref string[] args)
		{
			Channel channel = p_manager.GetChannel();
			string chan_name = channel.GetName();
			string hostmask = channel.GetHostmask(nick);

			switch (args[0]) {
			#region MISC
			case "$help":
				channel.Say(nick + ": $info, [$lua/$] <text../help()>, $rev <text..>, " +
					"$c <text..>, $tell <nick> <text..>, $lgame, $updghp. See also: " +
					"https://github.com/SmallJoker/NyisBot/blob/master/HELP.txt");
				break;
			case "$info":
			case "$about":
				channel.Say(nick + ": " + G.settings["identifier"]);
				break;
			case "$join":
				if (hostmask != G.settings["owner_hostmask"]) {
					channel.Say(nick + ": who are you?");
					return;
				}
				if (args[1] != "")
					E.Join(args[1]);
				break;
			case "$part":
				if (hostmask != G.settings["owner_hostmask"]) {
					channel.Say(nick + ": who are you?");
					return;
				}
				if (args[1] != "")
					E.Part(args[1]);
				break;
			#endregion
			}
		}

		void Cmd_c(string nick, string message)
		{
			var channel = p_manager.GetChannel();

			var colorized = new System.Text.StringBuilder(message.Length * 7);
			for (int i = 0; i < message.Length; i++) {
				colorized.Append((char)3);
				if ((i & 1) == 0)
					colorized.Append("04,09");
				else
					colorized.Append("09,04");
				colorized.Append(message[i]);
			}

			channel.Say(nick + ": " + colorized.ToString());
		}
	}
}
