using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{
	class m_TimeBomb
	{
		static string[] colors = {
			"black",
			"blue",
			"brown",
			"green",
			"orange",
			"purple",
			"red",
			"turquoise",
			"violet",
			"white",
			"yellow"
		};

		class DisarmData
		{
			public string nick;
			public string color;
			public SucklessTimer timer;

			public DisarmData(string _nick, string _color, double interval)
			{
				nick = _nick;
				color = _color;
				timer = new SucklessTimer(interval);
				timer.Enabled = true;
				timer.Start();
			}
		}
		Dictionary<string, DisarmData> m_timers;
		Dictionary<string, SucklessTimer> m_cooldown;

		public m_TimeBomb()
		{
			m_timers = new Dictionary<string, DisarmData>();
			m_cooldown = new Dictionary<string, SucklessTimer>();

			Actions.OnUserSay += OnUserSay;
		}

		void BoomTimerElapsed(string channel)
		{
			var data = m_timers[channel];
			E.Say(channel, "BOOOM! " + data.nick + " died instantly.");

			var cooldown = new SucklessTimer(E.rand.Next(30, 70) * 1000.0);
			cooldown.Elapsed += delegate {
				m_cooldown.Remove(channel);
			};
			cooldown.Enabled = true;
			cooldown.Start();
			m_cooldown[channel] = cooldown;
			m_timers.Remove(channel);
		}

		void OnUserSay(string nick, ref Channel chan, string message,
				int length, ref string[] args)
		{
			if (chan.name[0] != '#')
				return;

			switch (args[0]) {
			case "$timebomb": {
					string channel = chan.name;
					if (m_timers.ContainsKey(channel)) {
						E.Say(channel, "Only one timebomb is allowed at a time.");
						return;
					}
					if (m_cooldown.ContainsKey(channel)) {
						E.Say(channel, "Field is too hot. Need to cooldown first. ("
						      + (int)(m_cooldown[channel].GetRemaining() / 1000.0) + "s)");
						return;
					}
					if (!chan.contains(args[1])) {
						E.Say(channel, nick + ": Unknown nickname '" + args[1] + "'");
						return;
					}

					// Take a random amount from "colors"
					string[] choices = new string[E.rand.Next(2, 5)];
					string choice_str = "";
					for (int i = 0; i < choices.Length; ++i) {
						choices[i] = colors[E.rand.Next(colors.Length)];
						// Format chat output
						choice_str += choices[i];
						if (i < choices.Length - 1)
							choice_str += ", ";
					}
					string color = choices[E.rand.Next(choices.Length)];
	
					var data = new DisarmData(args[1], color, E.rand.Next(30, 70) * 1000.0);
					data.timer.Elapsed += delegate {
						BoomTimerElapsed(channel);
					};
	
					m_timers[channel] = data;
					E.Say(channel, args[1] + ": Tick tick.. " + (int)(data.timer.Interval / 1000.0)
					      + "s until explosion. Try $cutwire <color> from one of these colors: " + choice_str);
				}
				break;
			case "$cutwire": {
					if (!m_timers.ContainsKey(chan.name)) {
						E.Say(chan.name, "There's no timebomb to disarm.");
						return;
					}
					var data = m_timers[chan.name];
					if (data.nick != nick) {
						E.Say(chan.name, nick + ": You may not help to disarm the bomb.");
						return;
					}
					args[1] = args[1].ToLower();
					int color_i = Array.IndexOf(colors, args[1]);

					if (color_i < 0) {
						E.Say(chan.name, nick + ": Unknown or missing wire color.");
						return;
					}

					if (data.color != colors[color_i]) {
						// Explode instantly
						BoomTimerElapsed(chan.name);
						return;
					}
					// Disarmed
					m_timers.Remove(chan.name);
					E.Say(chan.name, nick + ": You successfully disarmed the bomb.");
				}
				break;
			}
		}
	}

	class SucklessTimer : System.Timers.Timer
	{
		private DateTime m_due;
		public SucklessTimer(double interval) : base(interval)
		{
			m_due = DateTime.Now.AddMilliseconds(interval);
		}

		public double GetRemaining()
		{
			return (m_due - DateTime.Now).Milliseconds;
		}
	}
}