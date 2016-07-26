local curl_lib, curl
local function download(url)
	assert(type(url) == "string", "Argument #1: String expected")

	if not curl_lib then
		curl_lib = require("luacurl")
		curl = curl_lib.new()
	end

	local chunks = {}
	local start_time = os.clock()

	curl:setopt(curl_lib.OPT_USERAGENT, "Mozilla/5.0 (NyisBot)")
	curl:setopt(curl_lib.OPT_URL, url)
	curl:setopt(curl_lib.OPT_CONNECTTIMEOUT, 4)
	curl:setopt(curl_lib.OPT_WRITEFUNCTION, function(param, buf)
		table.insert(chunks, buf)
		assert(os.clock() - start_time < 4, "Download took too long.")
		return #buf
	end)
	--curl:setopt(curl_lib.OPT_PROGRESSFUNCTION, function(param, dltotal, dlnow)
	--	print('%', url, dltotal, dlnow)
	--end)
	curl:setopt(curl_lib.OPT_NOPROGRESS, true)
	assert(curl:perform())

	return table.concat(chunks)
end

function loadScript(url)
	local script, err = loadstring(download(url))
	if not err then
		script()
	else
		print(err)
	end
end

function downloadPrint(url)
	local text = download(url)
	local len = string.len(text)
	print("Length: ".. len .. " |")
	if len > 255 then
		print(text:sub(1, 250) .. " ...")
	else
		print(text)
	end
end

function duckduckgo(text, result)
	assert(type(text) == "string", "Argument #1: String expected")
	result = result or 1
	assert(type(result) == "number", "Argument #2: Nil or Number expected")

	text = text:gsub("&", " and ")
	text = text:gsub("+", "%%2B")
	text = text:gsub(" ", "+")
	local json = require("json")
	text = download(
		"http://api.duckduckgo.com/?q="..text.."&format=json&no_redirect=1",
		"python-duckduckgo 0.242"
	)
	local decoded = json.decode(text)
	local topics
	local descriptions = {}
	local links = {}

	if decoded.Redirect and decoded.Redirect ~= "" then
		table.insert(descriptions, "!Bang redirect")
		table.insert(links, decoded.Redirect)
	end

	topics = decoded.Results
	if topics and #topics > 0 then
		for i,v in ipairs(topics) do
			table.insert(descriptions, v.Text)
			table.insert(links, v.FirstURL)
		end
	end

	topics = decoded.RelatedTopics
	if topics and #topics > 0 then
		for i,v in ipairs(topics) do
			table.insert(descriptions, v.Text)
			table.insert(links, v.FirstURL)
		end
	end

	if decoded.Abstract and decoded.Abstract ~= "" then
		table.insert(descriptions, decoded.Abstract)
		table.insert(links,  decoded.AbstractURL)
	end

	if #links == 0 then
		error("No results? Try something else")
	end
	if result > #links then
		result = #links
	end

	local description = descriptions[result]
	local link = links[result]

	if string.len(description) + string.len(link) > 400 then
		description = description:sub(1, 400 - string.len(link))
	end
	print("[".. result .."] " .. description .. " ( " .. link .. " )")
end

function ddg(text, result)
	duckduckgo(text, result)
end