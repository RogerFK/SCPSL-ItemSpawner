# SMOD2-ItemSpawner

# Latest release

To get the latest release, head over to https://github.com/RogerFK/SMOD2-ItemSpawner/releases/latest.

# Usage

Video of me explaining it with my bad spaniard accent: https://www.youtube.com/watch?v=OYSSO26tmHA

You can choose to directly use the files, or you can use the commands below, in the commands section (if you're lazy, it's in [this](https://www.youtube.com/watch?v=dQw4w9WgXcQ) link)

`items.txt` file format:

`RoomType:ItemType1, ItemType2...:Probability:Vector:Rotation` where RoomType comes from [this](https://github.com/Grover-c13/Smod2/wiki/Enum-Lists#roomtype) link and ItemTypes from [this](https://github.com/Grover-c13/Smod2/wiki/Enum-Lists#itemtype) link ; probability takes a float number (75.48 for example); and both Vector and Rotation take things like `5,1.48,10:1,0,0`

# Commands

## Server console
The command to fetch a new position is `newpos`. I felt this was the best way because you can easily bind it to any key by doing `cmdbind <key> .newpos` so you can choose a bunch of spawnpoints easily. These get added to the "NEWLIST", which you can access via the R.A.

## Remote Admin Console
*You can type `ITEMSPAWNER HELP [COMMAND]` for more info about one specific command, or just the command without any other argument. <> means it's mandatory, [] is optional*

 - `ITEMSPAWNER ADDCOINS <RoomType>` - Adds the coin spawned through the newpos command to a list you can later modify
 - `ITEMSPAWNER CLEARLIST` - Removes all the spawned coins positions that haven't been added to the NEWLIST
 - `ITEMSPAWNER NEWLIST [EDIT/REMOVE/CONFIRM] <ID> <Parameters>` - Displays the current list that will be added to the items.txt file, which you can modify
 - `ITEMSPAWNER SPAWNLIST [EDIT/REMOVE]` - Displays or modifies the current spawnlist, so you can modify it
 - `ITEMSPAWNER ROOMLIST` - Displays every RoomType in the game. Non-unique rooms like hallways will probably not work, tho.

# Configs
| Config Option | Value Type | Default Value | Description |
|:----------------:|:----------:|:-------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------:|
| is_enable | bool | true | Enables/disables this plugin completely |
| is_allowedranks | Rank list | owner, admin | Who can use this plugin, you shouldn't really give this to mods, tbh, instead tell them to download this plugin and tell them to do it in their machines |
| is_verbose | bool | true | Prints info in the console about spawned stuff and some other stuff |
| is_use_global_items | bool | true | Reads from items.txt globally or in a per-server basis (or using the items.txt inside the appdata folder if you're hosting one server) |

# API for other plugins
This plugin implements Smod2 Piping, which can be used following this guide: https://github.com/Grover-c13/Smod2/wiki/Piping

Every piped method is inside the `Spawner.cs` class (read it, I'm not listing them here). You need Smod 3.4.0 anyways, but if you're not comfortable using piping, you can freely place the `Spawner.cs` class inside your project. If you don't give credit just don't remove the first lines comments, if you don't want to get in trouble.
