# SCPSL-ItemSpawner

# Latest release

To get the latest release, head over to https://github.com/RogerFK/SMOD2-ItemSpawner/releases/latest.

# Discord
## In case you want to get pinged about my stuff and all of that, hop on my Discord: https://discord.gg/MQdRQT2

# Usage

Video of me explaining it with my bad spaniard accent: https://www.youtube.com/watch?v=OYSSO26tmHA

You can choose to directly use the files, or you can use the commands below, in the commands section (if you're lazy, it's in [this](https://www.youtube.com/watch?v=dQw4w9WgXcQ) link)

`items.txt` file format:

`RoomType:ItemType1, ItemType2...:Probability:Vector3:Rotation` where RoomType comes from [this](https://github.com/Grover-c13/Smod2/wiki/Enum-Lists#roomtype) link and ItemTypes from [this](https://github.com/Grover-c13/Smod2/wiki/Enum-Lists#itemtype) link; probability takes a float number (75.48 for example); and both Vector3 and Rotation take things like `5,1.48,10:1,0,0`

If you're using the ItemManager version, to use the items you must type `"IM_XXX"` where XXX is the ID of the Custom Item you want to spawn. That is, instead of typing something like `IS NL E 1 items=COM15` you'd have to do `IS NL E 1 items=IM:101` for the HS8 shotgun, for example; same goes for the .txt file, `COIN, IM_101, IM_105` will have a 33% probability of spawning any of these.

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
