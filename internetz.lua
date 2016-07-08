local curl_lib, curl
local function download(url)
	assert(type(url) == "string", "Argument #1: String expected")

	if not curl_lib then
		curl_lib = require("luacurl")
		curl = curl_lib.new()
	end

	local chunks = {}
	local start_time = os.clock()

	curl:setopt(curl_lib.OPT_URL, url)
	curl:setopt(curl_lib.OPT_WRITEFUNCTION, function(param, buf)
		table.insert(chunks, buf)
		assert(os.clock() - start_time < 3, "Download took too long.")
		return #buf
	end)
	curl:setopt(curl_lib.OPT_PROGRESSFUNCTION, function(param, dltotal, dlnow)
		print('%', url, dltotal, dlnow)
	end)
	curl:setopt(curl_lib.OPT_NOPROGRESS, true)
	assert(curl:perform())

	return table.concat(chunks)
end

function loadScript(url)
	loadstring(download(url))()
end

function duckduckgo(text, result)
	assert(type(text) == "string", "Argument #1: String expected")
	result = result or 1
	assert(type(result) == "number", "Argument #2: Nil or Number expected")

	text = text:gsub("&", " and ")
	text = text:gsub("+", " ")
	local json = require("json")
	text = download("http://api.duckduckgo.com/?q="..text.."&format=json&no_redirect=1")
	local decoded = json.decode(text)
	local description, link

	if not link then
		if decoded.Redirect and decoded.Redirect ~= "" then
			description = "!Bang redirect"
			link = decoded.Redirect
		end
	end
	if not link then
		if decoded.Abstract and decoded.Abstract ~= "" then
			description = decoded.Abstract
			link = decoded.AbstractURL
		end
	end
	if not link then
		local topics = decoded.Results
		if topics and #topics > 0 then
			if result > #topics then
				result = #topics
			end
			description = topics[result].Text
			link = topics[result].FirstURL
		end
	end
	if not link then
		local topics = decoded.RelatedTopics
		if topics and #topics > 0 then
			if result > #topics then
				result = #topics
			end
			description = topics[result].Text
			link = topics[result].FirstURL
		end
	end
	if not link then
		error("No results? Try something else")
	end

	if string.len(description) + string.len(link) > 400 then
		description = description:sub(1, 400 - string.len(link))
	end
	print("[".. result .."] " .. description .. " ( " .. link .. " )")
end

function ddg(text, result)
	duckduckgo(text, result)
end