using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

using System.Xml;
using System.Net.Security;

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MAIN
{
	class m_GitHub
	{
		Dictionary<string, List<string>> github_projects;
		DateTime github_updated;
		X509CertificateCollection github_certs = new X509CertificateCollection();
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
				L.Log("m_GitHub::LoadGithubProjects, File 'github_projects.txt' not found");
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
					L.Log("m_GitHub::LoadGithubProjects, " + data[0] + " has no (valid) channels");
				}
			}

			L.Log("m_GitHub::LoadGithubProjects, entries = " + github_projects.Count);
		}

		void NewsFeedThread()
		{
			L.Log("m_GitHub::NewsFeedThread start");
			//github_updated -= new TimeSpan(2, 0, 0);
			E.running = true;
			while (E.running) {
				Thread.Sleep(2000);
				foreach (KeyValuePair<string, List<string>> repo in github_projects) {
					if (!E.running)
						return;

					try {
						GetGithubCommits(repo);
					} catch (Exception ex) {
						L.Dump("m_GitHub::NewsFeedThread", repo.Key, ex.ToString());
					}
				}
				L.Log("m_GitHub::NewsFeedThread check completed");
				github_updated = DateTime.Now;
				for (int i = 0; i < 60 * 5; i++) {
					if (!E.running)
						return;

					Thread.Sleep(1000);
				}
			}
			L.Log("m_GitHub::NewsFeedThread stop");
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

			SslStream secureStream = new SslStream(stream, true, ValidateRemoteCertificate);

			secureStream.AuthenticateAsClient("github.com", github_certs,
				SslProtocols.Tls, false);

			string url = "/" + repo_info[0] + "/commits/" + branch + ".atom";
			string content = RequestResponse(secureStream, url);
			secureStream.Close();
			stream.Close();

			if (content == null)
				return;
			#endregion

			XmlDocument docwest = new XmlDocument();
			try {
				docwest.LoadXml(content);
			} catch(Exception e) {
				L.Dump("m_GitHub::GetGithubCommits", url, e.Message + "\n" + e.ToString());
				return;
			}

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

";

		string RequestResponse(SslStream stream, string address)
		{
			// Prepare header text
			string head_str = string.Format(github_request_header, address);

			// Write header to the network stream
			byte[] header_sent = E.enc.GetBytes(head_str);
			stream.Write(header_sent, 0, header_sent.Length);
			stream.Flush();
			Thread.Sleep(500);

			System.IO.MemoryStream ms = new System.IO.MemoryStream();
			int r = 0;
			while (true) {
				r = stream.ReadByte();
				if (r == -1)
					break;
				ms.WriteByte((byte)r);
			}
			string content = E.enc.GetString(ms.ToArray());
			ms.Close();

			int content_start = content.IndexOf("<?xml", 0, Math.Min(content.Length, 4096));
			if (content_start < 0) {
				L.Log("m_GitHub::RequestResponse failed! " + address);
				return null;
			}
			int content_end = content.LastIndexOf('>', content.Length - 1);
			return content.Substring(content_start, content_end - content_start + 1);
		}

		bool ValidateRemoteCertificate(object sender,
			X509Certificate cert,
			X509Chain chain,
			SslPolicyErrors err)
		{
			// If the certificate is a valid, signed certificate, return true.
			if (err == SslPolicyErrors.None)
				return true;

			//Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
			//	cert.Subject, result.ToString());
			L.Log("m_GitHub::ValidateRemoteCertificate, '" + cert.Subject +
				"'. Error type: " + err.ToString());
			return true;
		}
	}
}