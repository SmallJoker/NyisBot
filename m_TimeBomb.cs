using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{
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
		}
	}

	class m_TimeBomb : Module
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

		Dictionary<string, DisarmData> m_timers;
		Dictionary<string, SucklessTimer> m_cooldown;

		public m_TimeBomb(Manager manager) : base("TimeBomb", manager)
		{
			m_timers = new Dictionary<string, DisarmData>();
			m_cooldown = new Dictionary<string, SucklessTimer>();
		}

		public override void CleanStage()
		{
			m_timers.Clear();
			m_cooldown.Clear();
		}

		void BoomTimerElapsed(string channel)
		{
			if (m_cooldown.ContainsKey(channel)) {
				L.Log("WARNING: Wanted to BOOM but the timer should already be deleted.");
				m_timers.Remove(channel);
				return;
			}
			// Maybe don't explode at all
			if (E.rand.Next(0, 100) >= 90) {
				m_timers.Remove(channel);
				return;
			}

			var data = m_timers[channel];
			data.timer.Stop();
			E.Say(channel, "BOOOM! " + data.nick + " died instantly.");

			var cooldown = new SucklessTimer(E.rand.Next(60, 90) * 1000.0);
			cooldown.Elapsed += delegate {
				m_cooldown.Remove(channel);
			};
			m_cooldown[channel] = cooldown;
			m_timers.Remove(channel);
		}

		public override void OnUserSay(string nick, string message,
				int length, ref string[] args)
		{
			Channel chan = p_manager.GetChannel();
			if (chan.IsPrivate())
				return;

			string channel = chan.GetName();

			switch (args[0]) {
			case "$timebomb": {
					if (m_timers.ContainsKey(channel)) {
						chan.Say("Only one timebomb is allowed at a time.");
						return;
					}
					if (m_cooldown.ContainsKey(channel)) {
						chan.Say("Field is too hot. Need to cooldown first. (" +
							(int)(m_cooldown[channel].GetRemaining() / 1000.0) + "s)");
						return;
					}

					string dst_name = chan.FindNickname(args[1]);
					if (dst_name == null) {
						chan.Say(nick + ": Unknown nickname '" + args[1] + "'");
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

					var data = new DisarmData(dst_name, color, E.rand.Next(50, 90) * 1000.0);
					data.timer.Elapsed += delegate {
						BoomTimerElapsed(channel);
					};

					m_timers[channel] = data;
					chan.Say(dst_name + ": Tick tick.. " + (int)(data.timer.Interval / 1000.0) +
						"s until explosion. Try $cutwire <color> from one of these colors: " + choice_str);
				}
				return;
			case "$cutewire":
					chan.Say(nick + ": Are you stupid or what? Try better next time.");
				return;
			case "$cutwire": {
					if (!m_timers.ContainsKey(channel)) {
						chan.Say("There's no timebomb to disarm.");
						return;
					}
					var data = m_timers[channel];
					if (data.nick != nick) {
						chan.Say(nick + ": You may not help to disarm the bomb.");
						return;
					}
					args[1] = args[1].ToLower();
					int color_i = Array.IndexOf(colors, args[1]);

					if (color_i < 0) {
						chan.Say(nick + ": Unknown or missing wire color.");
						return;
					}

					if (data.color != colors[color_i]) {
						// Explode instantly
						BoomTimerElapsed(channel);
						return;
					}
					// Disarmed
					m_timers.Remove(channel);
					chan.Say(nick + ": You successfully disarmed the bomb.");
				}
				return;
			}
		}
	}

	class SucklessTimer : System.Timers.Timer
	{
		private DateTime m_due;
		public SucklessTimer(double interval) : base()
		{
			base.Interval = interval;
			m_due = DateTime.Now.AddMilliseconds(interval);
			base.Start();
		}

		public double GetRemaining()
		{
			return (m_due - DateTime.Now).TotalMilliseconds;
		}
	}
}