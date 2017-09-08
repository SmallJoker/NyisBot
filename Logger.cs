using System;

public class L
{
	static string filename = "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
	static System.IO.TextWriter wr;
	static bool wr_locked = false;

	public static void Log(string s, bool error = false)
	{
		byte[] s_raw = System.Text.Encoding.ASCII.GetBytes(s);

		System.Text.StringBuilder sb = new System.Text.StringBuilder(s_raw.Length);

		sb.Append(DateTime.Now.ToString("T"));
		sb.Append(' ');
		if (error)
			sb.Append("ERROR: ");

		for (int i = 0; i < s.Length; i++) {
			byte cur = s_raw[i];
			if (cur < 32 && cur != 9) {
				sb.Append('{');
				sb.Append(cur);
				sb.Append('}');
			} else {
				sb.Append((char)cur);
			}
		}

		Console.WriteLine(sb.ToString());
	}

	public static void Dump(string function, string trace, string content)
	{
		if (wr == null)
			wr = new System.IO.StreamWriter(filename, true, MAIN.E.enc);

		while (wr_locked)
			System.Threading.Thread.Sleep(10);
	
		wr_locked = true;

		string time = DateTime.Now.ToString("T");
		wr.WriteLine("[" + time + "] Starting dump of: " + function);
		if (trace != null && trace != "")
			wr.WriteLine("### Trace: " + trace);
		wr.WriteLine(content);
		wr.WriteLine("### Dump end (" + content.Length + " characters)");
		wr.Flush();

		wr_locked = false;
		Log(function + " failed", true);
	}
}

