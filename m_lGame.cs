using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{

	class LGPlayer
	{
		public string nick;
		public List<string> cards;

		public LGPlayer(string _nick)
		{
			nick = _nick;
			cards = new List<string>();
		}
	}

	class LGameChannel
	{
		public string name;
		public bool lg_running;
		public string lg_card;
		public int lg_nick;
		public List<string> lg_stack, lg_stack_top;
		public List<LGPlayer> lg_players;

		public LGameChannel(string channel_name)
		{
			name = channel_name;
			lg_card = "";
			lg_nick = 0;
			lg_running = false;
			lg_stack = new List<string>();
			lg_stack_top = new List<string>();
			lg_players = new List<LGPlayer>();
		}
	}

	class m_lGame
	{
		string[] CARD_TYPES = { "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
		LGameChannel[] lchans;

		public m_lGame(bool is_restart)
		{
			lchans = new LGameChannel[E.chans.Length];
			E.OnUserSay += OnUserSay;
			E.OnUserLeave += OnUserLeave;
			E.OnUserRename += OnUserRename;
		}

		void OnUserSay(string nick, ref Channel chan, string message,
			int length, ref string[] args)
		{
			if (chan.name[0] != '#')
				return;
			string channel = chan.name;

			switch (args[0]) {
			#region L join
			case "$ljoin": {
					int player_i = FindPlayer(channel, nick);
					if (player_i >= 0) {
						E.Notice(nick, "You are already part of the game.");
						return;
					}

					LGameChannel c = getChannelId(channel);
					if (c == null) {
						for (int i = 0; i < lchans.Length; i++) {
							if (lchans[i] == null) {
								lchans[i] = new LGameChannel(channel);
								c = lchans[i];
								break;
							}
						}
					}

					if (c.lg_running) {
						E.Notice(nick, "You can not join a running game.");
						return;
					}

					c.lg_players.Add(new LGPlayer(nick));
					int count = c.lg_players.Count;
					string text = "Player " + nick + " joined the game. Total " + count + " players ready.";
					if (count > 2)
						text += " If you want to start the game, use \"$lstart\"";
					else if (count == 1)
						text += " At least 3 players are required to start the game.";
					E.Say(channel, text);
				}
				break;
			#endregion
			#region L leave
			case "$lleave": {
					int player_i = FindPlayer(channel, nick);
					if (player_i < 0) {
						E.Notice(nick, "You are not part of the game.");
						return;
					}

					OnUserLeave(nick, "", channel);
				}
				break;
			#endregion
			#region L start
			case "$lstart": {
					int player_i = FindPlayer(channel, nick);
					if (player_i < 0) {
						E.Notice(nick, "You are not part of the game.");
						return;
					}
					LGameChannel c = getChannelId(channel);

					if (c.lg_running) {
						E.Notice(nick, "The game is already running.");
						return;
					}

					int total_players = c.lg_players.Count;
					if (total_players < 3) {
						E.Say(channel, nick + ": At least 3 players are required to start the game.");
						return;
					}

					c.lg_running = true;
					List<string> cards = new List<string>();

					foreach (string card in CARD_TYPES) {
						int amount = (card == "Q") ? 3 : 4;
						while (amount > 0) {
							cards.Add(card);
							amount--;
						}
					}
					E.Shuffle(ref cards);

					int player = 0;
					foreach (string card in cards) {
						c.lg_players[player].cards.Add(card);

						player++;
						if (player >= total_players)
							player = 0;
					}

					foreach (LGPlayer n in c.lg_players) {
						E.Notice(n.nick, "Your cards: " + FormatCards(n.cards, true));
						Thread.Sleep(300);
					}
					c.lg_card = "";
					c.lg_nick = 0;
					c.lg_stack.Clear();
					E.Say(channel, "Game started! Player " + c.lg_players[0].nick +
						" can play the first card using \"$ladd <'main card'> <card nr.> [<card nr.> [<card nr.>]]\" (Card nr. from your hand)");

					CheckCards(channel);
				}
				break;
			#endregion
			#region L cards
			case "$lcards": {
					int player_i = FindPlayer(channel, nick);
					if (player_i < 0) {
						E.Notice(nick, "You are not part of the game.");
						return;
					}
					LGameChannel c = getChannelId(channel);

					E.Shuffle(ref c.lg_players[player_i].cards);
					E.Notice(nick, FormatCards(c.lg_players[player_i].cards, true));
				}
				break;
			#endregion
			#region L add
			case "$ladd": {
					int player_index = FindPlayer(channel, nick);
					if (player_index < 0) {
						E.Notice(nick, "You are not part of the game. (yet?)");
						return;
					}

					LGameChannel c = getChannelId(channel);

					if (!c.lg_running) {
						E.Notice(nick, "The game is not running yet. Use \"$lstart\"");
						return;
					}

					int current_player = c.lg_nick;
					if (player_index != current_player) {
						E.Notice(nick, "It's not your turn yet. Please wait for " +
							c.lg_players[current_player].nick);
						return;
					}

					if (length < 3) {
						E.Say(channel, nick + ": Expected arguments <'main card'> <index> [<index> ..]" +
							"(Blue number: card value, Black number: index)");
						return;
					}

					string card = args[1];
					string card_upper = card.ToUpper();
					string main_card = c.lg_card;

					// Check for valid card, correct name
					bool valid_main_card = false;
					string cards = "";
					for (int i = 0; i < CARD_TYPES.Length; i++) {
						string l_card = CARD_TYPES[i].ToUpper();
						if (l_card == card_upper) {
							valid_main_card = true;
							// Correct spelling
							card = CARD_TYPES[i];
							break;
						}
						if (l_card != "Q")
							cards += " " + CARD_TYPES[i];
					}

					// $lg <fake> <c1> <c2>
					if (main_card != "" && main_card != card) {
						E.Say(channel, nick + ": Wrong card type! Please pretend to place a card of type [" + main_card + "]!");
						return;
					}

					if (card_upper == "Q") {
						E.Say(channel, nick + ": The Queen is the bad card and can not be used as the main card of a stack.");
						return;
					}

					if (!valid_main_card) {
						E.Say(channel, nick + ": There is no such card type. Valid are: " + cards);
						return;
					}

					if (main_card == "")
						main_card = card;

					string[] card_mirror = c.lg_players[current_player].cards.ToArray();

					List<int> card_a = new List<int>();

					for (int i = 2; i < length; i++) {
						int index = E.getInt(args[i]) - 1;
						if (index < 0 || index >= card_mirror.Length) {
							E.Notice(nick, "Invalid card index \"" + args[i] + "\". Play one between 1 and " + card_mirror.Length + " from your hand.");
							return;
						}
						if (!card_a.Contains(index))
							card_a.Add(index);
					}

					c.lg_stack_top.Clear();

					foreach (int card_i in card_a) {
						string l_card = card_mirror[card_i];

						c.lg_stack.Add(l_card);
						c.lg_stack_top.Add(l_card);
						bool success = c.lg_players[current_player]
							.cards.Remove(l_card);
						if (!success)
							L.Log("m_lGame::$ladd, failed to remove cards", true);
					}

					int next_player = c.lg_nick;

					next_player++;
					if (next_player >= c.lg_players.Count)
						next_player = 0;
					c.lg_nick = next_player;
					c.lg_card = main_card;

					string next_nick = c.lg_players[next_player].nick;
					E.Say(channel, "[LGame] Main card: [" + main_card +
						"] Stack height: " + c.lg_stack.Count +
						" Next player: " + next_nick);
					Thread.Sleep(300);
					E.Notice(next_nick, FormatCards(c.lg_players[next_player].cards, true));

					CheckCards(channel, nick);
				}
				break;
			#endregion
			#region L Check
			case "$lcheck": {
					int checker_id = FindPlayer(channel, nick);
					if (checker_id < 0) {
						E.Notice(nick, "You are not part of the game.");
						return;
					}

					LGameChannel c = getChannelId(channel);

					if (!c.lg_running) {
						E.Notice(nick, "The game is not running yet. Use \"$lstart\"");
						return;
					}

					int current_player = c.lg_nick;
					if (checker_id != current_player) {
						E.Notice(nick, "It's not your turn yet. Please wait for " +
							c.lg_players[current_player].nick);
						return;
					}

					if (c.lg_card == "") {
						E.Notice(nick, "You can not check an empty stack.");
						return;
					}

					string main_card = c.lg_card;
					bool correct_cards = true;
					foreach (string s in c.lg_stack_top) {
						if (s != main_card) {
							correct_cards = false;
							break;
						}
					}

					string card_msg = "";
					if (correct_cards) {
						card_msg = "The top cards were correct! ";

						current_player++;
						if (current_player >= c.lg_players.Count)
							current_player = 0;

						c.lg_nick = current_player;
					} else {
						card_msg = "One or more top cards were not a [" + main_card + "]. ";
					}

					// Add cards to previous player
					c.lg_players[WrapIndex(c, current_player - 1)]
						.cards.AddRange(c.lg_stack);
					// Clear stack
					c.lg_card = "";
					c.lg_stack.Clear();
					CheckCards(channel);
					current_player = c.lg_nick; // Update current player index

					card_msg += "( " + FormatCards(c.lg_stack_top) + ") ";
					if (getChannelId(channel) != null) { // Reference is not updated after deleting the channel

						int last_player = WrapIndex(c, current_player - 1);
						string nick_current = c.lg_players[current_player].nick;
						string nick_last = c.lg_players[last_player].nick;

						card_msg += "Complete stack goes to " + nick_last + ". " +
							 nick_current + " can start with an empty stack.";

						E.Say(channel, card_msg);
						E.Notice(nick_last, FormatCards(
								c.lg_players[last_player].cards,
								true
						));
						E.Notice(nick_current, FormatCards(c.lg_players[current_player].cards, true));
					} else {
						E.Say(channel, card_msg);
					}
				}
				break;
			#endregion
			#region L set cards
			case "$lsetcards":
				if (chan.nicks[nick] != G.settings["owner_hostmask"]) {
					E.Say(channel, nick + ": who are you?");
					return;
				}
				if (length < 4) {
					E.Say(channel, nick + ": Too few cards! Add more :(");
					return;
				}
				for (int i = 0; i < lchans.Length; i++) {
					if (lchans[i] == null)
						continue;

					if (lchans[i].lg_running) {
						E.Say(channel, nick + ": Can not change, there's a running game in the channel " +
							E.chans[i].name + "!");
						return;
					}
				}
				CARD_TYPES = new string[length - 1];
				for (int i = 1; i < length; i++) {
					CARD_TYPES[i - 1] = args[i];
				}
				E.Say(channel, nick + ": OK. Done :)");
				break;
			#endregion
			}
		}

		void OnUserRename(string nick, string hostmask, string old_nick)
		{
			for (int i = 0; i < lchans.Length; i++) {
				if (lchans[i] == null)
					continue;

				for (int k = 0; k < lchans[i].lg_players.Count; k++) {
					if (lchans[i].lg_players[k].nick == old_nick) {
						lchans[i].lg_players[k].nick = nick;
						return;
					}
				}
			}
		}

		void OnUserLeave(string nick, string hostmask, string channel)
		{
			int chan_id = 0;
			for (; chan_id < lchans.Length; chan_id++) {
				if (lchans[chan_id] == null)
					continue;

				if (lchans[chan_id].name == channel)
					break;
			}

			if (chan_id == lchans.Length)
				return;

			LGameChannel c = lchans[chan_id];

			if (c.lg_players.Count <= 3 &&
				c.lg_running) {

				lchans[chan_id] = null;
				E.Say(channel, "Game ended.");
			} else {
				RemovePlayer(ref c, nick);
			}
		}

		string FormatCards(List<string> cards, bool number = false)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int i = 0;
			foreach (string card in cards) {
				if (number) {
					sb.Append(++i);
					sb.Append((char)3);
					if (card == "Q")
						sb.Append("04");
					else
						sb.Append("12");
				}
				sb.Append('[');
				sb.Append(card);
				sb.Append(']');
				if (number)
					sb.Append((char)15);
				sb.Append(' ');
			}
			return sb.ToString();
		}

		int FindPlayer(string channel, string nick)
		{
			for (int i = 0; i < lchans.Length; i++) {
				if (lchans[i] == null)
					continue;
				if (lchans[i].name != channel)
					continue;

				List<LGPlayer> players = lchans[i].lg_players;
				for (int k = 0; k < players.Count; k++) {
					if (players[k].nick == nick)
						return k;
				}
			}

			return -1;
		}

		void RemovePlayer(ref LGameChannel c, string nick)
		{
			int player_index = FindPlayer(c.name, nick);

			if (player_index < 0)
				return;

			c.lg_players.RemoveAt(player_index);

			if (player_index <= c.lg_nick) {
				if (c.lg_nick == 0)
					c.lg_nick = c.lg_players.Count - 1;
				else
					c.lg_nick--;
			}

			if (c.lg_running)
				E.Say(c.name, nick + " left the game. Next player: " + c.lg_players[c.lg_nick].nick);
			else
				E.Say(c.name, nick + " left the game.");
		}

		void CheckCards(string channel, string nick = null)
		{
			LGameChannel c = getChannelId(channel);
			if (c == null)
				return;

			List<string> player_remove = new List<string>();

			int max_len = c.lg_players.Count;
			for (int i = 0; i < max_len; i++) {
				LGPlayer info = c.lg_players[i];
				int amount = info.cards.Count;

				if (amount >= 4) {
					Dictionary<string, int> cards = new Dictionary<string, int>();
					// Count cards
					foreach (string card in info.cards) {
						if (cards.ContainsKey(card))
							cards[card]++;
						else
							cards.Add(card, 1);
					}
					// Discard pairs
					foreach (KeyValuePair<string, int> card in cards) {
						if (card.Value >= 4) {
							for (int x = 0; x < 4; x++)
								c.lg_players[i].cards.Remove(card.Key);

							E.Say(channel, info.nick + " can discard four [" + card.Key + "] cards. Left cards: " +
								c.lg_players[i].cards.Count);
							Thread.Sleep(300);
						}
					}
				}

				if (amount == 0) {
					if (nick == null || (nick != null && nick != info.nick)) {
						E.Say(channel, info.nick + " has no cards left. Congratulations, you're a winner.");
						player_remove.Add(info.nick);
					}
				} else if (amount <= 3) {
					if (nick != null && nick == info.nick) {
						E.Say(channel, info.nick + " has only " + amount + " cards left!");
					}
				}
			}

			foreach (string p in player_remove)
				OnUserLeave(p, "", channel);
		}

		int WrapIndex(LGameChannel c, int index)
		{
			int count = c.lg_players.Count;
			if (index < 0)
				index = count + index;
			else if (index >= count)
				index -= count;
			return index;
		}

		LGameChannel getChannelId(string name)
		{
			for (int i = 0; i < lchans.Length; i++) {
				if (lchans[i] == null)
					continue;

				if (lchans[i].name == name)
					return lchans[i];
			}

			return null;
		}
	}
}