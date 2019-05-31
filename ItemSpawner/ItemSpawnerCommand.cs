using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Smod2;
using Smod2.API;
using Smod2.Commands;
using Smod2.EventHandlers;
using Smod2.Events;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemSpawner
{
	class ItemSpawnerCommand : ICommandHandler, IEventHandlerCallCommand
	{
		private readonly ItemSpawner plugin;

		private static List<PosVectorPair> spawnedCoins = new List<PosVectorPair>(50);

		private static List<SpawnInfo> addList = new List<SpawnInfo>(50);
		public ItemSpawnerCommand(ItemSpawner plugin)
		{
			this.plugin = plugin;
		}

		public string GetCommandDescription()
		{
			return "Commands to use the itemspawner command";
		}

		public string GetUsage()
		{
			return "\nYou can type ITEMSPAWNER HELP [COMMAND] for more info about one specific command. <> means it's mandatory, [] is optional\n" +
				   "ITEMSPAWNER ADDCOINS <RoomType> - Adds the coin spawned through the newpos command to a list you can later modify, then removes them from the map\n" +
				   "ITEMSPAWNER CLEARLIST - Removes all the spawned coins positions that haven't been added to the NEWLIST\n" +
				   "ITEMSPAWNER NEWLIST [EDIT/REMOVE/CONFIRM] - Displays the current list that will be added to the items.txt file, which you can modify\n" +
				   "ITEMSPAWNER SPAWNLIST [EDIT/REMOVE] - Displays or modifies the current spawnlist, so you can modify it\n" +
				   "ITEMSPAWNER ROOMLIST - Displays every RoomType in the game. Non-unique rooms like hallways will probably not work, tho.";
		}
		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (!plugin.enable)
			{
				return new string[] { "This plugin is currently disabled." };
			}
			if (sender is Player p)
			{
				if (!plugin.allowedranks.Contains(p.GetUserGroup().Name))
				{
					return new string[] { "You're not allowed to run this command." };
				}
			}
			if (args.Length == 0)
			{
				return new string[] { "Please, introduce a second argument", "<ADDCOINS/CLEARLIST/NEWLIST/SPAWNLIST/ROOMLIST>" };
			}
			switch (args[0].ToUpper())
			{
				#region HELP region
				case "HELP":
					if (args.Count() > 1)
					{
						switch (args[1].ToUpper())
						{
							case "ADDCOINS":
								return new string[] { "ITEMSPAWNER ADDCOINS <RoomType> - Adds the coins to spawn relatively to a roomtype from the list in ITEMSPAWNER ROOMLIST to a new list in ITEMSPAWNER NEWLIST so you can modify them one by one," +
									" then removes them from the map, and then you can use ITEMSPAWNER NEWLIST to modify their parameters (such as it's probability, etc.)." };
							case "CLEARLIST":
								return new string[] { "ITEMSPAWNER CLEARLIST - Removes all the spawned points positions" };
							case "NEWLIST":
								return new string[] { "ITEMSPAWNER NEWLIST - Displays the current list that will get added when you do ITEMSPAWNER NEWLIST CONFIRM",
									"ITEMSPAWNER NEWLIST EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]- Edits the element with it's id when those arguments are passed.\nExample: ITEMSPAWNER NEWLIST EDIT 2 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
									"ITEMSPAWNER NEWLIST REMOVE <id> - Removes the element at the given id",
									"ITEMSPAWNER NEWLIST CONFIRM - Saves the current list to items.txt" };
							case "SPAWNLIST":
								return new string[] { "ITEMSPAWNER SPAWNLIST - Displays the current spawnlist",
									"ITEMSPAWNER SPAWNLIST EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z] - Edits the element with it's id when those arguments are passed.\nExample: ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
									"ITEMSPAWNER SPAWNLIST REMOVE <id> - Removes the element at the given id" };
							case "ROOMLIST": // done
								return new string[] { "ITEMSPAWNER ROOMLIST - Displays every RoomType in the game. Non-unique rooms like hallways will not work as intended, tho." };
							default:
								return new string[] { GetUsage() };
						}
					}
					return new string[] { GetUsage() };
				#endregion
				case "CLEARLIST":
					spawnedCoins.Clear();
					return new string[] { "Cleared the list of spawned coins" };
				case "ADDCOINS":
					if(args.Count() < 2)
					{
						return new string[] { "Usage: ITEMSPAWNER ADDCOINS <RoomType> - Adds the coin spawned through the newpos command to a list you can later modify, then removes them from the map" };
					}
					if(!Enum.TryParse(args[1], out RoomType muhRoomType)){
						return new string[]{ "Introduce a valid RoomType." };
					}
					if(spawnedCoins.Count == 0)
					{
						return new string[] { "Currently, the spawned coin list is empty" };
					}
					Room muhRoom = Spawner.rooms.Where(x => x.RoomType.Equals(muhRoomType)).First();
					int lines = FileManager.ReadAllLines("./items.txt").Count();
					plugin.Info(spawnedCoins.Count.ToString());
					foreach(PosVectorPair pair in spawnedCoins)
					{
						lines++;
						addList.Add(new SpawnInfo(muhRoomType, lines, new ItemType[] { ItemType.COIN }, 100f, Spawner.GetRelativePosition(muhRoom, pair.position), Spawner.GetRelativeRotation(muhRoom, pair.rotation)));
					}
					spawnedCoins.Clear();
					return new string[] { "Added coins to the NEWLIST and cleared the list" };
				case "NEWLIST":
					if (args.Count() > 1)
					{
						#region Addlist Region
						switch (args[1].ToUpper())
						{
							case "CONFIRM":
								foreach (SpawnInfo finalSpawnInfo in addList)
								{
									FileManager.AppendFile(ItemsFileManager.SpawnInfoToStr(finalSpawnInfo), "./items.txt", true);
									ItemsFileManager.spawnlist.Add(finalSpawnInfo);
								}
								addList.Clear();
								return new string[] { "New spawns succesfully written to the file items.txt" };
							case "EDIT":
								if (args.Count() < 3)
								{
									return new string[] { "Usage: ITEMSPAWNER SPAWNLIST EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]\nExample: 'ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5'. Items CAN'T be separated with spaces." };
								}
								// Here comes the fun part.
								if (addList.Count == 0)
								{
									return new string[] { "There are no items in the NEWLIST." };
								}
								if (!int.TryParse(args[2], out int id))
								{
									return new string[] { "Please, enter a numerical ID." };
								}
								if (addList.Count < id || id < 1)
								{
									return new string[] { "Please, enter a valid ID." };
								}
								SpawnInfo spawnInfo = addList.ElementAt(id - 1);
								addList.RemoveAt(id - 1);
								string returningString = "Item with ID " + args[2];
								string[] editArgs = args.Skip(3).ToArray();
								for (int i = 0; i < editArgs.Count(); i++)
								{
									if (editArgs[i].ToUpper().StartsWith("ITEMS="))
									{
										string[] probablyItems = editArgs[i].Substring(6).Split(',');
										ItemType[] itemsToAdd = new ItemType[probablyItems.Count()];
										int j = 0;
										foreach (string item in probablyItems)
										{
											if (Enum.TryParse(item, out ItemType itemType))
											{
												itemsToAdd[j] = itemType;
												j++;
											}
										}
										if (j == 0)
										{
											returningString += "\nPlease, introduce valid items.";
										}
										spawnInfo.items = itemsToAdd.Take(j).ToArray();
										addList.Add(spawnInfo);
										addList = addList.OrderBy(x => x.line).ToList();
										returningString += "\nModified to use items " + ItemsFileManager.ParseItems(spawnInfo.items);
									}
									else if (editArgs[i].ToUpper().StartsWith("PROBABILITY="))
									{
										string prob = editArgs[i].Substring(12);
										if (float.TryParse(prob, out float probParsed))
										{
											spawnInfo.probability = probParsed;
											returningString += "\nModified to use probability " + prob;
										}
										else
										{
											returningString += "\nPlease, introduce a valid probability.";
										}
									}
									else if (editArgs[i].ToUpper().StartsWith("ROTATION="))
									{
										Vector vec = ParseRot(editArgs[i].Substring(9));
										if (vec != null)
										{
											spawnInfo.rotation = vec;
											returningString += "\nModified to use rotation " + vec;
										}
										else
										{
											returningString += "\nPlease introduce a valid rotation (X.XX,Y.YY,Z.ZZ)";
										}
									}
									else if (editArgs[i].ToUpper().StartsWith("POSITION="))
									{
										Vector vec = ParseRot(editArgs[i].Substring(9));
										if (vec != null)
										{
											spawnInfo.position = vec;
											returningString += "\nModified to use position " + vec;
										}
										else
										{
											returningString += "\nPlease introduce a valid position (X.XX,Y.YY,Z.ZZ)";
										}
									}
									else
									{
										returningString += "\nUnknown parameter: " + editArgs[i];
									}
								}
								return new string[] { Environment.NewLine + returningString };
							case "REMOVE":
								if (addList.Count == 0)
								{
									return new string[] { "There are no items in the NEWLIST." };
								}
								if (args.Count() < 2)
								{
									return new string[] { "Usage: ITEMSPAWNER REMOVE <id>" };
								}
								if (!int.TryParse(args[2], out int removeId))
								{
									return new string[] { "Please, enter a numerical ID." };
								}
								if (addList.Count < removeId)
								{
									return new string[] { "Please, enter a valid ID." };
								}
								addList.RemoveAt(removeId - 1);
								return new string[] { $"Item with ID {args[2]} successfully removed" };
						} 
						#endregion
					}
					else
					{
						if(addList.Count == 0)
						{
							return new string[] { "There are no items in the NEWLIST." };
						}
						string addListString = "List:\n";
						int i = 0;
						foreach (SpawnInfo spawnInfo in addList)
						{
							i++;
							addListString += Environment.NewLine + i + ":\n - Roomtype:" + spawnInfo.RoomType.ToString()
								+ "\n - Items: " + ItemsFileManager.ParseItems(spawnInfo.items)
								+ "\n - Probability: " + spawnInfo.probability.ToString()
								+ "\n - Position: " + spawnInfo.position.ToString()
								+ "\n - Rotation: " + spawnInfo.rotation.ToString();
						}
						return new string[] { addListString };
					}
					break; // Don't ask why I have to place this break here
				case "ROOMLIST":
					string retValue = "List of ROOMTYPES:\n";
					foreach (RoomType room in Enum.GetValues(typeof(RoomType)))
					{
						retValue += room.ToString() + ", ";
					}
					return new string[] { retValue };
				case "SPAWNLIST":
					if (args.Count() == 1)
					{
						// RoomType:ItemType, ItemType2...:Probability:Vector:Rotation
						string spawnlistString = "List:\n";
						int i = 0;
						foreach (SpawnInfo spawnInfo in ItemsFileManager.spawnlist)
						{
							i++;
							spawnlistString += Environment.NewLine +  i + ":\n - Roomtype:" + spawnInfo.RoomType.ToString()
								+ "\n - Items: " + ItemsFileManager.ParseItems(spawnInfo.items)
								+ "\n - Probability: " + spawnInfo.probability.ToString()
								+ "\n - Position: " + spawnInfo.position.ToString()
								+ "\n - Rotation: " + spawnInfo.rotation.ToString();
						}
						return new string[] { spawnlistString }; 
					}
					else switch(args[1].ToUpper())
						{
							case "EDIT":
								if (args.Count() < 3)
								{
									return new string[] { "Usage: ITEMSPAWNER SPAWNLIST EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]\nExample: 'ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5'. Items CAN'T be separated with spaces." };
								}
								// Here comes the fun part.
								if (ItemsFileManager.spawnlist.Count == 0)
								{
									return new string[] { "There are no items in the SPAWNLIST." };
								}
								if (!int.TryParse(args[2], out int id))
								{
									return new string[] { "Please, enter a numerical ID." };
								}
								if (ItemsFileManager.spawnlist.Count < id || id < 1)
								{
									return new string[] { "Please, enter a valid ID." };
								}
								SpawnInfo spawnInfoRef = ItemsFileManager.spawnlist.ElementAt(id - 1);
								SpawnInfo spawnInfo = new SpawnInfo(spawnInfoRef.RoomType, spawnInfoRef.line, spawnInfoRef.items, spawnInfoRef.probability, spawnInfoRef.position, spawnInfoRef.rotation);
								string returningString = "Item with ID " + args[2];
								string[] editArgs = args.Skip(3).ToArray();
								for (int i = 0; i < editArgs.Count(); i++)
								{
									if (editArgs[i].ToUpper().StartsWith("ITEMS="))
									{
										string[] probablyItems = editArgs[i].Substring(6).Split(',');
										ItemType[] itemsToAdd = new ItemType[probablyItems.Count()];
										foreach (string item in probablyItems)
										{
											if (Enum.TryParse(item, out ItemType itemType))
											{
												itemsToAdd.Append(itemType);
											}
										}
										if (itemsToAdd.Count() == 0)
										{
											returningString += "\nPlease, introduce valid items.";
										}
										spawnInfo.items = itemsToAdd;
										returningString += "\nModified to use items " + ItemsFileManager.ParseItems(spawnInfo.items);
									}
									else if (editArgs[i].ToUpper().StartsWith("PROBABILITY="))
									{
										string prob = editArgs[i].Substring(12);
										if (float.TryParse(prob, out float probParsed))
										{
											spawnInfo.probability = probParsed;
											returningString += "\nModified to use probability " + prob;
										}
										else
										{
											returningString += "\nPlease, introduce a valid probability.";
										}
									}
									else if (editArgs[i].ToUpper().StartsWith("ROTATION="))
									{
										Vector vec = ParseRot(editArgs[i].Substring(9));
										if (vec != null)
										{
											spawnInfo.rotation = vec;
											returningString += "\nModified to use rotation " + vec;
										}
										else
										{
											returningString += "\nPlease introduce a valid rotation (X.XX,Y.YY,Z.ZZ)";
										}
									}
									else if (editArgs[i].ToUpper().StartsWith("POSITION="))
									{
										Vector vec = ParseRot(editArgs[i].Substring(9));
										if (vec != null)
										{
											spawnInfo.position = vec;
											returningString += "\nModified to use position " + vec;
										}
										else
										{
											returningString += "\nPlease introduce a valid position (X.XX,Y.YY,Z.ZZ)";
										}
									}
									else
									{
										returningString += "\nUnknown parameter: " + editArgs[i];
									}
								}
								ItemsFileManager.UpdateSpawnInfo(spawnInfoRef, spawnInfo);
								ItemsFileManager.spawnlist.Add(spawnInfo);
								ItemsFileManager.spawnlist.Remove(spawnInfoRef);
								ItemsFileManager.spawnlist = ItemsFileManager.spawnlist.OrderBy(x => x.line).ToList();
								return new string[] { Environment.NewLine + returningString };
							case "REMOVE":
								if (ItemsFileManager.spawnlist.Count == 0)
								{
									return new string[] { "There are no items in the Spawnlist." };
								}
								if (args.Count() < 2)
								{
									return new string[] { "Usage: ITEMSPAWNER REMOVE <id>" };
								}
								if (!int.TryParse(args[2], out int removeId))
								{
									return new string[] { "Please, enter a numerical ID." };
								}
								if (ItemsFileManager.spawnlist.Count < removeId || removeId < 1)
								{
									return new string[] { "Please, enter a valid ID." };
								}
								ItemsFileManager.DelSpawnInfo(ItemsFileManager.spawnlist.ElementAt(removeId - 1));
								return new string[] { $"Item in line {args[2]} successfully removed" };
						}
					return new string[] { GetUsage() };
			}
			return new string[] { GetUsage() };
		}

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			if (ev.Command.StartsWith("newpos"))
			{
				if (!plugin.allowedranks.Contains(ev.Player.GetUserGroup().Name))
				{
					ev.ReturnMessage = "You can't use this command.";
					return;
				}
				var scp049Component = ((GameObject)ev.Player.GetGameObject()).GetComponent<Scp049PlayerScript>();
				var scp106Component = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>();
				plugin.Info(scp049Component.plyCam.transform.forward.ToString());
				Vector3 plyRot = scp049Component.plyCam.transform.forward;
				Physics.Raycast(scp049Component.plyCam.transform.position, plyRot, out RaycastHit where, 40f, scp106Component.teleportPlacementMask);
				if (where.point.Equals(Vector3.zero))
				{
					ev.ReturnMessage = "Failed to spawn the coin. Try another place.";
				}
				else
				{
					Vector rotation = new Vector(-plyRot.x, plyRot.y, -plyRot.z), position = Spawner.Vec3ToVector(where.point) + (Vector.Up * 0.1f);
					PluginManager.Manager.Server.Map.SpawnItem(ItemType.COIN, position, rotation);
					spawnedCoins.Add(new PosVectorPair(position, rotation));
					plugin.Info(spawnedCoins.Count.ToString());
					Room room = ClosestRoom(where.point);
					ev.ReturnMessage = "Added " + where.point.ToString() + " to the list."
						+ "\nYou're probably looking for the RoomType: " + room.RoomType.ToString();
				}
			}
		}
		public Room ClosestRoom(Vector3 yourpos)
		{
			float closestDist = 10000f;
			Room room = null;
			foreach (Room r in Spawner.rooms)
			{
				float curDist = Vector.Distance(Spawner.Vec3ToVector(yourpos), r.Position);
				if (curDist < closestDist)
				{
					closestDist = curDist;
					room = r;
				}
			}
			return room;
		}
		private Vector ParseRot(string vectorData)
		{
			string[] vector = vectorData.Split(',');
			if (vector.Length != 3)
			{
				plugin.Info("Bad format for a vector (" + vectorData + ')');
				return null;
			}
			if (!float.TryParse(vector[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
				|| !float.TryParse(vector[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
				|| !float.TryParse(vector[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
			{
				plugin.Info("Error parsing vector: (" + vectorData + ')');
				return null;
			}
			return new Vector(x, y, z);
		}
	}
}
