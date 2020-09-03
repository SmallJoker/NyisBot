using System;

public class L
{
	static string filename = "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
	static System.IO.FileStream fs;
	static System.Threading.Mutex mt = new System.Threading.Mutex();

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
		if (fs == null) {
			fs = new System.IO.FileStream(filename,
				System.IO.FileMode.Append,
				System.IO.FileAccess.Write,
				System.IO.FileShare.Read);
		}

		mt.WaitOne();

		string time = DateTime.Now.ToString("T");
		WriteLine("[" + time + "] Dump start: " + function);
		if (trace != null && trace != "")
			WriteLine("### Trace: " + trace);
		WriteLine(content);
		WriteLine("### Dump end (" + content.Length + " characters)");
		fs.Flush();

		Console.WriteLine(content);
		mt.ReleaseMutex();
	}

	static void WriteLine(string text)
	{
		byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
		fs.Write(data, 0, data.Length);
		fs.WriteByte((byte)'\n');
	}
}

