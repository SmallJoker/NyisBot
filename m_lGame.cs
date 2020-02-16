using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{

	class LGPlayer
	{
		public string nick;
		public List<string> cards;
		UserData m_user;

		public LGPlayer(string nick, UserData user)
		{
			this.nick = nick;
			cards = new List<string>();
			m_user = user;
		}

		~LGPlayer()
		{
			//Console.WriteLine("~LGPlayer " + nick);
			m_user.cmd_scope = null;
		}
	}

	class LGameChannel
	{
		public string name;
		public bool is_active;
		public string main_card;
		public List<string> stack_all, stack_top;
		public List<LGPlayer> players;
		private int current_player;

		public LGameChannel(string channel_name)
		{
			name = channel_name;
			current_player = 0;
			is_active = false;
			players = new List<LGPlayer>();
			CleanStack();
		}

		public void CleanStack()
		{
			main_card = "";
			stack_all = new List<string>();
			stack_top = new List<string>();
		}

		public LGPlayer GetPlayer(string nickname)
		{
			return players.Find(item => item.nick == nickname);
		}

		public LGPlayer GetPlayer(int offset = 0)
		{
			int count = players.Count;
			offset += current_player + count;
			return players[offset % count];
		}

		public LGPlayer NextPlayer()
		{
			++current_player;
			current_player %= players.Count;
			return players[current_player];
		}

		public bool RemovePlayer(string nick)
		{
			int index = players.FindIndex(item => item.nick == nick);

			if (index < 0)
				return false;

			players.RemoveAt(index);
			GC.Collect();

			if (index <= current_player) {
				if (current_player == 0)
					current_player = players.Count - 1;
				else
					current_player--;
			}
			return true;
		}
	}

	class m_lGame : Module
	{
		string[] CARD_TYPES = { "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
		List<LGameChannel> lchans;
		Chatcommand m_subcommand;

		public m_lGame(Manager manager) : base("lGame", manager)
		{
			lchans = new List<LGameChannel>();

			var sub = p_manager.GetChatcommand().Add("$lgame");
			m_subcommand = sub;
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
				foreach (LGPlayer player in chan.players) {
					if (player.nick == old_nick)
						player.nick = nick;
				}
			}
		}

		public override void OnUserLeave(string nick)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());

			if (game == null || !game.RemovePlayer(nick))
				return; // Player was not part of the round

			if (game.players.Count < 3 && game.is_active) {
				lchans.Remove(game);
				game = null;
				GC.Collect();
				channel.Say("Game ended.");
				return;
			}
			if (game.is_active) {
				channel.Say(nick + " left the game. Next player: " + game.GetPlayer().nick);
				return;
			}
			channel.Say(nick + " left the game.");

			if (game.players.Count == 0) {
				lchans.Remove(game);
				game = null;
				GC.Collect(); // Force calling LGPlayer::~LGPlayer
			}
		}

		void Cmd_join(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());

			if (game != null && game.is_active) {
				channel.Say(nick + ": Please wait for " + game.GetPlayer() +
					" to finish their game.");
				return;
			}

			if (game != null && game.GetPlayer(nick) != null) {
				E.Notice(nick, "You already joined the game.");
				return;
			}

			if (game == null) {
				game = new LGameChannel(channel.GetName());
				lchans.Add(game);
			}

			UserData user = channel.GetUserData(nick);
			user.cmd_scope = m_subcommand;
			game.players.Add(new LGPlayer(nick, user));

			int count = game.players.Count;
			string text = "Player " + nick + " joined the game. Total " + count + " players ready.";
			if (count > 2)
				text += " If you want to start the game, use \"$start\"";
			else if (count == 1)
				text += " At least 3 players are required to start the game.";
			channel.Say(text);
		}

		void Cmd_leave(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());

			if (game == null || game.GetPlayer(nick) == null) {
				E.Notice(nick, "You are not part of a liar game.");
				return;
			}

			OnUserLeave(nick);
		}

		void Cmd_start(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());

			if (game == null || game.GetPlayer(nick) == null) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}

			if (game.is_active) {
				E.Notice(nick, "The game is already running.");
				return;
			}

			int total_players = game.players.Count;
			if (total_players < 3) {
				channel.Say(nick + ": At least 3 players are required to start the game.");
				return;
			}

			var cards = new List<string>();

			foreach (string card in CARD_TYPES) {
				int amount = (card == "Q") ? 3 : 4;
				while (amount > 0) {
					cards.Add(card);
					amount--;
				}
			}
			Utils.Shuffle(ref cards);

			foreach (string card in cards)
				game.NextPlayer().cards.Add(card);

			game.is_active = true;

			foreach (LGPlayer n in game.players) {
				E.Notice(n.nick, "Your cards: " + FormatCards(n.cards, true));
				Thread.Sleep(300);
			}
			game.CleanStack();

			channel.Say("Game started! Player " + game.GetPlayer().nick +
				" can play the first card using \"$add <'main card'> <card nr.> [<card nr.> [<card nr.>]]\"" +
				" (Card nr. from your hand)");

			CheckCards();
		}

		void Cmd_cards(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());

			if (game == null || !game.is_active) {
				E.Notice(nick, "There's no game or it did not start yet.");
				return;
			}

			LGPlayer player = game.GetPlayer(nick);
			if (player == null) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}

			Utils.Shuffle(ref player.cards);
			E.Notice(nick, FormatCards(player.cards, true));
		}

		void Cmd_add(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());
			LGPlayer player = game != null ? game.GetPlayer(nick) : null;

			#region Sanity check
			if (player == null || !game.is_active) {
				E.Notice(nick, "There's no game ongoing yet. Join & start to begin.");
				return;
			}

			if (player != game.GetPlayer()) {
				E.Notice(nick, "It's not your turn yet. Please wait for " +
					game.GetPlayer().nick);
				return;
			}
			#endregion

			string card = Chatcommand.GetNext(ref message);
			string card_upper = card.ToUpper();
			string main_card = game.main_card;

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
				channel.Say(nick + ": Wrong card type! Please pretend to place a card of type [" + main_card + "]!");
				return;
			}

			if (card_upper == "Q") {
				channel.Say(nick + ": The Queen is the bad card and can not be used as the main card of a stack.");
				return;
			}

			if (!valid_main_card) {
				channel.Say(nick + ": There is no such card type. Valid types: " + cards);
				return;
			}

			if (main_card == "")
				main_card = card;

			string[] card_mirror = player.cards.ToArray();

			var card_add = new List<int>();
			for (int n = 0; true; n++) {
				string index_s = Chatcommand.GetNext(ref message);
				if (index_s == "" && n == 0) {
					channel.Say(nick + ": Expected arguments <'main card'> <index> [<index> ..]" +
						"(Blue number: card value, Black number: index)");
					return;
				}
				if (index_s == "")
					break;

				int index_i = Utils.toInt(index_s) - 1;
				if (index_i < 0 || index_i >= card_mirror.Length) {
					E.Notice(nick, "Invalid card index \"" + index_s + "\". Play one between 1 and " +
						card_mirror.Length + " from your hand.");
					return;
				}

				if (!card_add.Contains(index_i))
					card_add.Add(index_i);
			}

			game.stack_top.Clear();
			foreach (int card_i in card_add) {
				string l_card = card_mirror[card_i];

				game.stack_all.Add(l_card);
				game.stack_top.Add(l_card);
				bool success = player.cards.Remove(l_card);
				if (!success)
					L.Log("m_lGame::$ladd, failed to remove cards", true);
			}

			game.main_card = main_card;
			player = game.NextPlayer();

			channel.Say("[LGame] Main card: [" + main_card + "]" +
				", Stack height: " + game.stack_all.Count +
				", Next player: " + player.nick);
			Thread.Sleep(300);
			E.Notice(player.nick, FormatCards(player.cards, true));

			CheckCards(nick);
		}

		void Cmd_check(string nick, string message)
		{
			Channel channel = p_manager.GetChannel();
			LGameChannel game = GetLChannel(channel.GetName());

			#region Sanity check
			if (game == null || !game.is_active) {
				E.Notice(nick, "There's no game or it did not start yet.");
				return;
			}

			LGPlayer player = game.GetPlayer(nick);
			if (player == null) {
				E.Notice(nick, "You are not part of the game.");
				return;
			}
			if (player != game.GetPlayer()) {
				E.Notice(nick, "It's not your turn yet. Please wait for " +
					game.GetPlayer().nick);
				return;
			}
			if (game.main_card == "") {
				E.Notice(nick, "You cannot check an empty stack.");
				return;
			}
			#endregion

			player = null;
			string main_card = game.main_card;
			bool contains_invalid = game.stack_top.FindIndex(item => item != main_card) >= 0;

			string card_msg = "";
			if (contains_invalid) {
				card_msg = "One or more top cards were not a [" + main_card + "]. ";
			} else {
				card_msg = "The top cards were correct! ";

				game.NextPlayer();
			}

			card_msg += "(" + FormatCards(game.stack_top) + ") ";

			// Add cards to previous player
			game.GetPlayer(-1).cards.AddRange(game.stack_all);
			game.CleanStack();
			CheckCards();

			// "channel" reference is not updated after deleting the channel!
			if (GetLChannel(channel.GetName()) == null) {
				channel.Say(card_msg);
				return;
			}

			var prev_player = game.GetPlayer(-1);
			var curr_player = game.GetPlayer(0);

			card_msg += "Complete stack goes to " + prev_player.nick + ". " +
				 curr_player.nick + " can start with an empty stack.";

			channel.Say(card_msg);
			E.Notice(prev_player.nick, FormatCards(prev_player.cards, true));
			E.Notice(curr_player.nick, FormatCards(curr_player.cards, true));
		}

		void Cmd_setcards(string nick, string message)
		{
			var chan = p_manager.GetChannel();
			string[] args = Chatcommand.Split(message);

			if (chan.GetUserData(nick).hostmask != G.settings["owner_hostmask"]) {
				chan.Say(nick + ": who are you?");
				return;
			}
			if (args.Length < 4) {
				chan.Say(nick + ": Too few cards! Add more.");
				return;
			}

			var lchan = lchans.Find(item => item.is_active);
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

		void CheckCards(string nick = null)
		{
			Channel chan = p_manager.GetChannel();
			LGameChannel game = GetLChannel(chan.GetName());
			if (game == null || !game.is_active)
				return;

			List<string> player_remove = new List<string>();

			foreach (LGPlayer player in game.players) {
				int amount = player.cards.Count;

				if (amount >= 4) {
					// Count cards
					var cards = new Dictionary<string, int>();
					foreach (string card in player.cards) {
						if (cards.ContainsKey(card))
							cards[card]++;
						else
							cards.Add(card, 1);
					}

					// Discard pairs
					var discarded = new List<string>();
					foreach (KeyValuePair<string, int> card in cards) {
						if (card.Value >= 4) {
							player.cards.RemoveAll(item => item == card.Key);
							discarded.Add(card.Key);
						}
					}
					if (discarded.Count > 0) {
						chan.Say(player.nick + " can discard four " + FormatCards(discarded) +
							" cards. Left cards: " + player.cards.Count);
						Thread.Sleep(300);
					}
				}

				if (amount == 0) {
					if (nick == null || (nick != null && nick != player.nick)) {
						chan.Say(player.nick + " has no cards left. Congratulations, you're a winner!");
						player_remove.Add(player.nick);
					} else {
						chan.Say(player.nick + " played " + Utils.Colorize("their last card", IRC_Color.ORANGE) + "!");
					}
				} else if (amount <= 3) {
					if (nick != null && nick == player.nick) {
						chan.Say(player.nick + " has " +
							Utils.Colorize("only " + amount + " cards", IRC_Color.ORANGE) + " left!");
					}
				}
			}

			foreach (string player in player_remove)
				OnUserLeave(player);
		}

		LGameChannel GetLChannel(string name)
		{
			return lchans.Find(item => item.name == name);
		}
	}
}