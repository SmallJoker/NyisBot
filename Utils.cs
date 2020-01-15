using System;
using System.Threading;
using System.Collections.Generic;

namespace MAIN
{
	enum IRC_Color
	{
		WHITE, BLACK, BLUE, GREEN,
		RED, MAROON, PURPLE, ORANGE,
		YELLOW, LIGHT_GREEN, TEAL, CYAN,
		LIGHT_BLUE, MAGENTA, GRAY, LIGHT_GRAY
	};

	static class Utils
	{
		public static Random random = new Random((int)DateTime.UtcNow.Ticks);

		public static double toDouble(string inp)
		{
			double ret = 0;
			double.TryParse(inp, out ret);
			return ret;
		}

		public static int toInt(string inp)
		{
			int ret = -1;
			int.TryParse(inp, out ret);
			return ret;
		}

		public static sbyte isYes(string r)
		{
			switch (r.ToLower()) {
			case "yes":
			case "true":
			case "1":
			case "on":
			case "enable":
			case "allow":
				return 1;
			case "no":
			case "false":
			case "0":
			case "off":
			case "disable":
			case "disallow":
				return 0;
			}
			return -1;
		}

		public static void Shuffle<T>(ref List<T> list)
		{
			int n = list.Count;
			while (n > 1) {
				n--;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static object RandomIn(Array array)
		{
			return array.GetValue(random.Next(array.Length));
		}

		public static int LevenshteinDistance(string s, string t)
		{
			if (string.IsNullOrEmpty(s)) {
				if (string.IsNullOrEmpty(t))
					return 0;
				return t.Length;
			}

			if (string.IsNullOrEmpty(t))
				return s.Length;

			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];

			// initialize the top and right of the table to 0, 1, 2, ...
			for (int i = 0; i <= n; d[i, 0] = i++) ;
			for (int j = 1; j <= m; d[0, j] = j++) ;

			for (int i = 1; i <= n; i++) {
				for (int j = 1; j <= m; j++) {
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
					int min1 = d[i - 1, j] + 1;
					int min2 = d[i, j - 1] + 1;
					int min3 = d[i - 1, j - 1] + cost;
					d[i, j] = Math.Min(Math.Min(min1, min2), min3);
				}
			}
			return d[n, m];
		}

		public static string Colorize(string text, IRC_Color color)
		{
			int color_i = (int)color;
			string start = "" + (char)0x03;

			if (color_i < 10)
				start += '0' + color_i.ToString();
			else
				start += color_i.ToString();

			return start + text + (char)0x0F;
		}
	}

	class SucklessTimer : System.Timers.Timer
	{
		DateTime m_due;
		public new Action Elapsed;

		public SucklessTimer(double interval) : base()
		{
			base.Interval = interval;
			m_due = DateTime.UtcNow.AddMilliseconds(interval);
			base.Start();
			base.Elapsed += DummyElapsed;
		}

		public double GetRemaining()
		{
			return (m_due - DateTime.UtcNow).TotalMilliseconds;
		}

		void DummyElapsed(object source, System.Timers.ElapsedEventArgs e)
		{
			// Timer triggers too often. Check whether it's okay this time
			if (GetRemaining() > 100.0)
				return;

			Elapsed();
		}
	}
}