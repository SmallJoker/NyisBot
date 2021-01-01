using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MAIN {
	public class SettingType {
		public virtual bool DeSerialize(string str)
		{
			// "str" is never null
			return false;
		}

		public virtual string Serialize()
		{
			return "<invalid>";
		}
	}

	public class Settings {
		readonly string m_file;
		Dictionary<string, string> m_settings;
		HashSet<string> m_modified;
		Settings m_parent;
		string m_prefix; // To access only certain settings

		public Settings(string filename, Settings parent = null, string prefix = "")
		{
			m_file = filename;
			m_settings = new Dictionary<string, string>();
			m_modified = new HashSet<string>();
			m_parent = parent;
			if (prefix.Length > 0)
				m_prefix = prefix + ".";
			else
				m_prefix = "";
		}

		public string this[string key]
		{
			get { return Get(key); }
			set { Set(key, value); }
		}

		public string Get(string key)
		{
			string ret;
			if (m_settings.TryGetValue(m_prefix + key, out ret))
				return ret;
			if (m_parent != null)
				return m_parent.Get(key);
			return ret;
		}

		public bool Get<T>(string key, ref T dst) where T : SettingType
		{
			string str = Get(key);
			if (str == null)
				return false;
			return dst.DeSerialize(str);
		}

		public IEnumerable<string> IterateKeys()
		{
			foreach (KeyValuePair<string, string> kv in m_settings) {
				if (kv.Key.StartsWith(m_prefix))
					yield return kv.Key.Substring(m_prefix.Length);
			}
		}

		public void Set(string key, string value)
		{
			key = m_prefix + key;
			// Key must not contain a space-like character!

			if (value != null)
				m_settings[key] = value;
			else
				m_settings.Remove(key);

			m_modified.Add(key);
		}

		public void Set(string key, SettingType value)
		{
			Set(key, value.Serialize());
		}

		// Sync with the settings file
		//  - Write changes of modified settings
		//  - Read in changes of unmodified settings
		//  - Copy & paste text between modified settings
		// Format "key = value". Obligatory space before '='
		public bool SyncFileContents()
		{
			if (!System.IO.File.Exists(m_file)) {
				Console.WriteLine("[ERROR] File '{0}' not found", m_file);
				return false;
			}

			StringBuilder sb = m_modified.Count > 0 ?
				new StringBuilder() : null;
			string text = System.IO.File.ReadAllText(m_file);
			int last_pos = 0;
			bool is_comment = false;

			for (int i = 0; i < text.Length; i++) {
				char cur = text[i];
				if (cur <= ' ')
					continue; // Ignore tailing spaces and controls

				// Start of comment "/*"
				if (i + 1 < text.Length && cur == '/' && text[i + 1] == '*') {
					is_comment = true;
					continue;
				}

				if (is_comment) {
					// Ignore all until "*/"
					if (cur == '/' && text[i - 1] == '*')
						is_comment = false;
					continue;
				}

				int line_end = text.IndexOfAny(new char[] { '\r', '\n' }, i);
				if (line_end < 0) {
					// Propery terminate last line
					text += Environment.NewLine;
					line_end = text.Length;
				}

				Match match = Regex.Match(
					text.Substring(i, line_end - i),
					@"^\s*(\S+)\s+=(.*)");

				if (match.Success) {
					string key = match.Groups[1].Value;
					if (m_modified.Contains(key)) {
						// Write change to file
						sb.Append(text.Substring(last_pos, i - last_pos));
						sb.AppendLine(key + " = " + m_settings[key]);

						m_modified.Remove(key);
						last_pos = line_end + 1;
					} else if (key.StartsWith(m_prefix)) {
						// Read change from file if prefix matches
						string val = match.Groups[2].Value.Trim();
						m_settings[key] = val;
					}
				} else {
					Console.WriteLine("Invalid config at pos=" + i + " len=" + (line_end - i));
				}

				// Line parsed, jump to next one
				i = line_end;
			}

			if (sb != null && last_pos < text.Length)
				sb.Append(text.Substring(last_pos, text.Length - last_pos));

			foreach (string key in m_modified)
				sb.AppendLine(key + " = " + m_settings[key]);

			m_modified.Clear();
			if (sb != null) {
				// Write back new file contents
				System.IO.File.WriteAllText(m_file, sb.ToString());
			}

			return true;
		}
	}
}
