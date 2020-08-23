using System;
using System.Collections.Generic;
using System.Threading;

namespace MAIN
{
	class m_Lua : Module
	{
		const int LUA_TIMEOUT = 4000;
		const int LUA_TEXT_MAX = 453;

		ScriptEngine SE = new ScriptEngine();
		System.Text.StringBuilder lua_packet = null;
		System.Diagnostics.Stopwatch lua_timer;
		bool lua_lock;

		Thread lua_thread;

		public m_Lua(Manager manager) : base("Lua", manager)
		{
			lua_timer = new System.Diagnostics.Stopwatch();
		}

		public override void OnUserSay(string nick, string message,
				int length, ref string[] args)
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
			Channel channel = p_manager.GetChannel();
			lua_timer.Start();
			lua_thread = new Thread(delegate () {
				channel.Say(LuaRun(nick, channel, str));
			});
			lua_thread.Start();
		}

		string LuaRun(string nick, Channel chan, string message)
		{
			if (!System.IO.File.Exists("plugins/security.lua"))
				return "Error: File 'plugins/security.lua' does not exist";

			bool is_private = chan.IsPrivate();
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
				foreach (var user in chan.users) {
					Lua.lua_pushinteger(SE.L, ++index);
					Lua.lua_newtable(SE.L);

					Lua.lua_pushstring(SE.L, "nick");
					Lua.lua_pushstring(SE.L, user.Key);
					Lua.lua_settable(SE.L, -3);

					Lua.lua_pushstring(SE.L, "hostmask");
					Lua.lua_pushstring(SE.L, user.Value.hostmask);
					Lua.lua_settable(SE.L, -3);

					Lua.lua_settable(SE.L, -3);
				}
			}
			Lua.lua_setglobal(SE.L, "N");
			#endregion

			#region Public chat variables variables
			SE.CreateLuaTable("L");
			Lua.lua_getglobal(SE.L, "L");
			SE.SetTableField("channel", chan.GetName());
			SE.SetTableField("nick", nick);
			SE.SetTableField("botname", G.settings["nickname"]);
			SE.SetTableField("hostmask", chan.GetUserData(nick).hostmask);
			SE.SetTableField("isprivate", is_private);
			SE.SetTableField("online", index);
			SE.SetTableField("owner_hostmask", G.settings["owner_hostmask"]);
			Lua.lua_settop(SE.L, 0);
			#endregion

			int lua_error = 1;
			string lua_output = null;

			lua_error = Lua.luaL_dofile(SE.L, "plugins/security.lua");
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
			return answer_s;
		}


		bool isTimeout(IntPtr ptr)
		{
			if (lua_timer.ElapsedMilliseconds > LUA_TIMEOUT) {
				L.Log("m_Lua::isTimeout, code ran too long");
				Lua.luaL_error(ptr, "STOP");
				return true;
			}
			return false;
		}

		int l_panic(IntPtr ptr)
		{
			L.Log("m_Lua::l_panic, panic", true);
			for (int i = 1; i <= Lua.lua_gettop(ptr); i++)
				Console.WriteLine("Panic [" + i + "]: " + Lua.lua_tostring(ptr, i));
			return 0;
		}

		int l_print(IntPtr ptr)
		{
			if (!SE.CheckString(ptr, "print", 1))
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
			if (isTimeout(ptr))
				return 0;

			if (!SE.CheckString(ptr, "stringldistance", 1) ||
				!SE.CheckString(ptr, "stringldistance", 2))
				return 0;

			string string_a = Lua.lua_tostring(ptr, 1);
			string string_b = Lua.lua_tostring(ptr, 2);

			Lua.lua_pushinteger(SE.L, Utils.LevenshteinDistance(string_a, string_b));
			return 1;
		}
		int l_getUserstatus(IntPtr ptr)
		{
			if (isTimeout(ptr))
				return 0;

			if (!SE.CheckString(ptr, "getUserstatus", 1))
				return 0;

			string to_find = Lua.lua_tostring(ptr, 1).ToLower();
			string nick = null;

			foreach (Channel chan in p_manager.UnsafeGetChannels()) {
				nick = chan.FindNickname(to_find, false);
				if (nick != null)
					break;
			}
			if (nick == null)
				return 0;

			Lua.lua_pushinteger(SE.L, p_manager.GetUserStatus(nick));
			return 1;
		}
	}
}