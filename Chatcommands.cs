using System;
using System.Collections.Generic;

namespace MAIN
{
	public class Chatcommand
	{
		public delegate void CmdAction(string nick, string message);
		CmdAction m_run;
		Dictionary<string, Chatcommand> m_subcommands;

		public Chatcommand()
		{
			m_subcommands = new Dictionary<string, Chatcommand>();
		}

		public Chatcommand(CmdAction action) : this()
		{
			m_run = action;
		}

		public Chatcommand Add(string subcommand)
		{
			if (m_subcommands.ContainsKey(subcommand))
				L.Log("Chatcommand::Register() overriding subcommand '" + subcommand + "'.");

			var cmd = new Chatcommand();
			m_subcommands[subcommand] = cmd;
			return cmd;
		}

		public void Add(string subcommand, CmdAction action)
		{
			Add(subcommand).m_run = action;
		}

		public void SetMain(CmdAction action)
		{
			m_run = action;
		}

		/// <returns>Whether the command was handled</returns>
		public bool Run(string nick, string message)
		{
			string cmd = GetNext(ref message);

			if (m_run != null && (cmd == null || m_subcommands.Count == 0)) {
				m_run(nick, message);
				return true;
			}

			if (cmd == null || !m_subcommands.ContainsKey(cmd))
				return false;

			return m_subcommands[cmd].Run(nick, message);
		}

		/// <returns>List of (sub)commands as string</returns>
		public string CommandsToString()
		{
			var commands = new List<string>();
			foreach (KeyValuePair<string, Chatcommand> sub_cmd in m_subcommands) {
				if (sub_cmd.Value.m_subcommands.Count > 0)
					commands.Add(sub_cmd.Key + " [+..]");
				else
					commands.Add(sub_cmd.Key);
			}
			return String.Join(", ", commands);
		}


		/// <summary>Trims 'message' by the next word</summary>
		/// <returns>The next word until 'delim'</returns>
		public static string GetNext(ref string message, char delim = ' ')
		{
			if (message.Length == 0)
				return "";

			int end_pos = message.IndexOf(delim);
			if (end_pos == -1)
				end_pos = message.Length;

			string part = message.Substring(0, end_pos);
			if (end_pos + 1 < message.Length)
				message = message.Substring(end_pos + 1);
			else
				message = "";

			return part.Trim();
		}

		public static string[] Split(string message)
		{
			return message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}