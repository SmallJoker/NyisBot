# Python Noob Generator by rubenwardy
-- License: WTFPL
-- See IRC bot using this: https://gist.github.com/rubenwardy/8863c20a75e4faad3562

-- Example:
--	a random team mate shot your mum also dat enemy scouted a
--	random gun and a random gun covered a random gun but also your
--	mum nuked dat arsehole also a random thingy ran it like a arsehole blew up a kid

-- Source: https://gist.github.com/rubenwardy/4cb29fedb7952e5a4cdf

-- Todo:
--  * Adjectives - crappy, fishy, rubbish, stupid
--  * a/an


local cons = {
	"oh yeah",
	"and",
	"also",
	"i don't know but",
	"but also",
	"and then",
	"but then",
	"but maybe",
	"also that",
	"but uh",
	"probs",
	"rekt",
	"yeah like",
	"like",
	"but you dont understand it was like",
	"",
	"wot",
	"shrekt"
}

local nouns = {
	"guy",
	"gun",
	"assassin",
	"enemy",
	"soldier",
	"dragon",
	"ninja",
	"player",
	"team mate",
	"xbox",
	"it",
	"playstation",
	"thingy",
	"hacker",
	"griefer",
	"noob",
	"mum",
	"arsehole",
	"butt",
	"kid",
	"playmobil",
	"pizza",
	"m8"
}

local verbs = {
	"ran",
	"shot",
	"blew up",
	"killed",
	"scouted",
	"covered",
	"nuked",
	"destroyed",
	"used",
	"fite"
}

local prefixes = {
	"the",
	"this",
	"a",
	"a random",
	"dat",
	"dem",
	"such a"
}

local function getrand(array)
	return array[math.ceil(math.random() * #array)]
end

local function getnoun()
	local noun = getrand(nouns)
	if noun == "it" then
		-- nothing
	elseif noun == "mum" then
		noun = "your mum"
	else
		noun = getrand(prefixes) .. " " .. noun
	end
	return noun
end

function gensentence()
	local noun1 = getnoun()
	local noun2 = getnoun()
	local verb = getrand(verbs)
	return noun1 .. " " .. verb .. " " .. noun2
end

function generate(length)
	if not length or type(length) ~= "number" then
		length = 3
	end
	
	if length > 6 then
		length = 6
	end
	
	local result = gensentence()
	for i = 1, length do
		result = result .. " " .. getrand(cons)
				.. " " .. gensentence()
	end
	
	print(result)
end

function credits()
	print("Original source: https://gist.github.com/rubenwardy/4cb29fedb7952e5a4cdf")
end