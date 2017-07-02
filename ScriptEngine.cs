/* 
 * Source code from: http://www.tuxenconsulting.dk/lua.zip
 */

using System;
using System.Collections.Generic;

namespace MAIN
{
	public class ScriptEngine
	{
		public IntPtr L = IntPtr.Zero;
		private List<Delegate> m_refs = new List<Delegate>();

		/// <summary>
		/// Returns the lua state, read-only
		/// </summary>

		/// <summary>
		/// Is the top of the lua stack nil ?
		/// </summary>
		private bool IsTopNil
		{
			get { return Lua.lua_type(L, -1) == Lua.LUA_TNIL; }
		}

		/// <summary>
		/// Default constructor, initializes a new Lua state by calling ResetLua() method
		/// </summary>
		public ScriptEngine()
		{
			//initialize lua
			ResetLua();
		}

		/// <summary>
		/// Constructor, initializes a new Lua state by calling ResetLua() method
		/// if lua is already open, then close current lua state, and assign the passed in state as the curent state
		/// </summary>
		public ScriptEngine(IntPtr lua_State)
		{
			//if lua is already open, then close current lua state
			CloseLua();
			//assign injected lua state
			L = lua_State;
		}

		/// <summary>
		/// Closes lua and clear errorlog and any references to functions that was registered with lua
		/// </summary>
		public void CloseLua()
		{
			if (L != IntPtr.Zero) {
				Lua.lua_settop(L, 0);
				Lua.lua_close(L);
				L = IntPtr.Zero;
			}
			//clear any references to callbacks to allow the garbage collector collect them
			m_refs.Clear();
		}

		/// <summary>
		/// Closes lua by calling CloseLua() method, then opens a new lua state with all libs open
		/// </summary>
		public void ResetLua()
		{
			//if lua is already open, then close current lua state, and open a new one
			CloseLua();

			//open lua
			L = Lua.lua_open();

			//open libraries
			Lua.luaL_openlibs(L);
		}

		/// <summary>
		/// Does the same as the staic method CloseLua(), but this method is a wrapper as an instance method
		/// </summary>
		public void StopEngineAndCleanup()
		{
			CloseLua();
		}

		/// <summary>
		/// Registers a .net method denoted by the delegate Lua.LuaFunction as a lua function
		/// the function will take on the name luaFuncName, in lua
		/// </summary>
		/// <param name="func">.net function to register with lua</param>
		/// <param name="luaFuncName">name of .net function in lua</param>
		public void RegisterLuaFunction(Lua.LuaFunction func, string luaFuncName)
		{
			Lua.lua_pushcfunction(L, func);
			Lua.lua_setglobal(L, luaFuncName);
			//Lua.lua_register(m_lua_State, luaFuncName, func);

			//make sure the delegate callback is not collected by the garbage collector before
			//unmanaged code has called back
			m_refs.Add(func);
		}

		/// <summary>
		/// Calls the Lua function 'luaFuncName' with the given parameters
		/// </summary>
		/// <param name="returnType">Error code</param>
		public int CallLuaFunction(string luaFuncName, object[] args)
		{
			Lua.lua_getglobal(L, luaFuncName);
			if (!Lua.lua_isfunction(L, -1)) {
				Lua.lua_pop(L, 1);
				return 1;
			}
			int argc = (args != null) ? args.Length : 0;
			for (int i = 0; i < argc; i++)
				PushBasicValue(args[i]);

			return Lua.lua_pcall(L, argc, 0, 0);
		}

		/// <summary>
		/// Calls the Lua function 'luaFuncName' with the given parameters
		/// </summary>
		/// <param name="returnType">Lua type of the return value</param>
		public object CallLuaFunction(string luaFuncName, object[] args, out int luaType)
		{
			luaType = 0;
			Lua.lua_getglobal(L, luaFuncName);
			if (!Lua.lua_isfunction(L, -1)) {
				Lua.lua_pop(L, 1);
				return null;
			}
			int argc = (args != null) ? args.Length : 0;
			for (int i = 0; i < argc; i++)
				PushBasicValue(args[i]);

			if (Lua.lua_pcall(L, argc, 1, 0) != 0)
				return null;

			return GetValueOfStack(out luaType);
		}

		/// <summary>
		/// Creates a table with tableName if it does not already exist.
		/// After creation or if the table already exists, stack will be empty and balanced.
		/// </summary>
		/// <param name="tableName"></param>
		public void CreateLuaTable(string tableName)
		{
			Lua.lua_getglobal(L, tableName);
			// Back to root
			Lua.lua_pop(L, 1);

			if (IsTopNil) {
				Lua.lua_newtable(L);
				Lua.lua_setglobal(L, tableName);
				//stack is empty
			}
		}

		/// <summary>
		/// Gets the value of a field of a table and returns it as type object i.e. return luaTable.someField
		/// After the call the lua stack is balanced
		/// </summary>
		/// <param name="tableName">name of lua table</param>
		/// <param name="fieldName">name of table field</param>
		/// <returns>returns luaTable.someField</returns>
		public object GetTableField(string tableName, string fieldName)
		{
			//push table on the stack
			Lua.lua_getglobal(L, tableName);

			if (IsTopNil)
				throw new Exception("GetTableField, the table does not exist: " + tableName);

			//push table field name on the stack
			Lua.lua_pushstring(L, fieldName);

			//get value of field of table: get tableName[fieldName]
			Lua.lua_gettable(L, -2);

			//get the result of the stack
			int luaType = 0;
			object value = GetValueOfStack(out luaType);

			//pop table of the stack
			Lua.lua_pop(L, 1);

			//stack is balanced
			return value;
		}

		/// <summary>
		/// Gets the value of a field of a lua table and returns it as type object i.e. return luaTable.someField
		/// The table to operate on must currently be on top of the lua stack.
		/// After the call the lua table is still on top of the stack
		/// </summary>
		/// <param name="fieldName">
		/// the name of the field, belonging to a lua table currently sitting on 
		/// top of the lua stack
		/// </param>
		/// <returns>returns luaTable.someField</returns>
		public object GetTableField(string fieldName)
		{
			if (IsTopNil)
				throw new Exception("GetTableField, no table on top of stack");

			//push table field name on the stack
			Lua.lua_pushstring(L, fieldName);

			//get value of field of table: get tableName[fieldName]
			Lua.lua_gettable(L, -2);

			//get the result of the stack
			int luaType = 0;
			object value = GetValueOfStack(out luaType);

			//table is still on top of the stack
			return value;
		}

		/// <summary>
		/// Sets a field of a lua table to a lua variable identified by luaIdentifier i.e. luaTable.someField = luaVariable
		/// After the call the lua stack is balanced.
		/// </summary>
		/// <param name="tableName">name of the lua table</param>
		/// <param name="fieldName">name of the field</param>
		/// <param name="luaIdentifier">the identifier identifying the lua variable</param>
		public void SetTableFieldToLuaIdentifier(string tableName, string fieldName, string luaIdentifier)
		{
			Lua.lua_getglobal(L, tableName);
			Lua.lua_pushstring(L, fieldName);
			Lua.lua_getglobal(L, luaIdentifier);
			Lua.lua_settable(L, -3);
			Lua.lua_pop(L, 1);
			//stack is balanced
		}

		/// <summary>
		/// Sets a field of a lua table to a lua variable identified by luaIdentifier i.e. luaTable.someField = luaVariable
		/// The table to operate on must currently be on top of the lua stack.
		/// After the call the lua table is still on top of the stack.
		/// </summary>
		/// <param name="fieldName">name of the field</param>
		/// <param name="luaIdentifier">the identifier identifying the lua variable</param>
		public void SetTableFieldToLuaIdentifier(string fieldName, string luaIdentifier)
		{
			Lua.lua_pushstring(L, fieldName);
			Lua.lua_getglobal(L, luaIdentifier);
			Lua.lua_settable(L, -3);
			//table is still on top of the stack
		}

		/// <summary>
		/// Sets a field of a lua table to a .net variable value i.e. luaTable.someField = value of .net variable.
		/// After the call the lua stack is balanced.
		/// </summary>
		/// <param name="tableName">name of the lua table</param>
		/// <param name="fieldName">name of the field</param>
		/// <param name="value">.net variable</param>
		public void SetTableField(string tableName, string fieldName, object value)
		{
			//push table on the stack
			Lua.lua_getglobal(L, tableName);

			if (IsTopNil)
				throw new Exception("SetTableField, the table does not exist: " + tableName);

			//push table field name on the stack
			Lua.lua_pushstring(L, fieldName);

			//push value on the stack
			PushBasicValue(value);

			//set field of table to value: tableName[fieldName]=value
			Lua.lua_settable(L, -3);
			//pop table of the stack
			Lua.lua_pop(L, 1);
			//stack is balanced
		}

		/// <summary>
		/// Sets a field of a lua table to a .net variable value i.e. luaTable.someField = value of .net variable.
		/// After the call the lua table is still on top of the stack.
		/// </summary>
		/// <param name="fieldName">name of the field</param>
		/// <param name="value">.net variable</param>
		public void SetTableField(string fieldName, object value)
		{
			if (IsTopNil)
				throw new Exception("SetTableField, no table on top of the stack");

			//push table field name on the stack
			Lua.lua_pushstring(L, fieldName);

			//push value on the stack
			PushBasicValue(value);

			//set field of table to value: tableName[fieldName]=value
			Lua.lua_settable(L, -3);

			//table is still on top of the stack
		}

		/// <summary>
		/// Pops the value sitting on top of the lua stack
		/// </summary>
		/// <param name="luaType">the lua type of the value that was retrieved from the stack, is stored in this out parameter</param>
		/// <returns>the value popped off the stack</returns>
		public object GetValueOfStack(out int luaType)
		{
			luaType = 0;
			object value = null;
			switch (Lua.lua_type(L, -1)) {
			case Lua.LUA_TNONE: {
					value = null;
					luaType = Lua.LUA_TNONE;
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}
			case Lua.LUA_TNIL: {
					value = null;
					luaType = Lua.LUA_TNIL;
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}
			case Lua.LUA_TSTRING: {
					luaType = Lua.LUA_TSTRING;
					value = Lua.lua_tostring(L, -1);
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}
			case Lua.LUA_TNUMBER: {
					luaType = Lua.LUA_TNUMBER;
					value = Lua.lua_tonumber(L, -1);
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}
			case Lua.LUA_TBOOLEAN: {
					luaType = Lua.LUA_TBOOLEAN;
					int intToBool = Lua.lua_toboolean(L, -1);
					value = intToBool > 0 ? true : false;
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}
			case Lua.LUA_TTABLE: {
					luaType = Lua.LUA_TTABLE;
					value = "table";
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}
			default: {
					value = null;
					//pop value from stack
					Lua.lua_pop(L, 1);
					break;
				}

			//case Lua.LUA_TINTEGER:
			//{
			//    value = Lua.lua_tointeger(m_lua_State, -1);
			//    //pop value from stack
			//    Lua.lua_pop(m_lua_State, 1);
			//    break;
			//}
			}

			return value;
		}

		/// <summary>
		/// Pushes the value on to the lua stack based on value.GetType(), and how that type maps to a lua type.
		/// Pushes nil if object is null or typeof object is not a basic type
		/// </summary>
		/// <param name="value">the value to push on to the stack</param>
		public void PushBasicValue(object value)
		{
			//push value on the stack
			if (value == null) {
				Lua.lua_pushnil(L);
				return;
			}
			if (value.GetType() == typeof(string)) {
				Lua.lua_pushstring(L, value.ToString());
				return;
			}
			if (value.GetType() == typeof(bool)) {
				Lua.lua_pushboolean(L, (bool)value);
				return;
			}
			if (value.GetType() == typeof(decimal)) {
				Lua.lua_pushnumber(L, Convert.ToDouble(((decimal)value)));
				return;
			}
			if (value.GetType() == typeof(float)) {
				Lua.lua_pushnumber(L, Convert.ToDouble(((float)value)));
				return;
			}
			if (value.GetType() == typeof(double)) {
				Lua.lua_pushnumber(L, Convert.ToDouble(((double)value)));
				return;
			}
			if (value.GetType() == typeof(int)) {
				Lua.lua_pushinteger(L, (int)value);
				return;
			}
			if (value.GetType() == typeof(DateTime)) {
				Lua.lua_pushstring(L, ((DateTime)value).ToString());
				return;
			}
			if (value.GetType() == typeof(Guid)) {
				Lua.lua_pushstring(L, ((Guid)value).ToString());
				return;
			}
			if (value.GetType().IsEnum) {
				Lua.lua_pushstring(L, value.ToString());
				return;
			}
			Lua.lua_pushnil(L);
		}

		public bool CheckString(string prefix, int idx)
		{
			if (Lua.lua_type(L, idx) == Lua.LUA_TSTRING)
				return true;

			Lua.luaL_error(L, prefix + ": Invalid argument #" + idx + ". Type 'string' expected");
			return false;
		}

		public bool CheckNumber(string prefix, int idx)
		{
			if (Lua.lua_type(L, idx) == Lua.LUA_TNUMBER)
				return true;

			Lua.luaL_error(L, prefix + ": Invalid argument #" + idx + ". Type 'number' expected");
			return false;
		}
	}
}