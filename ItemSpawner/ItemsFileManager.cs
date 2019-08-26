using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;

namespace ItemSpawner
{
	public class ItemsFileManager : IEventHandlerWaitingForPlayers, IEventHandlerRoundStart
	{
		private readonly ItemSpawnerPlugin plugin;
		private readonly Random rand = new Random();
		private Queue<CusItemInfo> CIQueue = new Queue<CusItemInfo>();
		public ItemsFileManager(ItemSpawnerPlugin plugin)
		{
			this.plugin = plugin;
		}
		private Vector VectorParser(string vectorData, int line = 0)
		{
			string[] vector = vectorData.Split(',');
			if (vector.Length != 3)
			{
				plugin.Info("Bad format for a vector (" + vectorData + (line > 0 ? ") in line " + line : ""));
				return null;
			}
			if (!float.TryParse(vector[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
				|| !float.TryParse(vector[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
				|| !float.TryParse(vector[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
			{
				plugin.Info("Error parsing vector: (" + vectorData + (line > 0 ? ") in line " + line : ""));
				return null;
			}
			return new Vector(x, y, z);
		}
		public static List<SpawnInfo> spawnlist = new List<SpawnInfo>();
		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (!plugin.enable)
			{
				if (plugin.verbose)
					plugin.Info("ItemSpawner is disabled: the list was not read");
				return;
			}
			string[] items;

			if (plugin.useGlobalItems)
			{
				if (!FileManager.FileExists("./items.txt"))
				{
					plugin.Info("Created items.txt file with a MTF_LIEUTENANT_KEYCARD (or a coin) in the Intercom room and one MICROHID at the Silo warhead as an example in the global directory.");
					File.WriteAllText("./items.txt", "NUKE:MICROHID:100:-0.05,402.46,3.52:1,0,0\nINTERCOM:MICROHID,COIN:100:-9.212725,-6.839905,-3.935197:0.5,0,0");
				}
				items = FileManager.ReadAllLines("./items.txt");
			}
			else
			{
				if (!FileManager.FileExists(FileManager.GetAppFolder() + ("items.txt")))
				{
					plugin.Info("Created items.txt file with a MTF_LIEUTENANT_KEYCARD (or a coin) in the Intercom room and one MICROHID at the Silo warhead as an example in the server directory or in the Appdata folder.");
					File.WriteAllText(FileManager.GetAppFolder() + ("items.txt"), "NUKE:MICROHID:100:-0.05,402.46,3.52:1,0,0\nINTERCOM:MTF_LIEUTENANT_KEYCARD,COIN:100:-9.212725,-6.839905,-3.935197:0.5,0,0");
				}
				items = FileManager.ReadAllLines(FileManager.GetAppFolder() + ("items.txt"));
			}
			if (items.Length < 0)
			{
				plugin.Error("Your 'items.txt' file is completely blank.");
				return;
			}
			else
			{
				int currentLine = -1;
				spawnlist.Clear(); // Reload the spawnlist
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
							plugin.Error("Error reading line " + (currentLine+1));
							continue;
						}
						if (!Enum.TryParse(data[0].Trim(), out RoomType room))
						{
							plugin.Error("Error using RoomType " + data[0] + " in line " + (currentLine + 1));
							continue;
						}
						string[] itemData = data[1].Split(',');
						List<ItemType> itemTypes = new List<ItemType>();
						List<int> CustomItems = new List<int>();
						if (itemData.Length == 0)
						{
							plugin.Error("Error fetching ItemTypes " + data[1] + " in line " + currentLine);
							continue;
						}
						foreach (string itemDataUntrimmed in itemData)
						{
							string itemDataValue = itemDataUntrimmed.Trim();
							#region ItemManager Region
							if (itemDataValue.StartsWith("IM_"))
							{
								if (int.TryParse(itemDataValue.Substring(3), out int customItem))
								{
									if (ItemManager.Items.Handlers.ContainsKey(customItem))
									{
										CustomItems.Add(customItem);
									}
									else
									{
										plugin.Error("Custom item with ID " + customItem + " not installed/not found!");
									}
								}
								else
								{
									plugin.Error("\"ID\" " + itemDataValue.Substring(3) + " isn't a valid ID for ItemManager!");
								}
							}
							else
							#endregion
							if (!Enum.TryParse(itemDataValue.Trim(), out ItemType itemType))
							{
								if (int.TryParse(itemDataValue.Trim(), out int id))
								{
									itemType = (ItemType)id;
									itemTypes.Add((ItemType)id);
								}
								else plugin.Error("Error using ItemType " + itemDataValue.Trim() + " in line " + currentLine);
							}
							else itemTypes.Add(itemType);
						}
						if (!float.TryParse(data[2].Trim(), out float probability))
						{
							plugin.Error("Error using probability " + data[2].Trim() + " in line " + currentLine);
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
						spawnlist.Add(new SpawnInfo(room, currentLine, itemTypes.ToArray(), CustomItems.ToArray(), probability, position, rotation));
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
							if (spawn.CustomItems != null)
							{
								int itemInt = rand.Next(0, spawn.items.Length + spawn.CustomItems.Length);
								if (itemInt < spawn.items.Length)
								{
									Spawner.SpawnItem(room, spawn.items[itemInt], spawn.position, spawn.rotation);
								}
								else
								{
									CIQueue.Enqueue(new CusItemInfo(room, spawn.CustomItems[itemInt - spawn.items.Length], spawn.position, spawn.rotation));
								}
							}
							else Spawner.SpawnItem(room, spawn.items[rand.Next(0, spawn.items.Length)], spawn.position, spawn.rotation);
						}
					}
				}
			}
		}
		public static string ParseItems(ItemType[] items, int[] customItems)
		{
			int i, size1 = 0, size2 = 0;
			if(items != null)
			{
				size1 = items.Length;
			}
			if(customItems != null)
			{
				size2 = customItems.Length;
			}
			string parsedValue = string.Empty;
			for (i = 0; i < size1; i++)
			{
				parsedValue += items[i] + (i != size1 + size2 - 1 ? ", " : string.Empty);
			}
			for (i = 0; i < size2; i++)
			{
				parsedValue += "IM_" + customItems[i] + (i != size2 - 1 ? ", " : string.Empty);
			}
			return parsedValue;
		}
		public static void DelSpawnInfo(SpawnInfo spawnInfo)
		{
			string oldStr = FileManager.ReadAllLines("./items.txt").ElementAt(spawnInfo.line);
			FileManager.ReplaceLine(spawnInfo.line, "# Deleted SpawnInfo: " + oldStr, "./items.txt");
			spawnlist.Remove(spawnInfo);
		}
		public static void UpdateSpawnInfo(SpawnInfo oldSpawnInfo, SpawnInfo newSpawnInfo)
		{
			// This causes an exception if any retard removes the items.txt file
			FileManager.ReplaceLine(oldSpawnInfo.line, SpawnInfoToStr(newSpawnInfo), "./items.txt");
		}
		public static string SpawnInfoToStr(SpawnInfo spawnInfo)
		{
			return spawnInfo.RoomType.ToString() + ':' + ParseItems(spawnInfo.items, spawnInfo.CustomItems) + ':' + spawnInfo.probability +
						':' + spawnInfo.position.x.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.position.y.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.position.z.ToString(CultureInfo.InvariantCulture) +
						':' + spawnInfo.rotation.x.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.rotation.y.ToString(CultureInfo.InvariantCulture) +
						',' + spawnInfo.rotation.z.ToString(CultureInfo.InvariantCulture);
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			while(CIQueue.Count != 0)
			{
				CusItemInfo info = CIQueue.Dequeue();
				Spawner.SpawnCustomItem(info.room, info.id, info.position, info.rotation);
			}
		}
	}

	internal class CusItemInfo
	{
		public Room room;
		public int id;
		public Vector position;
		public Vector rotation;

		public CusItemInfo(Room room, int id, Vector position, Vector rotation)
		{
			this.room = room;
			this.id = id;
			this.position = position;
			this.rotation = rotation;
		}
	}
}
