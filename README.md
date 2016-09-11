 NyisBot
=========

The source code of the IRC bot *Nyisorn*.
It uses LuaJIT to parse and execute Lua scripts which can be found in the `plugins` directory.

License: BSD 3-Clause


 Modules
=========
Modules are written in C# and have the file naming in the form `m_<NAME>.cs`.
Unlike with plugins it is possible to use callbacks which do not rely on entered chat commands:
Player joined, left or renamed.

 Currently available
---------------------
* **GitHub**: Checks for new commits in the repositories on its watchlist
* **Lua**: Executes an entered command using the plugins
* **lGame**: Liar game, more information at the very bottom of `HELP.txt`
* **Tell**: Keeps the entered message(s) and notifies the target user on join


 Plugins
=========
The Lua scripts allow to extend the bot's functions easily. 
Use modules for complex functions. This Lua implementation does not allow to use cache (yet).
In the file `plugins\PLUGIN_API.txt` you will find an explanation how 
to write Lua scripts and a function reference of all scripts.