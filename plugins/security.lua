-- security.lua
-- This file is included before any chat command is executed.

local _rawget = rawget
local start_time = os.clock()

local function timeouts(event, line)
	if os.clock() - start_time > 5 then
		error("THREAD STOPPED ")
	end
end
debug.sethook(timeouts, "l", 1E6)

function table.find(tab, val)
	for k, v in pairs(tab) do
		if v == val then
			return k
		end
	end
end


--{ READONLY TABLES
local function protect_table(t)
	return setmetatable({}, {
		__index = t,
		__newindex = function(t, key, value)
			error("Table is readonly")
		end,
		__metatable = false
	})
end

for k = 1, L.online do
	N[k] = protect_table(N[k])
end

L = protect_table(L)
--} READONLY TABLES

--{ PROTECT INCLUDE FUNCTIONS
local function protect_function(name, empty_msg, check)
	local old_func = _G[name]
	assert(old_func ~= nil, "Function "..name.." is nil.")

	_G[name] = function(arg)
		assert(type(arg) == "string", "Argument #1: String expected.")
		assert(arg ~= "", empty_msg)
		if check then
			arg = check(arg) or arg
		end
		return old_func(arg)
	end
end

-- List of already included files, to prevent spam
local included_files = {}

local function check_included(file)
	assert(not table.find(included_files, file), "Blocked from re-including "..file..".")
	table.insert(included_files, file)
	return file
end

protect_function("dofile", "Invalid file name.", function(arg)
	local path_given = string.find(arg, "\\") or string.find(arg, "/")
	if debug.getinfo(3).source:sub(1, 1) ~= "@" then
		assert(not path_given, "Blocked for security reasons.")
	end
	return check_included(path_given and arg or ("plugins/" .. arg))
end)

protect_function("loadfile", "Invalid file name.", function(arg)
	local path_given = string.find(arg, "\\") or string.find(arg, "/")
	if debug.getinfo(3).source:sub(1, 1) ~= "@" then
		assert(not path_given, "Blocked for security reasons.")
	end
	return check_included(path_given and arg or ("plugins/" .. arg))
end)

protect_function("require", "Invalid file name.", function(arg)
	assert(debug.getinfo(3).source:sub(1, 1) == "@", "Blocked for security reasons.")
	check_included(arg)
end)

protect_function("loadstring", "Invalid input string.", function(arg)
	assert(debug.getinfo(3).source:sub(1, 1) == "@", "Blocked for security reasons.")
	check_included(arg)
end)
--} PROTECT INCLUDE FUNCTIONS

start_time = os.clock()

--{ DISABLE EVIL FUNCTIONS
os.execute = nil
os.exit = nil

os.getenv = nil
os.remove = nil
os.rename = nil
os.setlocale = nil

include = dofile
coroutine = nil
module = nil
package = nil

load = nil
pcall = nil
rawget = nil
rawset = nil
rawequal = nil
xpcall = nil
unpack = nil
getfenv = nil
setfenv = nil

debug.sethook = nil

io = nil

debug = protect_table(debug)
--} DISABLE EVIL FUNCTIONS

math.randomseed(os.clock() * 1000)

dofile("misc_helpers.lua")
string = protect_table(string)
math   = protect_table(math)
table  = protect_table(table)

dofile("complex.lua")
--dofile("internetz.lua")

-- On-demand API functions
local api_loaded = {}
local function register_api(tablename, filename)
	_G[tablename] = setmetatable({}, {
		__index = function(self, key)
			if not api_loaded[tablename] then
				dofile(filename)
				api_loaded[tablename] = true
			end
			return _rawget(self, key)
		end,
		__metatable = false
	})
end

register_api("karma",   "karma.lua")
register_api("morse",   "morse.lua")
register_api("noobgen", "noobgen.lua")
register_api("quotes",  "quotes.lua")


function sell(text)
	assert(type(text) == "string", "Argument #1: String expected.")

	if math.random() < 0.167 then
		print(text.." was thrown into the \x034fire\x0F. \x033Please wait...")
		return
	end
	print("Sold "..text.." for \x033$"..(math.ceil(math.random() * 100) * 10))
end

-- Russian roulette. Have fun.
function fire()
	if math.random() > 0.167 then
		print(L.nick ..": *click*")
		return
	end
	print(L.nick ..": PAM. Congratulations, you wasted your life for nothing!")
end

function help()
	print(L.nick ..": Explanation of the basic functions: https://github.com/SmallJoker/NyisBot/blob/master/HELP.txt")
end

local function groupAction(nick, group, func)
	assert(type(nick) == "string" and type(group) == "string",
			"Argument #1 and/or #2: String expected.")

	local object = getUserObject(nick)
	if not object then
		print(L.nick ..": Unknown nickname.")
		return
	end
	print("(".. L.nick ..") ".. func():gsub("{n}", object.nick))
end

function adduser(nick, group)
	groupAction(nick, group, function()
		return "Added user {n} to group ".. c(group)
	end)
end

function remuser(nick, group)
	groupAction(nick, group, function()
		return "Removed user {n} from group ".. c(group)
	end)
end

function paperfold(height, thickness)
	if type(height) == "string" then
		height = fromFancy(height, 1)
	else
		assert(type(height) == "number", "Argument #1: Number expected")
	end

	if type(thickness) == "string" then
		thickness = fromFancy(thickness, 2)
	else
		if type(thickness) ~= "number" then
			thickness = 0.1E-3
		end
	end

	local step1 = height / thickness

	-- 0.1mm * 2^x = height
	-- height / thickness = 2^x -> step1 = 2^x
	-- log(step1) = log(2) * x

	local times = math.round(math.log(step1) / math.log(2))
	local real_height = thickness * 2^times
	print(L.nick..": Folding it ".. times .." times results in a "..
		toFancy(real_height, 3) .." meter high pile of paper")
end

-- For the case when there's another Lua supporting bot
function haystack(text)
	if not text then
		text = "Hello"
	else
		text = tostring(text)
	end
	local out = ""
	local cmd = false
	local depth = 0

	--		0		1		3		  7
	-- print2("print1(\"print2(\\\"print1(\\\\\\\"test\\\\\\\")\\\")")")
	-- print1("print2(\"print1(\\\"test\\\")\")")
	-- print2("print1(\"test\")")
	-- print1("test")
	while depth < 7 do
		if cmd then
			out = out.."$ print("
		else
			out = out..">src.lua print("
		end
		out = out..string.rep("\\", 2^depth - 1) .."\""
		cmd = not cmd
		depth = depth + 1
	end
	out = out ..text
	while depth > 0 do
		out = out .. string.rep("\\", 2^(depth - 1) - 1) .. "\")"
		depth = depth - 1
	end
	print(out)
end

function decide(text)
	assert(type(text) == "string", "Argument #1: String expected")
	assert(string.find(text, '?') == text:len(), "No question mark found at the end!")
	assert(string.len(text) > 8, "Too short question!")

	local rand = math.random()
	if rand < 0.35 then
		print(L.nick ..": Yes")
	elseif rand < 0.7 then
		print(L.nick ..": No")
	else
		print(L.nick ..": Maybe")
	end
end

function executionTime()
	local t = os.clock() - start_time
	print(L.nick .. ": Took " .. math.round(t * 1000, 2) .. " ms")
end

function coin(n)
	n = n or 1
	assert(type(n) == "number", "Argument #1: Number expected")
	assert(n < 1E5, "Number too large.")
	assert(n > 0, "Number too small.")

	local heads = 0
	for i = 1, n do
		if math.random() < 0.5 then
			heads = heads + 1
		end
	end

	print("Flipped one coin "..n.." times. It took "..
			math.round(math.random() * n / 20, 2).." minutes"..
			" to count the heads and finally got ".. heads..
			" of them. ("..  math.round(heads * 100 / n, 2) .."%)")
end
