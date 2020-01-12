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

	class m_lGame : Module
	{
		string[] CARD_TYPES = { "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
		List<LGameChannel> lchans;

		public m_lGame(Manager manager) : base("lGame", manager)
		{
			lchans = new List<LGameChannel>();

			var cmd = p_manager.GetChatcommand();
			cmd.Add("$ljoin", Cmd_join);
			cmd.Add("$lleave", Cmd_leave);
			cmd.Add("$lstart", Cmd_start);
			cmd.Add("$lcards", Cmd_cards);
			cmd.Add("$ladd", Cmd_add);
			cmd.Add("$lcheck", Cmd_check);
			cmd.Add("$lsetcards", Cmd_setcards);

			var sub = cmd.Add("$lgame");
			sub.SetMain(delegate (string nick, string message) {
				var channel = p_manager.GetChannel();
				channel.Say(nick + ": Available subcommands: " + sub.CommandsToString() +
					". Check out the GitHub repository for a game explanation.");
			});

			sub.Add("join", Cmd_join);
			sub.Add("leave", Cmd_leave);
			sub.Add("start", Cmd_start);
			sub.Add("cards", Cmd_cards);
			sub.Add("add", Cmd_add);
			sub.Add("check", Cmd_check);
			sub.Add("setcards", Cmd_setcards);
		}

		public override void CleanStage()
		{
			lchans.Clear();
		}

		public override void OnUserRename(string nick, string old_nick)
		{
			foreach (LGameChannel chan in lchans) {
				foreach (LGPlayer player in chan.lg_players) {
					if (player.nick == old_nick)
						player.nick = nick;
				}
			}
		}

		public override void OnUserLeave(string nick)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel c = GetLChannel(channel.GetName());

			if (c == null || !RemovePlayer(ref c, nick))
				return; // Player was not part of the round

			if (c.lg_players.Count < 3 && c.lg_running) {
				lchans.Remove(c);
				channel.Say("Game ended.");
			} else if (c.lg_running) {
				channel.Say(nick + " left the game. Next player: " + c.lg_players[c.lg_nick].nick);
			} else {
				channel.Say(nick + " left the game.");
			}
		}

		void Cmd_join(string nick, string message)
		{
			var chan = p_manager.GetChannel();
			string channel = chan.GetName();

			int player_i = FindPlayer(channel, nick);
			if (player_i >= 0) {
				E.Notice(nick, "You are already part of the game.");
				return;
			}

			LGameChannel c = GetLChannel(channel);
			if (c == null) {
				c = new LGameChannel(channel);
				lchans.Add(c);
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
			chan.Say(text);
		}

		void Cmd_leave(string nick, string message)
		{
			int player_i = FindPlayer(p_manager.GetChannel().GetName(), nick);
			if (player_i < 0) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}

			OnUserLeave(nick);
		}

		void Cmd_start(string nick, string message)
		{
			var chan = p_manager.GetChannel();
			string channel = chan.GetName();

			int player_i = FindPlayer(channel, nick);
			if (player_i < 0) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}
			LGameChannel c = GetLChannel(channel);

			if (c.lg_running) {
				E.Notice(nick, "The game is already running.");
				return;
			}

			int total_players = c.lg_players.Count;
			if (total_players < 3) {
				chan.Say(nick + ": At least 3 players are required to start the game.");
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
			chan.Say("Game started! Player " + c.lg_players[0].nick +
				" can play the first card using \"$ladd <'main card'> <card nr.> [<card nr.> [<card nr.>]]\"" +
				" (Card nr. from your hand)");

			CheckCards();
		}

		void Cmd_cards(string nick, string message)
		{
			string channel = p_manager.GetChannel().GetName();

			int player_i = FindPlayer(channel, nick);
			if (player_i < 0) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}
			LGameChannel c = GetLChannel(channel);

			E.Shuffle(ref c.lg_players[player_i].cards);
			E.Notice(nick, FormatCards(c.lg_players[player_i].cards, true));
		}

		void Cmd_add(string nick, string message)
		{
			var chan = p_manager.GetChannel();
			string channel = chan.GetName();
			string[] args = Chatcommand.Split(message);

			int player_index = FindPlayer(channel, nick);
			if (player_index < 0) {
				E.Notice(nick, "You are not part of the game. (yet?)");
				return;
			}

			LGameChannel c = GetLChannel(channel);

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

			if (args.Length < 2) {
				chan.Say(nick + ": Expected arguments <'main card'> <index> [<index> ..]" +
					"(Blue number: card value, Black number: index)");
				return;
			}

			string card = args[0];
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
					cards += CARD_TYPES[i] + " ";
			}

			// $lg <fake> <c1> <c2>
			if (main_card != "" && main_card != card) {
				chan.Say(nick + ": Wrong card type! Please pretend to place a card of type [" + main_card + "]!");
				return;
			}

			if (card_upper == "Q") {
				chan.Say(nick + ": The Queen is the bad card and can not be used as the main card of a stack.");
				return;
			}

			if (!valid_main_card) {
				chan.Say(nick + ": There is no such card type. Valid are: " + cards);
				return;
			}

			if (main_card == "")
				main_card = card;

			string[] card_mirror = c.lg_players[current_player].cards.ToArray();

			List<int> card_a = new List<int>();

			for (int i = 1; i < args.Length; i++) {
				int index = E.getInt(args[i]) - 1;
				if (index < 0 || index >= card_mirror.Length) {
					E.Notice(nick, "Invalid card index \"" + args[i] + "\". Play one between 1 and " +
						card_mirror.Length + " from your hand.");
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
				bool success = c.lg_players[current_player].cards.Remove(l_card);
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
			chan.Say("[LGame] Main card: [" + main_card + "]" +
				", Stack height: " + c.lg_stack.Count +
				", Next player: " + next_nick);
			Thread.Sleep(300);
			E.Notice(next_nick, FormatCards(c.lg_players[next_player].cards, true));

			CheckCards(nick);
		}

		void Cmd_check(string nick, string message)
		{
			var chan = p_manager.GetChannel();
			string channel = chan.GetName();

			#region Sanity check
			int checker_id = FindPlayer(channel, nick);
			if (checker_id < 0) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}

			LGameChannel c = GetLChannel(channel);

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
			#endregion

			string main_card = c.lg_card;
			bool contains_invalid = c.lg_stack_top.FindIndex(item => item != main_card) >= 0;

			string card_msg = "";
			if (contains_invalid) {
				card_msg = "One or more top cards were not a [" + main_card + "]. ";
			} else {
				card_msg = "The top cards were correct! ";

				current_player++;
				if (current_player >= c.lg_players.Count)
					current_player = 0;

				c.lg_nick = current_player;
			}

			// Add cards to previous player
			c.lg_players[WrapIndex(c, current_player - 1)]
				.cards.AddRange(c.lg_stack);
			// Clear stack
			c.lg_card = "";
			c.lg_stack.Clear();
			CheckCards();
			current_player = c.lg_nick; // Update current player index

			card_msg += "(" + FormatCards(c.lg_stack_top) + ") ";
			// "channel" reference is not updated after deleting the channel!
			if (GetLChannel(channel) == null) {
				chan.Say(card_msg);
				return;
			}

			int last_player = WrapIndex(c, current_player - 1);
			string nick_current = c.lg_players[current_player].nick;
			string nick_last = c.lg_players[last_player].nick;

			card_msg += "Complete stack goes to " + nick_last + ". " +
				 nick_current + " can start with an empty stack.";

			chan.Say(card_msg);
			E.Notice(nick_last, FormatCards(
					c.lg_players[last_player].cards,
					true
			));
			E.Notice(nick_current, FormatCards(c.lg_players[current_player].cards, true));
		}

		void Cmd_setcards(string nick, string message)
		{
			var chan = p_manager.GetChannel();
			string[] args = Chatcommand.Split(message);

			if (chan.nicks[nick] != G.settings["owner_hostmask"]) {
				chan.Say(nick + ": who are you?");
				return;
			}
			if (args.Length < 4) {
				chan.Say(nick + ": Too few cards! Add more.");
				return;
			}

			var lchan = lchans.Find(item => item.lg_running);
			if (lchan != null) {
				chan.Say(nick + ": Cannot change; wait for channel " +
					lchan.name + " to complete the game.");
				return;
			}
			CARD_TYPES = args;
			chan.Say(nick + ": Set the cards! Deck size is now " + args.Length * 4);
		}

		string FormatCards(List<string> cards, bool number = false)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int i = 1;
			foreach (string card in cards) {
				if (i > 1)
					sb.Append(' ');
				if (number) {
					sb.Append(i);
					sb.Append((char)3);
					if (card == "Q")
						sb.Append("04");
					else
						sb.Append("12");
				}

				sb.Append('[' + card + ']');
				if (number)
					sb.Append((char)15);
				i++;
			}
			return sb.ToString();
		}

		int FindPlayer(string channel, string nick)
		{
			for (int i = 0; i < lchans.Count; i++) {
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

		bool RemovePlayer(ref LGameChannel c, string nick)
		{
			int player_index = FindPlayer(c.name, nick);

			if (player_index < 0)
				return false;

			c.lg_players.RemoveAt(player_index);

			if (player_index <= c.lg_nick) {
				if (c.lg_nick == 0)
					c.lg_nick = c.lg_players.Count - 1;
				else
					c.lg_nick--;
			}
			return true;
		}

		void CheckCards(string nick = null)
		{
			Channel chan = p_manager.GetChannel();
			LGameChannel c = GetLChannel(chan.GetName());
			if (c == null || !c.lg_running)
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
					List<string> discarded = new List<string>();
					foreach (KeyValuePair<string, int> card in cards) {
						if (card.Value >= 4) {
							for (int x = 0; x < card.Value; x++)
								c.lg_players[i].cards.Remove(card.Key);

							discarded.Add(card.Key);
						}
					}
					if (discarded.Count > 0) {
						chan.Say(info.nick + " can discard four " + FormatCards(discarded) + " cards. Left cards: " +
							c.lg_players[i].cards.Count);
						Thread.Sleep(300);
					}
				}

				if (amount == 0) {
					if (nick == null || (nick != null && nick != info.nick)) {
						chan.Say(info.nick + " has no cards left. Congratulations, you're a winner!");
						player_remove.Add(info.nick);
					} else {
						chan.Say(info.nick + " played " + (char)3 + "07their last card" + (char)15 + "!");
					}
				} else if (amount <= 3) {
					if (nick != null && nick == info.nick) {
						chan.Say(info.nick + " has " + (char)3 + "07only "
							+ amount + " cards" + (char)15 + " left!");
					}
				}
			}

			foreach (string player in player_remove)
				OnUserLeave(player);
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

		LGameChannel GetLChannel(string name)
		{
			return lchans.Find(item => item.name == name);
		}
	}
}