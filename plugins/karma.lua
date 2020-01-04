local sqlite3 = require("lsqlite3")
local db = sqlite3.open("karma.sqlite3")
local block_addmarma = false
local block_getkarma = false

db:exec[[
 CREATE TABLE karma (
	id			INTEGER PRIMARY KEY,
	name		TEXT,
	amount		INTEGER
 );
]]

local function lsqlite_exec(query)
	local status = db:exec(query)
	if status ~= 0 then
        error("SQL: ".. db:error_message())
	end
end

local function getRow(query)
	for v in db:rows(query) do
		return v
	end
	return nil
end

function karma.get(nick)
	if not nick then
		nick = L.nick
	else
		local in_room = getUserObject(nick)
		if in_room then
			nick = in_room.nick
		end
	end

	if block_getkarma then
		error("Flood detected.")
	end
	block_getkarma = true

	local result = getRow("SELECT amount FROM karma WHERE name = '".. string.lower(nick) .."' LIMIT 1")
	if not result then
		throwError(404, "No karma found.")
	else
		print(L.nick ..": Karma of ".. nick ..": ".. c(result[1]))
	end
end

function karma.up(nick)
	if getUserstatus(L.nick) ~= 3 then
		throwError(401, "Authentification required.")
		return
	end

	local in_room = getUserObject(nick or L.nick)
	if not in_room then
		throwError(404, "That nickname was not found in this channel.")
		return
	end
	nick = in_room.nick

	if block_addkarma then
		error("Flood detected.")
	end
	block_addkarma = true

	if nick == L.nick then
		throwError(403, "You can not add karma to yourself!")
		return
	end

	local nick_l = string.lower(nick)
	local karma = nil
	local result = getRow("SELECT amount FROM karma WHERE name = '".. nick_l .."' LIMIT 1")

	if not result then
		lsqlite_exec("INSERT INTO karma VALUES (NULL, '".. nick_l .."', 1)")
		karma = 1
	else
		lsqlite_exec("UPDATE karma SET amount = amount + 1 WHERE name = '".. nick_l .."'")
		karma = result[1] + 1
	end

	print(L.nick ..": Karma level of ".. nick .." is now at ".. c(karma)..".")
end

function karma.down(nick)
	if not (L.nick == "Krock" and getUserstatus(L.nick) == 3) then
		throwError(403, "You are not authorized to use this command.")
		return
	end

	local in_room = getUserObject(nick or L.nick)
	if not in_room then
		throwError(404, "That nickname was not found in this channel.")
		return
	end
	nick = in_room.nick

	local nick_l = string.lower(nick)
	local karma = nil
	local result = getRow("SELECT amount FROM karma WHERE name = '".. nick_l .."' LIMIT 1")

	if not result then
		lsqlite_exec("INSERT INTO karma VALUES (NULL, '".. nick_l .."', -1)")
		karma = -1
	else
		lsqlite_exec("UPDATE karma SET amount = amount - 1 WHERE name = '".. nick_l .."'")
		karma = result[1] - 1
	end

	print(L.nick ..": Karma level of ".. nick .." is now at ".. c(karma)..".")
end

function karma.credits()
	print("Created by Krock (C) 2016, using the lsqlite3 library")
end
