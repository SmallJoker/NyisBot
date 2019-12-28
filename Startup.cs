#define LINUX

using System;
using System.Threading;
using System.Collections.Generic;

namespace MAIN
{
	public class G
	{
#if !LINUX
		[DllImport("kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

		private delegate bool EventHandler(int sig);
		static EventHandler _handler;
#endif

		public static Dictionary<string, string> settings;

		static void Main(string[] args)
		{
			if (!System.IO.File.Exists("config.example.txt")) {
				Console.WriteLine("[ERROR] File 'config.example.txt' not found - no future for this bot.");
				Console.WriteLine("Press any key to continue");
				Console.ReadKey(false);
				return;
			}

			settings = new Dictionary<string, string>();
			ReadConfig("config.example.txt");
			ReadConfig("config.txt");

			E e = new E();

			e.Start();
			E.OnBotReady += delegate () {
				string[] chans = settings["channels"].Split(' ');
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i].Length < 2 || chans[i][0] != '#')
						continue;

					e.Join(chans[i]);
				}
			};

#if !LINUX
			_handler += new EventHandler(e.Stop);
			SetConsoleCtrlHandler(_handler, true);
#endif

			while (true) {
				string str = Console.ReadLine();
				if (str == "q" ||
					str == "exit" ||
					str == "quit") {
					e.Stop();
					Thread.Sleep(1000);
					break;
				}
				E.send(str);

				Thread.Sleep(200);
			}
		}

		static void ReadConfig(string file)
		{
			if (!System.IO.File.Exists(file)) {
				Console.WriteLine("[ERROR] File '{0}' not found", file);
				return;
			}

			string text = System.IO.File.ReadAllText(file);
			bool read_key = true; // false = value
			bool snap = false;
			bool is_comment = false;
			int start_pos = 0;
			string key = "";

			for (int i = 0; i < text.Length; i++) {
				char cur = text[i];
				bool is_valid = false;

				if (read_key)
					is_valid = cur > ' ' && cur != '=';
				else
					is_valid = cur > ' ' || (cur == ' ' && snap);

				// Ignore comments
				if (i + 1 < text.Length && cur == '/' && text[i + 1] == '*') {
					is_comment = true;
					continue;
				}

				if (is_comment) {
					if (cur == '/' && text[i - 1] == '*')
						is_comment = false;
					continue;
				}

				if (!snap && is_valid) {
					// Begin reading
					start_pos = i;
					snap = true;
				}
				if (snap && !is_valid) {
					// Stop reading
					string val = text.Substring(start_pos, i - start_pos);

					if (read_key)
						key = val;
					else
						settings[key] = (val == ".") ? "" : val;

					read_key = true;
					snap = false;
				}
				if (read_key && cur == '=')
					read_key = false;
			}
			if (!read_key)
				settings[key] = text.Substring(start_pos, text.Length - start_pos);
		}
	}
}
