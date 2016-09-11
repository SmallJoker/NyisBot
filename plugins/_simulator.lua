-- IRC conditions simulator for the Lua command line

N = {
	{ nick = "TestNick1", hostmask = "TestMask1" },
	{ nick = "TestNick2", hostmask = "TestMask2" }
}

L = {}
L.online = #N
L.nick = N[1].nick
L.hostmask = N[1].hostmask

dofile("security.lua")
