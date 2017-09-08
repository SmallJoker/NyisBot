using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{
	class m_Lua
	{
		const int LUA_TIMEOUT = 4000;
		const int LUA_TEXT_MAX = 453;

		ScriptEngine SE = new ScriptEngine();
		System.Text.StringBuilder lua_packet = null;
		System.Diagnostics.Stopwatch lua_timer;
		int lua_timeout;
		bool lua_lock;

		Thread lua_thread;
		public Dictionary<string, int> userstatus_queue;

		public m_Lua()
		{
			lua_timer = new System.Diagnostics.Stopwatch();
			userstatus_queue = new Dictionary<string, int>();
			E.OnUserSay += OnUserSay;
		}

		void OnUserSay(string nick, string hostmask, string channel, string message,
			int length, int channel_id, ref string[] args)
		{
			if (args[0] != "$" && args[0] != "$lua")
				return;

			if (lua_timer.IsRunning)
				return;

			string str = "";
			for (int i = 1; i < length; i++) {
				if (i + 1 < length)
					str += args[i] + ' ';
				else
					str += args[i];
			}
			if (str.Length < 5) {
				E.Notice(nick, "Too short input text.");
				return;
			}

			lua_thread = new Thread(delegate() {
				E.Say(channel, LuaRun(nick, hostmask, channel, str, channel_id));
			});
			lua_thread.Start();
		}

		string LuaRun(string nick, string hostmask, string channel,
			string message, int channel_id)
		{
			bool is_private = channel[0] != '#';
			// Initialize packet lock, packet and start time
			lua_lock = false;
			lua_packet = new System.Text.StringBuilder();

			SE.ResetLua();
			SE.RegisterLuaFunction(l_print, "print");
			SE.RegisterLuaFunction(l_stringldistance, "stringldistance");
			SE.RegisterLuaFunction(l_getUserstatus, "getUserstatus");
			Lua.lua_atpanic(SE.L, l_panic);

			#region Nick list
			Lua.lua_newtable(SE.L);

			int index = 0;
			if (!is_private) {
				foreach (KeyValuePair<string, string> _nick in E.chans[channel_id].nicks) {
					Lua.lua_pushinteger(SE.L, ++index);
					Lua.lua_newtable(SE.L);

					Lua.lua_pushstring(SE.L, "nick");
					Lua.lua_pushstring(SE.L, _nick.Key);
					Lua.lua_settable(SE.L, -3);
					Lua.lua_pushstring(SE.L, "hostmask");
					Lua.lua_pushstring(SE.L, _nick.Value);

					Lua.lua_settable(SE.L, -3);
					Lua.lua_settable(SE.L, -3);
				}
			}
			Lua.lua_setglobal(SE.L, "N");
			#endregion

			#region Public chat variables variables
			SE.CreateLuaTable("L");
			Lua.lua_getglobal(SE.L, "L");
			SE.SetTableField("channel", channel);
			SE.SetTableField("nick", nick);
			SE.SetTableField("botname", G.settings["nickname"]);
			SE.SetTableField("hostmask", hostmask);
			SE.SetTableField("isprivate", is_private);
			SE.SetTableField("online", index);
			SE.SetTableField("owner_hostmask", G.settings["owner_hostmask"]);
			Lua.lua_pop(SE.L, 1);
			#endregion

			lua_timeout = E.HDDisON() ? LUA_TIMEOUT : (LUA_TIMEOUT + 5000);
			lua_timer.Start();
			// Can abort lua_thread but not Lua!

			int lua_error = 1;
			string lua_output = null;

			lua_error = Lua.luaL_dofile(SE.L, "security.lua");
			if (lua_error == 0)
				lua_error = Lua.luaL_dostring(SE.L, message);

			while (lua_lock)
				System.Threading.Thread.Sleep(100);

			int type = Lua.lua_type(SE.L, -1);

			if (type == Lua.LUA_TSTRING) {
				int length = Lua.lua_strlen(SE.L, -1);
				if (length > LUA_TEXT_MAX) {
					lua_output = "<too long message>";
					Lua.lua_pop(SE.L, 1);
					type = Lua.LUA_TNONE;
				}
			}
			if (type != Lua.LUA_TNIL && type != Lua.LUA_TNONE) {
				lua_output = Lua.lua_tostring(SE.L, -1);
			}

			lua_lock = true;
			if (lua_output != null)
				lua_packet.Append(lua_output);

			if (lua_error > 0)
				L.Log("m_Lua::LuaRun, errorcode = " + lua_error, true);

			lua_lock = false;

			SE.CloseLua();
			lua_timer.Reset();

			#region Remove control characters, '\n' to space
			char[] answer = new char[LUA_TEXT_MAX];
			int pos = 0;
			for (int i = 0; i < lua_packet.Length && pos < answer.Length; i++) {
				char cur = lua_packet[i];
				if (cur == '\t' || cur == '\n')
					cur = ' ';

				if (cur == 0 || cur == '\r')
					continue;

				answer[pos] = cur;
				pos++;
			}
			#endregion
			if (pos == answer.Length) {
				answer[--pos] = '.';
				answer[--pos] = '.';
				answer[--pos] = ' ';
				pos += 3;
			}

			string answer_s = new string(answer, 0, pos);
			if (pos == 0)
				answer_s = nick + ": <no return text>";

			userstatus_queue.Clear();
			return answer_s;
		}


		bool l_checktimer()
		{
			if (lua_timer.ElapsedMilliseconds > lua_timeout) {
				Lua.luaL_error(SE.L, "STOP");
				L.Log("m_Lua::l_checktimer, code ran too long", true);
				lua_timer.Reset();
				return true;
			}
			return false;
		}
		/*void Lua_breakdown()
		{
			lua_timer.Start();
			while (lua_timer.IsRunning) {
				if (lua_timer.ElapsedMilliseconds > lua_timeout) {
					Lua.lua_settop(L.L, 0);

					Lua.luaL_error(L.L, "Code execution interrupted.");

					Lua.lua_getglobal(L.L, "os");
					Lua.lua_pushliteral(L.L, "exit");
					int r = Lua.lua_pcall(L.L, 0, 0, 0);
					if (r != 0) {
						log(Lua.lua_tostring(L.L, -1), true);
						Lua.lua_pop(L.L, 1);
					}
					lua_timer.Reset();
				}
			}
		}*/

		int l_panic(IntPtr ptr)
		{
			L.Log("m_Lua::l_panic, panic", true);
			return 0;
		}

		int l_print(IntPtr ptr)
		{
			if (l_checktimer())
				return 0;

			if (!SE.CheckString("print", 1))
				return 0;

			while (lua_lock)
				System.Threading.Thread.Sleep(5);
			lua_lock = true;

			string text = Lua.lua_tostring(ptr, 1);
			lua_packet.AppendLine(text);

			lua_lock = false;
			return 0;
		}
		int l_stringldistance(IntPtr ptr)
		{
			if (l_checktimer())
				return 0;

			if (!SE.CheckString("stringldistance", 1) ||
				!SE.CheckString("stringldistance", 2))
				return 0;

			string string_a = Lua.lua_tostring(ptr, 1);
			string string_b = Lua.lua_tostring(ptr, 2);

			Lua.lua_pushinteger(SE.L, E.LevenshteinDistance(string_a, string_b));
			return 1;
		}
		int l_getUserstatus(IntPtr ptr)
		{
			if (l_checktimer())
				return 0;

			if (!SE.CheckString("getUserstatus", 1))
				return 0;

			string nick = Lua.lua_tostring(ptr, 1).ToLower();

			bool found = false;
			for (int i = 0; i < E.chans.Length; i++) {
				Channel cur = E.chans[i];
				if (cur == null)
					continue;

				foreach (KeyValuePair<string, string> k in cur.nicks) {
					if (k.Key.ToLower() == nick) {
						nick = k.Key;
						found = true;
						break;
					}
				}
				if (found)
					break;
			}
			if (!found)
				return 0;

			if (userstatus_queue.ContainsKey(nick)) {
				Lua.lua_pushinteger(SE.L, userstatus_queue[nick]);
				return 1;
			}

			System.Diagnostics.Stopwatch nickserv_time = new System.Diagnostics.Stopwatch();
#if USE_ACC_FOR_NICKSERV
			E.Say("NickServ", "ACC " + nick);
#else
			E.Say("NickServ", "STATUS " + nick);
#endif

			while (nickserv_time.ElapsedMilliseconds < 1500) {
				if (userstatus_queue.ContainsKey(nick)) {
					Lua.lua_pushinteger(SE.L, userstatus_queue[nick]);
					return 1;
				}

				Thread.Sleep(100);
			}
			userstatus_queue.Remove(nick);
			return 0;
		}
	}
}