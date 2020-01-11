using System;
namespace MAIN
{
	public class m_Builtin : Module
	{
		public m_Builtin(Manager manager) : base("Builtin", manager) { }

		public override void OnUserSay(string nick, string message,
				int length, ref string[] args)
		{
			Channel channel = p_manager.GetChannel();
			string chan_name = channel.GetName();
			string hostmask = channel.GetHostmask(nick);

			switch (args[0]) {
			#region MISC
			case "$help":
				if (args[1].ToLower() == "lgame") {
					channel.Say(nick + ": $ljoin, $lleave, $lstart, $ladd, $lcheck, $lcards. " +
						"You can find a tutorial in the help file.");
					return;
				}
				channel.Say(nick + ": $info, [$lua/$] <text../help()>, $rev <text..>, " +
					"$c <text..>, $tell <nick> <text..>, $help lgame, $updghp. See also: " +
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
			#region Reverse
			case "$rev": {
					if (length == 1) {
						channel.Say(nick + ": Expected arguments: <text ..>");
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

					channel.Say(nick + ": " + reversed);
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

					channel.Say(nick + ": " + colorized.ToString());
				}
				break;
				#endregion
			}
		}
	}
}
