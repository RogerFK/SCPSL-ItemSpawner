# SMOD2-ItemSpawner

# Usage

You can choose to directly use the files, or you can use the commands below, in the commands section (if you're lazy, it's in [this](https://www.youtube.com/watch?v=dQw4w9WgXcQ) link)

`items.txt` file format:

`RoomType:ItemType1, ItemType2...:Probability:Vector:Rotation` where RoomType comes from [this](https://github.com/Grover-c13/Smod2/wiki/Enum-Lists#roomtype) link and ItemTypes from [this](https://github.com/Grover-c13/Smod2/wiki/Enum-Lists#itemtype) link ; probability takes a float number (75.48 for example); and both Vector and Rotation take things like `5,1.48,10:1,0,0`

# Commands

## Server console
The command to fetch a new position is `newpos`. I felt this was the best way because you can easily bind it to any key by doing `cmdbind <key> .newpos` so you can choose a bunch of spawnpoints easily. These get added to the "NEWLIST", which you can access via the R.A.

## Remote Admin Console
*You can type `ITEMSPAWNER HELP [COMMAND]` for more info about one specific command. <> means it's mandatory, [] is optional*

 - **ITEMSPAWNER ADDCOINS <RoomType>** - Adds the coin spawned through the newpos command to a list you can later modify
 - **ITEMSPAWNER CLEARLIST** - Removes all the spawned coins positions that haven't been added to the NEWLIST
 - **ITEMSPAWNER NEWLIST [EDIT/REMOVE/CONFIRM]** - Displays the current list that will be added to the items.txt file, which you can modify
 - **ITEMSPAWNER SPAWNLIST [EDIT/REMOVE]** - Displays or modifies the current spawnlist, so you can modify it
 - **ITEMSPAWNER ROOMLIST** - Displays every RoomType in the game. Non-unique rooms like hallways will probably not work, tho.

# API for other plugins
This plugin implements Smod2 Piping, which can be used following this guide: https://github.com/Grover-c13/Smod2/wiki/Piping

Every piped method is inside the `Spawner.cs` class (read it, I'm not listing them here). You need Smod 3.4.0 anyways, but if you're not comfortable using piping, you can freely place the `Spawner.cs` class inside your project. If you don't give credit just don't remove the first lines comments, if you don't want to get in trouble.
