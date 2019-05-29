using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;

namespace ItemSpawner
{
	public class ItemFileReader : IEventHandlerWaitingForPlayers
	{
		private readonly ItemSpawner plugin;
		private readonly Random rand = new Random();

		public ItemFileReader(ItemSpawner plugin)
		{
			this.plugin = plugin;
		}
		private Vector VectorParser(string vectorData, int line)
		{
			string[] vector = vectorData.Split(',');
			if (vector.Length != 3)
			{
				plugin.Info("Bad format for a vector (" + vectorData + ") in line " + line);
				return null;
			}
			if	(	!float.TryParse(vector[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) 
				||	!float.TryParse(vector[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) 
				||	!float.TryParse(vector[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
			{
				plugin.Info("Error parsing vector: (" + vectorData + ") in line " + line);
				return null;
			}
			return new Vector(x, y, z);
		}
		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (!FileManager.FileExists("./items.txt"))
			{
				plugin.Info("Created items.txt file with a microhid (or a coin) in the Intercom room and one at the Silo warhead as an example.");
				File.WriteAllText("./items.txt", "NUKE:MICROHID:100:-0.05,402.46,3.52:1,0,0\nINTERCOM:MICROHID,COIN:100:-9.212725,-6.839905,-3.935197:0.5,0,0");
			}
			List<SpawnInfo> spawnlist = new List<SpawnInfo>();
			string[] items = FileManager.ReadAllLines("./items.txt");
			if (items.Length < 0)
			{
				plugin.Error("Your 'items.txt' file is completely blank.");
				return;
			}
			else
			{
				int currentLine = 0;
				foreach (string item in items)
				{
					currentLine++;

					if (string.IsNullOrWhiteSpace(item))
					{
						continue;
					}

					if (item[0] == '#')
					{
						continue;
					}

					try
					{
						// RoomType:ItemType, ItemType2...:Probability:Vector:Rotation
						string[] data = item.Split(':');
						if (data.Length == 0)
						{
							continue;
						}
						if (data.Length != 5)
						{
							plugin.Info("Error reading line " + currentLine);
							continue;
						}
						if (!Enum.TryParse(data[0].Trim(), out RoomType room))
						{
							plugin.Info("Error using RoomType " + data[0] + " in line " + currentLine);
							continue;
						}
						string[] itemData = data[1].Split(',');
						List<ItemType> itemTypes = new List<ItemType>();
						if (itemData.Length == 0)
						{
							plugin.Info("Error fetching ItemTypes " + data[1] + " in line " + currentLine);
							continue;
						}
						foreach (string itemDataValue in itemData)
						{
							if (!Enum.TryParse(itemDataValue.Trim(), out ItemType itemType))
							{
								plugin.Info("Error using ItemType " + itemDataValue.Trim() + " in line " + currentLine);
								continue;
							}
							itemTypes.Add(itemType);
						}
						if (!float.TryParse(data[2].Trim(), out float probability))
						{
							plugin.Info("Error using probability " + data[2].Trim() + " in line " + currentLine);
							continue;
						}
						Vector position = VectorParser(data[3].Trim(), currentLine);
						if (position == null)
						{
							continue;
						}
						Vector rotation = VectorParser(data[4].Trim(), currentLine);
						if (rotation == null)
						{
							continue;
						}

						// If it worked until here means everything went to plan uwu
						spawnlist.Add(new SpawnInfo(room, currentLine, itemTypes.ToArray(), probability, position, rotation));
					}
					catch (Exception e)
					{
						plugin.Info("Somewhere it fucked up: " + e.ToString());
					}
				}
			}
			if (spawnlist.Count != 0)
			{
				foreach (Room room in Spawner.rooms)
				{
					foreach (SpawnInfo spawn in spawnlist.Where(x => x.RoomType == room.RoomType))
					{
						if (rand.Next(0, 10000) <= spawn.probability * 100)
						{
							Spawner.SpawnItem(room, spawn.items[rand.Next(0, spawn.items.Length - 1)], spawn.position, spawn.rotation);
						}
					}
				}
			}
		}
		public void DelSpawnInfo(SpawnInfo spawnInfo)
		{
			// I don't know if this works
			FileManager.ReplaceLine(spawnInfo.line, "", "./items.txt");
		}
		public void UpdateSpawnInfo(SpawnInfo spawnInfo)
		{
			// This causes an exception if the any retard removes the items.txt file
			FileManager.ReplaceLine(spawnInfo.line, SpawnInfoToStr(spawnInfo), "./items.txt");
		}
		public string SpawnInfoToStr(SpawnInfo spawnInfo)
		{
			return spawnInfo.RoomType.ToString() + ':' + string.Join(",", spawnInfo.items.ToString()) + ':' + spawnInfo.probability +
						':' + spawnInfo.position.x.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.position.y.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.position.z.ToString(CultureInfo.InvariantCulture) +
						':' + spawnInfo.rotation.x.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.rotation.y.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.rotation.z.ToString(CultureInfo.InvariantCulture);
		}
	}
}
