-- Some stupid way to "encrypt" and "decrypt" text

local lookupstring = "abcdefghijklmnopqrstuvwxyz ,4).08_{<1$-;27?6!9:£(3}>5"
local LEN = string.len(lookupstring)

local function getIndex(chr)
	for i = 1, LEN do
		if lookupstring:sub(i, i) == chr then
			return i
		end
	end
	return 0
end

function fromB8C(text)
	assert(type(text) == "string", "Argument #1: String expected")
	assert(string.len(text) > 6, "Input text too short")

	local output = ""
	for i = 1, #text, 2 do
		local cL = text:sub(i, i)
		local cU = text:sub(i + 1, i + 1)
		local num = tonumber(cU..cL, 8)
		if num and num > 0 and num <= LEN then
			output = lookupstring:sub(num, num)..output
		end
	end
	print(L.nick ..": ".. output)
end

function toB8C(text)
	assert(type(text) == "string", "Argument #1: String expected")
	assert(string.len(text) > 6, "Input text too short")

	text = string.lower(text)
	local output = ""
	for i = #text, 1, -1 do
		local num = getIndex(text:sub(i, i))
		if num > 0 then
			num = string.format("%02o", num)
			local cL = num:sub(2, 2)
			local cU = num:sub(1, 1)

			output = output..cL..cU
		end
	end
	print(L.nick ..": ".. output)
end