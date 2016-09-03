using System;
using System.Runtime.InteropServices;

namespace MAIN
{
	public static class Lua_53
	{
		const string LIBNAME = "lua53";

		public const string LUA_SIGNATURE = "\033Lua";

		public const int LUA_MULTRET = (-1);

		public const int LUA_REGISTRYINDEX = (-10000),
				LUA_ENVIRONINDEX = (-10001),
				LUA_GLOBALSINDEX = (-10002);

		public const int LUA_YIELD = 1,
				LUA_ERRRUN = 2,
				LUA_ERRSYNTAX = 3,
				LUA_ERRMEM = 4,
				LUA_ERRERR = 5;

		public const int LUA_TNONE = (-1),
				LUA_TNIL = 0,
				LUA_TBOOLEAN = 1,
				LUA_TLIGHTUSERDATA = 2,
				LUA_TNUMBER = 3,
				LUA_TSTRING = 4,
				LUA_TTABLE = 5,
				LUA_TFUNCTION = 6,
				LUA_TUSERDATA = 7,
				LUA_TTHREAD = 8,
				LUA_MINSTACK = 20;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl‌)]
		public delegate int LuaFunction(IntPtr lua_State);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gc")]
		public static extern int lua_gc(IntPtr luaState, int what, int data);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_typename")]
		public static extern IntPtr lua_typename(IntPtr luaState, int type);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "luaL_error")]
		public static extern void luaL_error(IntPtr luaState, string message);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_where")]
		public static extern void luaL_where(IntPtr luaState, int level);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_newstate")]
		public static extern IntPtr luaL_newstate();

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_close")]
		public static extern void lua_close(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_openlibs")]
		public static extern void luaL_openlibs(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "luaL_loadstring")]
		public static extern int luaL_loadstring(IntPtr luaState, string chunk);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadstring")]
		public static extern int luaL_loadstring(IntPtr luaState, byte[] chunk);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_createtable")]
		public static extern void lua_createtable(IntPtr luaState, int narr, int nrec);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettable")]
		public static extern void lua_gettable(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_settop")]
		public static extern void lua_settop(IntPtr luaState, int newTop);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_insert")]
		public static extern void lua_insert(IntPtr luaState, int newTop);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_remove")]
		public static extern void lua_remove(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawget")]
		public static extern void lua_rawget(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_settable")]
		public static extern void lua_settable(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawset")]
		public static extern void lua_rawset(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setmetatable")]
		public static extern void lua_setmetatable(IntPtr luaState, int objIndex);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getmetatable")]
		public static extern int lua_getmetatable(IntPtr luaState, int objIndex);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_equal")]
		public static extern int lua_equal(IntPtr luaState, int index1, int index2);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushvalue")]
		public static extern void lua_pushvalue(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_replace")]
		public static extern void lua_replace(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettop")]
		public static extern int lua_gettop(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_type")]
		public static extern int lua_type(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_ref")]
		public static extern int luaL_ref(IntPtr luaState, int registryIndex);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawgeti")]
		public static extern void lua_rawgeti(IntPtr luaState, int tableIndex, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawseti")]
		public static extern void lua_rawseti(IntPtr luaState, int tableIndex, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_newuserdata")]
		public static extern IntPtr lua_newuserdata(IntPtr luaState, uint size);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_touserdata")]
		public static extern IntPtr lua_touserdata(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_unref")]
		public static extern void luaL_unref(IntPtr luaState, int registryIndex, int reference);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isstring")]
		public static extern int lua_isstring(IntPtr luaState, int index);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_iscfunction")]
		public static extern int lua_iscfunction(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushnil")]
		public static extern void lua_pushnil(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pcall")]
		public static extern int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tocfunction")]
		public static extern LuaFunction lua_tocfunction(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tonumber")]
		public static extern double lua_tonumber(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_toboolean")]
		public static extern int lua_toboolean(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_atpanic")]
		public static extern void lua_atpanic(IntPtr luaState, LuaFunction panicf);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushstdcallcfunction")]
		public static extern void lua_pushstdcallcfunction(IntPtr luaState, LuaFunction function);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushnumber")]
		public static extern void lua_pushnumber(IntPtr luaState, double number);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushinteger")]
		public static extern void lua_pushinteger(IntPtr lua_State, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushboolean")]
		public static extern void lua_pushboolean(IntPtr luaState, int value);


		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tolstring")]
		public static extern IntPtr lua_tolstring(IntPtr luaState, int index, out uint strLen);

#if WSTRING
		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "lua_pushlwstring")]
#else
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_pushlstring")]
#endif
		public static extern void lua_pushlstring(IntPtr luaState, string str, uint size);

#if WSTRING
		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "lua_pushwstring")]
#else
		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_pushstring")]
#endif
		public static extern void lua_pushstring(IntPtr luaState, string str);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushcclosure")]
		public static extern void lua_pushcclosure(IntPtr lua_State, LuaFunction func, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "luaL_newmetatable")]
		public static extern int luaL_newmetatable(IntPtr luaState, string meta);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_getfield")]
		public static extern void lua_getfield(IntPtr luaState, int stackPos, string meta);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "luaL_checkudata")]
		public static extern IntPtr luaL_checkudata(IntPtr luaState, int stackPos, string meta);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "luaL_getmetafield")]
		public static extern int luaL_getmetafield(IntPtr luaState, int stackPos, string field);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_loadbuffer")]
		public static extern int lua_loadbuffer(IntPtr luaState, string buff, uint size, string name);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_loadbuffer")]
		public static extern int lua_loadbuffer(IntPtr luaState, byte[] buff, uint size, string name);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_loadfile")]
		public static extern int lua_loadfile(IntPtr luaState, string filename);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_error")]
		public static extern void lua_error(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_checkstack")]
		public static extern int lua_checkstack(IntPtr luaState, int extra);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_next")]
		public static extern int lua_next(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushlightuserdata")]
		public static extern void lua_pushlightuserdata(IntPtr luaState, IntPtr udata);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_checkmetatable")]
		public static extern int luaL_checkmetatable(IntPtr luaState, int obj);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gethookmask")]
		public static extern int lua_gethookmask(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_sethook")]
		public static extern int lua_sethook(IntPtr luaState, IntPtr func, int mask, int count);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gethookcount")]
		public static extern int lua_gethookcount(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getinfo")]
		public static extern int lua_getinfo(IntPtr luaState, string what, IntPtr ar);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getstack")]
		public static extern int lua_getstack(IntPtr luaState, int level, IntPtr n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getlocal")]
		public static extern IntPtr lua_getlocal(IntPtr luaState, IntPtr ar, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setlocal")]
		public static extern IntPtr lua_setlocal(IntPtr luaState, IntPtr ar, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getupvalue")]
		public static extern IntPtr lua_getupvalue(IntPtr luaState, int funcindex, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setupvalue")]
		public static extern IntPtr lua_setupvalue(IntPtr luaState, int funcindex, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tonetobject")]
		public static extern int lua_tonetobject(IntPtr luaState, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_newudata")]
		public static extern void lua_newudata(IntPtr luaState, int val);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawnetobj")]
		public static extern int lua_rawnetobj(IntPtr luaState, int obj);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_checkudata")]
		public static extern int lua_checkudata(IntPtr luaState, int ud, string tname);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettag")]
		public static extern IntPtr lua_gettag();

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushglobaltable")]
		public static extern void lua_pushglobaltable(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_popglobaltable")]
		public static extern void lua_popglobaltable(IntPtr luaState);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_getglobal")]
		public static extern void lua_getglobal(IntPtr luaState, string name);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "lua_setglobal")]
		public static extern void lua_setglobal(IntPtr luaState, string name);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_registryindex")]
		public static extern int lua_registryindex();

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_get_main_state")]
		public static extern IntPtr lua_get_main_state(IntPtr luaState);


		public static void lua_pushboolean(IntPtr lua_State, bool b)
		{
			lua_pushboolean(lua_State, b ? 1 : 0);
		}

		public static void lua_pop(IntPtr lua_State, int amount)
		{
			lua_settop(lua_State, -(amount) - 1);
		}

		public static void lua_newtable(IntPtr lua_State)
		{
			lua_createtable(lua_State, 0, 0);
		}

		public static void lua_register(IntPtr lua_State, string n, LuaFunction func)
		{
			lua_pushcclosure(lua_State, func, 0);
			lua_setglobal(lua_State, n);
		}

		public static void lua_pushcfunction(IntPtr lua_State, LuaFunction func)
		{
			lua_pushcclosure(lua_State, func, 0);
		}

		public static bool lua_isfunction(IntPtr lua_State, int n)
		{
			return lua_type(lua_State, n) == LUA_TFUNCTION;
		}

		public static bool lua_istable(IntPtr lua_State, int n)
		{
			return lua_type(lua_State, n) == LUA_TTABLE;
		}

		public static bool lua_isnil(IntPtr lua_State, int n)
		{
			return lua_type(lua_State, n) == LUA_TNIL;
		}

		public static bool lua_isboolean(IntPtr lua_State, int n)
		{
			return lua_type(lua_State, n) == LUA_TBOOLEAN;
		}

		public static bool lua_isnone(IntPtr lua_State, int n)
		{
			return lua_type(lua_State, n) == LUA_TNONE;
		}

		public static bool lua_isnoneornil(IntPtr lua_State, int n)
		{
			return lua_type(lua_State, n) <= 0;
		}

		public static string lua_tostring(IntPtr lua_State, int n)
		{
			uint length;
			IntPtr data = lua_tolstring(lua_State, n, out length);
			return Marshal.PtrToStringAuto(data, (int)length);
		}

		public static IntPtr lua_open()
		{
			return luaL_newstate();
		}

		public static int luaL_dostring(IntPtr lua_State, string s)
		{
			if (luaL_loadstring(lua_State, s) != 0)
				return 1;
			return lua_pcall(lua_State, 0, LUA_MULTRET, 0);
		}

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadfilex(IntPtr lua_State, string s, string mode);

		public static int luaL_dofile(IntPtr lua_State, string s)
		{
			if (luaL_loadfilex(lua_State, s, "r") != 0)
				return 1;
			return lua_pcall(lua_State, 0, LUA_MULTRET, 0);
		}
	}
}
