using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

using System.Xml;
using OpenSSL;
using OpenSSL.SSL;
using OpenSSL.X509;

namespace MAIN
{
	class m_GitHub
	{
		Dictionary<string, List<string>> github_projects;
		DateTime github_updated;
		X509List github_certs = new X509List();
		X509Chain github_chain = new X509Chain();
		Thread github_thread;

		public m_GitHub()
		{
			github_updated = DateTime.Now;
			LoadGithubProjects();
			github_thread = new Thread(NewsFeedThread);
			github_thread.Start();
			E.OnUserSay += OnUserSay;
		}

		void OnUserSay(string nick, string hostmask, string channel, string message,
			int length, int channel_id, ref string[] args)
		{
			if (args[0] != "$updghp")
				return;

			if (hostmask != G.settings["owner_hostmask"]) {
				E.Say(channel, nick + ": who are you?");
				return;
			}
			LoadGithubProjects();
			E.Say(channel, nick + ": Updated! Got in total " + github_projects.Count + " Github projects to check.");
		}

		void LoadGithubProjects()
		{
			github_projects = new Dictionary<string, List<string>>();
			if (!System.IO.File.Exists("github_projects.txt")) {
				E.Log("GHPF: File 'github_projects.txt' not found!");
				return;
			}

			string[] lines = System.IO.File.ReadAllLines("github_projects.txt");

			for (int i = 0; i < lines.Length; i++) {
				string cur = lines[i];
				if (cur.Length < 10 || cur.IndexOf('/') == -1)
					continue;

				string[] data = cur.Split(' ', '\t');
				List<string> chans = new List<string>();

				for (int e = 1; e < data.Length; e++) {
					if (data[e].Length < 2 || data[e][0] != '#')
						continue;

					chans.Add(data[e]);
				}
				if (chans.Count > 0) {
					github_projects.Add(data[0], chans);
				} else {
					E.Log("GHPF: Project " + data[0] + " does not have any (valid) channels!");
				}
			}

			E.Log("GHPF: Found and added " + github_projects.Count + " projects!");
		}

		void NewsFeedThread()
		{
			E.Log("Github news feed started!");
			//github_updated -= new TimeSpan(2, 0, 0);
			while (E.running) {
				for (int i = 0; i < 900; i++) {
					if (!E.running)
						return;

					Thread.Sleep(1000);
				}

				foreach (KeyValuePair<string, List<string>> repo in github_projects) {
					if (!E.running)
						return;

					try {
						GetGithubCommits(repo);
					} catch (Exception ex) {
						E.Log("Failed to read feed " + repo.Key + " properly.");
						Console.WriteLine(ex.ToString());
						Thread.Sleep(2000);
					}
				}
				E.Log("Github news check completed.");
				github_updated = DateTime.Now;
			}
			E.Log("Github news feed stopped");
		}

		void GetGithubCommits(KeyValuePair<string, List<string>> repo)
		{
			string branch = "master";
			string[] repo_info = repo.Key.Split(':');
			if (repo_info.Length >= 2)
				branch = repo_info[1];

			#region Download
			TcpClient tc = new TcpClient("github.com", 443);
			NetworkStream stream = tc.GetStream();

			SslStream secureStream = new SslStream(stream, true,
				new RemoteCertificateValidationHandler(ValidateRemoteCertificate));

			secureStream.AuthenticateAsClient("github.com", github_certs, github_chain,
				SslProtocols.Tls, SslStrength.All, false);


			string content = null;
			RequestResponse(secureStream, "/" + repo_info[0] + "/commits/" + branch + ".atom", ref content);
			secureStream.Close();
			stream.Close();

			if (content == null)
				return;
			#endregion

			XmlDocument docwest = new XmlDocument();
			docwest.LoadXml(content);

			int count = 0;

			foreach (XmlNode node in docwest.GetElementsByTagName("entry")) {
				DateTime timestamp = Convert.ToDateTime(node["updated"].InnerText); // Including my timezone!
				if (timestamp < github_updated || count >= 2)
					break;

				string cappucino = node["id"].InnerText.Split('/')[1].Remove(6);
				string budspencer = node["title"].InnerText.Trim();
				string terencehill = node["author"]["name"].InnerText;
				if (budspencer.Length > 160)
					budspencer = budspencer.Remove(150) + " ...";

				string chucknorris = E.colorize(terencehill, 3)
					+ " @" + E.colorize(repo.Key.Split('/')[1], 5)
					+ ": " + E.colorize(budspencer, 14)
					+ " -> https://github.com/" + repo_info[0] + "/commit/" + cappucino;

				if (repo.Value == null) {
					for (int x = 0; x < E.chans.Length; x++)
						if (E.chans[x] != null && E.chans[x].name[0] == '#') {
							Thread.Sleep(200);
							E.Say(E.chans[x].name, chucknorris);
						}
				} else {
					foreach (string chan in repo.Value) {
						Thread.Sleep(200);
						E.Say(chan, chucknorris);
					}
				}
				count++;
			}
		}

		const string github_request_header =
@"GET {0} HTTP/1.1
Host: github.com
User-Agent: Mozilla/5.0 (NyisBot)
Referer: https://duckduckgo.com

";

		void RequestResponse(SslStream stream, string address, ref string content)
		{
			// Prepare header text
			string head_str = string.Format(github_request_header, address);

			// Write header to the network stream
			byte[] header_sent = E.enc.GetBytes(head_str);
			stream.Write(header_sent, 0, header_sent.Length);
			stream.Flush();

			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			int LEN = 1024;
			byte[] data = new byte[LEN];
			int pos = 0;
			int read = 0;
			while (true) {
				Thread.Sleep(read == LEN ? 50 : 500);
				read = stream.Read(data, 0, LEN);
				sb.Append(E.enc.GetString(data, 0, read));
				pos += read;
				if (read == 0)
					break;
			}
			data = null;

			int old_pos = 0;
			bool snap = false;

			// Stupid SSL adds random hashes
			for (pos = 1; pos < sb.Length - 2; pos++) {
				if (sb[pos] == '\r')
					continue;

				char next = sb[pos + 1];
				if (sb[pos - 1] == '\r' && sb[pos] == '\n'
					&& ((next >= '0' && next <= '9')
						|| (next >= 'a' && next <= 'f'))
					&& !snap) {

					snap = true;
					continue;
				}

				if (snap) {
					if (sb[pos] == '\n')
						snap = false;
				} else {
					if (old_pos != pos)
						sb[old_pos] = sb[pos];
					old_pos++;
				}
			}
			sb.Length = old_pos;
			content = sb.ToString();
			sb = null;

			int content_start = content.IndexOf("<?xml", 0, 2048);
			content = content.Substring(content_start);
		}

		bool ValidateRemoteCertificate(object sender,
			X509Certificate cert,
			X509Chain chain,
			int depth,
			VerifyResult result)
		{
			// If the certificate is a valid, signed certificate, return true.
			if (result == VerifyResult.X509_V_OK)
				return true;

			//Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
			//	cert.Subject, result.ToString());

			return true;
		}
	}
}