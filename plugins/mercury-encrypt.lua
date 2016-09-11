-- Yet another way to make text messages longer and more complicated, without any particular reason

local lookuptable = {
	"a", "b", "c", "d", "e", "f", "g", "h",
	"i", "j", "k", "l", "m", "n", "o", "p",
	"q", "r", "s", "t", "u", "v", "w", "x",
	"y", "z", "'0", "'1", "'2", "'3", "'4",
	"'5", "'6", "'7", "'8", "'9", " "
}

function fromME(text)
	assert(type(text) == "string", "Argument #1: String expected")

	local output = ""
	local splits = text:split(" ")
	for i, v in ipairs(splits) do
		local num = tonumber(v, 16)
		if num and num <= #lookuptable then
			output = lookuptable[num]..output
		end
	end

	print(L.nick ..": ".. output)
end

function toME(text)
	assert(type(text) == "string", "Argument #1: String expected")

	text = string.lower(text)
	local output = ""
	for c in text:gmatch(".") do
		local chr = string.byte(c)
		if c >= 'a' and c <= 'z' then
			chr = string.format("%X", chr - 0x60)
		elseif c >= '0' and c <= '9' then
			chr = string.format("%X", chr - 0x30 + 0x1B)
		elseif c == ' ' then
			chr = '25'
		end

		if type(chr) == "string" then
			output = chr .. " X " ..output
		end
	end
	print(L.nick ..": ".. output)
end