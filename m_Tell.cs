using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{
	struct TellInfo
	{
		public string dst_nick, src_nick, datetime, text;

		public string[] Serialize()
		{
			return new string[] {
				dst_nick,
				src_nick,
				datetime,
				text
			};
		}

		public static TellInfo Deserialize(string[] data)
		{
			return new TellInfo {
				dst_nick = data[0],
				src_nick = data[1],
				datetime = data[2],
				text = data[3]
			};
		}
	}

	class m_Tell : Module
	{
		const string TELL_TEXT_DB = "tell_text.db";

		List<TellInfo> tell_text;
		string tell_last = "";
		bool tell_save_required = false;

		public m_Tell(Manager manager) : base("Tell", manager)
		{
			TellLoad();

			E.OnPong += delegate {
				TellSave(false);
			};

			p_manager.GetChatcommand().Add("$tell", Cmd_tell);
		}

		~m_Tell()
		{
			TellSave(false);
		}

		public override void CleanStage()
		{
			TellSave(false);
		}

		public override void OnUserJoin(string nick)
		{
			TellTell(nick, p_manager.GetChannel().GetName());
		}

		public override void OnUserRename(string nick, string old_nick)
		{
			foreach (Channel channel in p_manager.UnsafeGetChannels()) {
				if (channel.nicks.ContainsKey(nick)) {
					TellTell(nick, channel.GetName());
					return;
				}
			}
			// When everything else failed - user is apparently online
			TellTell(nick, nick);
		}

		void Cmd_tell(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			string dst_nick = Chatcommand.GetNext(ref message);

			if (dst_nick.Length < 3) {
				channel.Say(nick + ": Expected arguments: <nick> <text ..>");
				return;
			}

			if (message.Length < 7) {
				E.Notice(nick, "Too short input text.");
				return;
			}
			if (message.Length > 250) {
				E.Notice(nick, "Too long input text.");
				return;
			}
			if (message == tell_last) {
				E.Notice(nick, "Text too repetitive.");
				return;
			}
			foreach (TellInfo info in tell_text) {
				if (info.text == message && CheckSimilar(info.dst_nick, dst_nick)) {
					E.Notice(nick, "That message is already in the queue.");
					return;
				}
			}

			tell_last = message;

			var in_channel = new List<Channel>();
			string user_normal = null;
			foreach (Channel chan in p_manager.UnsafeGetChannels()) {
				string found = chan.FindNickname(dst_nick);
				if (found != null) {
					user_normal = found;
					in_channel.Add(chan);
				}
			}

			if (in_channel.Count > 0) {
				foreach (Channel chan in in_channel) {
					if (chan.GetHostmask(nick) != null) {
						E.Notice(nick, "Found " + user_normal + " in channel " +
							chan.GetName() + ". No need to use $tell.");
						return;
					}
				}
				in_channel[0].Say(user_normal + ": TELL from " + nick + ": " + message);
				E.Notice(nick, "Message directly sent to " + user_normal +
					" in channel " + in_channel[0].GetName() + ".");
				return;
			}

			tell_text.Add(new TellInfo {
				dst_nick = dst_nick,
				src_nick = nick,
				datetime = DateTime.UtcNow.ToString("s"),
				text = message
			});
			channel.Say(nick + ": meh okay. I'll look out for that user.");
			TellSave(true);
		}

		bool CheckSimilar(string a, string b)
		{
			// fooBAR0 fooBOT1 -> 3
			double sensivity = Math.Min(a.Length, b.Length) / 4.0;
			double distance = Utils.LevenshteinDistance(a, b);

			return distance <= sensivity;
		}

		void TellSave(bool mark_dirty)
		{
			tell_save_required |= mark_dirty;

			if (!tell_save_required)
				return;

			tell_save_required = false;

			System.IO.FileStream stream = new System.IO.FileStream(TELL_TEXT_DB, System.IO.FileMode.OpenOrCreate);
			System.IO.BinaryWriter wr = new System.IO.BinaryWriter(stream);

			wr.Write("TT01");
			byte[] buf;
			wr.Write((byte)4);

			foreach (TellInfo info in tell_text) {
				string[] data = info.Serialize();

				for (int i = 0; i < data.Length; i++) {
					buf = E.enc.GetBytes(data[i]);
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
			tell_text = new List<TellInfo>();
			if (!System.IO.File.Exists(TELL_TEXT_DB))
				return;

			System.IO.FileStream stream = new System.IO.FileStream(TELL_TEXT_DB, System.IO.FileMode.Open);
			System.IO.BinaryReader rd = new System.IO.BinaryReader(stream);

			if (rd.ReadString() != "TT01") {
				L.Log("m_Tell::TellLoad, invalid file magic", true);
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
			string[] data = new string[4];

			while (true) {
				len = rd.ReadByte();
				if (len == 0)
					break;
				buf = rd.ReadBytes(len);
				data[i] = E.enc.GetString(buf);

				i = (i + 1) % 4;
				if (i == 0)
					tell_text.Add(TellInfo.Deserialize(data));
			}

			rd.Close();
			stream.Close();
		}

		void TellTell(string nick, string channel)
		{
			int removed = 0;
			string nick_l = nick.ToLower();

			for (int i = 0; i < tell_text.Count; i++) {
				TellInfo info = tell_text[i];

				if (nick_l != info.dst_nick
						&& !CheckSimilar(nick_l, info.dst_nick))
					continue;

				// Nickname match
				L.Log("src= " + info.src_nick + ", dst= " + info.dst_nick);
				E.Say(channel, nick + ": [UTC " + info.datetime + "] From " +
					info.src_nick + ": " + info.text);
				Thread.Sleep(300);

				removed++;
				info.src_nick = null;
			}
			if (removed == 0)
				return;

			tell_text.RemoveAll(item => item.src_nick == null);
			tell_save_required = true;
		}
	}
}