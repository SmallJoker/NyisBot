using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{
	class m_Tell
	{
		const string TELL_TEXT_DB = "tell_text.db";

		// <to nick> <from nick> <datetime> <text>
		List<string[]> tell_text;
		string tell_last = "";
		bool tell_save_required = false;

		public m_Tell(bool is_restart)
		{
			if (!is_restart)
				TellLoad();

			E.OnPong += SaveIfRequired;
			E.OnUserSay += OnUserSay;
			E.OnExit += SaveIfRequired;
			E.OnUserJoin += delegate(string nick, string hostmask, string channel) {
				TellTell(nick, channel);
				TellSave(true);
			};
			E.OnUserRename += FindTellUser;
		}

		~m_Tell()
		{
			SaveIfRequired();
		}

		public void SaveIfRequired()
		{
			if (tell_save_required)
				TellSave(false);
		}

		public void OnChannelJoin(ref Channel chan)
		{
			foreach (KeyValuePair<string, string> nick in chan.nicks)
				TellTell(nick.Key, chan.name);

			TellSave(true);
		}

		public void FindTellUser(string nick, string hostmask, string old_nick)
		{
			foreach (Channel c in E.chans) {
				if (c == null || c.name[0] != '#')
					continue;
				foreach (KeyValuePair<string, string> user in c.nicks) {
					if (user.Key == nick) {
						TellTell(nick, c.name);
						return;
					}
				}
			}
		}

		void OnUserSay(string nick, ref Channel chan, string message,
			int length, ref string[] args)
		{
			if (args[0] != "$tell")
				return;

			if (length < 2) {
				E.Say(chan.name, nick + ": Expected arguments: <nick> <text ..>");
				return;
			}

			args[1] = args[1].ToLower();
			string str = "";
			for (int i = 2; i < length; i++) {
				if (i + 1 < length)
					str += args[i] + ' ';
				else
					str += args[i];
			}
			if (str.Length < 10) {
				E.Notice(nick, "Too short input text.");
				return;
			}
			if (str.Length > 250) {
				E.Notice(nick, "Too long input text.");
				return;
			}
			if (str == tell_last) {
				E.Notice(nick, "Text too repetitive.");
				return;
			}
			foreach (string[] tell_msg in tell_text) {
				if (tell_msg[3] == str &&
					TellSimilar(tell_msg[0], args[1])) {
					E.Notice(nick, "That message is already in the queue.");
					return;
				}
			}

			tell_last = str;

			List<int> in_channel = new List<int>();
			string user_normal = null;
			for (int i = 0; i < E.chans.Length; i++) {
				if (E.chans[i] != null && E.chans[i].name[0] == '#') {
					foreach (KeyValuePair<string, string> user in E.chans[i].nicks) {
						if (TellSimilar(user.Key.ToLower(), args[1])) {
							in_channel.Add(i);
							user_normal = user.Key;
							break;
						}
					}
				}
			}
			if (in_channel.Count > 0) {
				foreach (int c_id in in_channel) {
					if (E.chans[c_id].nicks.ContainsKey(nick)) {
						E.Notice(nick, "Found " + user_normal + " in channel " +
							E.chans[c_id].name + " no need to use $tell.");
						return;
					}
				}
				E.Say(E.chans[in_channel[0]].name, user_normal + ": TELL from " + nick + ": " + str);
				E.Notice(nick, "Message directly sent to " + user_normal + " in channel " + E.chans[in_channel[0]].name);
				return;
			}
			string date = DateTime.UtcNow.ToString("s");
			tell_text.Add(new string[] { args[1], nick, date, str });
			E.Say(chan.name, nick + ": meh okay. I'll look out for that user.");
			TellSave(true, true);
		}

		bool TellSimilar(string a, string b)
		{
			// xerox123 xeroxBot -> 3 (16 length)
			double sensivity = Math.Min(a.Length, b.Length) / 4.0;
			double distance = E.LevenshteinDistance(a, b);

			return distance <= sensivity;
		}
		void TellSave(bool check_power, bool needs_save = false)
		{
			if (needs_save)
				tell_save_required = true;

			if (check_power) {
				if (!E.HDDisON())
					return;
			}
			if (!tell_save_required)
				return;

			tell_save_required = false;

			System.IO.FileStream stream = new System.IO.FileStream(TELL_TEXT_DB, System.IO.FileMode.OpenOrCreate);
			System.IO.BinaryWriter wr = new System.IO.BinaryWriter(stream);

			wr.Write("TT01");
			byte[] buf;
			wr.Write((byte)4);

			foreach (string[] msg in tell_text) {
				for (int i = 0; i < msg.Length; i++) {
					buf = E.enc.GetBytes(msg[i]);
					wr.Write((byte)buf.Length);
					wr.Write(buf);
				}
			}
			wr.Write((byte)0);

			wr.Close();
			stream.Close();
		}

		void TellLoad()
		{
			tell_save_required = false;
			tell_text = new List<string[]>();
			if (!System.IO.File.Exists(TELL_TEXT_DB))
				return;

			System.IO.FileStream stream = new System.IO.FileStream(TELL_TEXT_DB, System.IO.FileMode.Open);
			System.IO.BinaryReader rd = new System.IO.BinaryReader(stream);

			if (rd.ReadString() != "TT01") {
				L.Log("m_Tell::TellLoad, invalid database magic", true);
				rd.Close();
				stream.Close();
				return;
			}
			int i = 0;
			int version = rd.ReadByte();

			if (version != 4) {
				L.Log("m_Tell::TellLoad, unsupported version: " + version, true);
				rd.Close();
				stream.Close();
				return;
			}

			int len;
			byte[] buf;
			string[] data = null;

			while (true) {
				if (i == 0)
					data = new string[4];

				len = rd.ReadByte();
				if (len == 0)
					break;
				buf = rd.ReadBytes(len);
				data[i] = E.enc.GetString(buf);

				i = (i + 1) % 4;
				if (i == 0)
					tell_text.Add(data);
			}

			rd.Close();
			stream.Close();
		}

		void TellTell(string nick, string channel)
		{
			bool any = false;
			string nick_l = nick.ToLower();

			for (int i = 0; i < tell_text.Count; i++) {
				string[] msg = tell_text[i];
				if (msg == null) {
					L.Log("m_Tell::TellTell, msg == null", true);
					any = true;
					continue;
				}

				if (nick_l == msg[0] || TellSimilar(nick_l, msg[0])) {
					E.Say(channel, nick + ": [UTC " + msg[2] + "] From " + msg[1] + ": " + msg[3]);
					Thread.Sleep(300);

					any = true;
					tell_text[i] = null;
				}
			}
			if (!any)
				return;

			tell_text.RemoveAll(item => item == null);
			tell_save_required = true;
		}
	}
}