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

		public static Settings settings;

		static void Main(string[] args)
		{
			if (!System.IO.File.Exists("config.example.txt")) {
				Console.WriteLine("[ERROR] File 'config.example.txt' not found - no future for this bot.");
				return;
			}

			Settings defaults = new Settings("config.example.txt");
			settings = new Settings("config.txt", defaults);
			defaults.SyncFileContents();
			settings.SyncFileContents();

			E e = new E();

			e.Start();
			E.OnBotReady += delegate () {
				string[] chans = settings["channels"].Split(' ');
				for (int i = 0; i < chans.Length; i++) {
					if (chans[i].Length < 2 || chans[i][0] != '#')
						continue;

					E.Join(chans[i]);
				}
			};

#if !LINUX
			_handler += new EventHandler(e.Stop);
			SetConsoleCtrlHandler(_handler, true);
#endif

			while (true) {
				string str = "";
				try {
					str = Console.ReadLine();
				} catch {
					L.Log("ReadLine() error. Mono sucks.", true);
				}

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
	}
}
