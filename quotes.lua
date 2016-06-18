local require = P.x_loadlib
local sqlite3 = require("lsqlite3")
local db = sqlite3.open("quotes.sqlite3")
local block_addquote = false
local block_getquote = false

db:exec[[
 CREATE TABLE quotes (
	id			INTEGER PRIMARY KEY,
	writer		TEXT,
	content		TEXT
 );
]]

local function isAdmin()
	if L.nick == "Krock" and getUserstatus(L.nick) == 3 then
		return true
	end
	return false
end

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

function addQuote(text)
	if not text or string.len(text) < 10 then
		print(L.nick ..": Too short message")
		return
	end
	if getUserstatus(L.nick) ~= 3 then
		throwError(401, "Authentification required.")
		return
	end
	if block_addquote then
		error("Flood detected.")
	end
	block_addquote = true
	
	text = text:gsub("'", "&#39;")
	
	local results = {}
	for v in db:rows("SELECT id FROM quotes WHERE content LIKE '%".. text .."%' LIMIT 1") do
		table.insert(results, v[1])
	end
	
	if #results > 0 then
		print(L.nick ..": Found similar quote, #".. results[1])
		return
	end
	
	lsqlite_exec("INSERT INTO quotes VALUES (NULL, '".. L.nick .."', '".. text .."')")
	local row = getRow("SELECT id FROM quotes WHERE content = '".. text .."' LIMIT 1")
	print(L.nick ..": Added quote #".. row[1])
end

function getQuote(text)
	local results = {}
	if type(text) == "number" then
		results[1] = getRow("SELECT * FROM quotes WHERE id = ".. text .." LIMIT 1")
	else
		for v in db:rows("SELECT * FROM quotes WHERE content LIKE '%".. text .."%'") do
			table.insert(results, v)
		end
	end
	if block_getquote then
		error("Flood detected.")
	end
	block_getquote = true
	
	if #results == 1 then
		local quote = results[1][3]:gsub("&#39;", "'")
		print(c("[#".. results[1][1] .."] ").. quote)
	elseif #results == 0 then
		throwError(404, "No matching quote found.")
	else
		local numbers = ""
		for i, v in ipairs(results) do
			numbers = numbers .. v[1] ..", "
		end
		print("Found following matching quotes: ".. numbers)
	end
end

function removeQuote(id)
	if not id or type(id) ~= "number" then
		error("Argument #1: Number expected.")
	end
	if getUserstatus(L.nick) ~= 3 then
		throwError(403, "Authentification required.")
		return
	end
	
	local quote = getRow("SELECT writer FROM quotes WHERE id = ".. id .." LIMIT 1")
	if not quote then
		throwError(404, "Quote not found")
		return
	end
	
	if L.nick == quote[1] or isAdmin() then
		lsqlite_exec("DELETE FROM quotes WHERE id = ".. id)
		print(L.nick ..": Deleted!")
	else
		throwError(403, "Is is your quote? Ask the bot admin to remove it.")
	end
end

function fuckyouiamadmin(text, dump_it)
	--DELETE * FROM quotes
	if not isAdmin() then
		throwError(403, "No, you're not an admin!")
		return
	end
	
	if dump_it then
		print(dump(getRow(text)))
	else
		lsqlite_exec(text)
	end
end

function credits()
	print("Created by Krock (C) 2015-2016, using the lsqlite3 library")
end
