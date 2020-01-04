local morsealphabet = {
	["a"] = ".-",
	["b"] = "-...",
	["c"] = "-.-.",
	["d"] = "-..",
	["e"] = ".",
	["f"] = "..-.",
	["g"] = "--.",
	["h"] = "....",
	["i"] = "..",
	["j"] = ".---",
	["k"] = "-.-",
	["l"] = ".-..",
	["m"] = "--",
	["n"] = "-.",
	["o"] = "---",
	["p"] = ".--.",
	["q"] = "--.-",
	["r"] = ".-.",
	["s"] = "...",
	["t"] = "-",
	["u"] = "..-",
	["v"] = "...-",
	["w"] = ".--",
	["x"] = "-..-",
	["y"] = "-.--",
	["z"] = "--..",
	[" "] = "/"
}
local reversemorsealphabet = {}
for k,v in pairs(morsealphabet) do
	reversemorsealphabet[v] = k
end

function morse.fromText(x)
	x = string.lower(x)

	local morsed = ""
	for c in x:gmatch(".") do
		if morsealphabet[c] then
			morsed = morsed .. morsealphabet[c] .. " "
		end
	end
	print(morsed)
end

function morse.toText(x)
	local text = x:split(' ')

	local demorsed = "";
	for i = 1, #text do
		local cache = reversemorsealphabet[text[i]]
		if cache then
			demorsed = demorsed .. cache
		end
	end
	print(demorsed)
end

function morse.credits()
	print("Original source: https://github.com/trfunk/lua/blob/master/dailyprogrammer%20%28reddit%29/%5B7%5Dmorsecode.lua")
end