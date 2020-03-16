using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using UnityEngine;
using Mirror;

namespace ItemSpawner
{
	class ItemSpawnerCommand : ICommandHandler, IEventHandlerCallCommand
	{
		private readonly ItemSpawnerPlugin plugin;

		private static List<PosVector3Pair> spawnedCoins = new List<PosVector3Pair>(50);

		private static List<SpawnInfo> addList = new List<SpawnInfo>(50);
		public ItemSpawnerCommand(ItemSpawnerPlugin plugin)
		{
			this.plugin = plugin;
		}

		public string GetCommandDescription()
		{
			return "Commands to use the ItemSpawner Plugin";
		}

		public string GetUsage()
		{
			return "\nYou can type ITEMSPAWNER/ITEMS/ITS/IS HELP [COMMAND] for more info about one specific command. <> means it's mandatory, [] is optional\n" +
				   "ITEMSPAWNER +/AC/ADDC/ADDCOINS <RoomType> - Adds the coin positions in relation to the ROOMTYPE you typed. It then removes those coins to be taken into account when doing IS + again\n" +
				   "ITEMSPAWNER CL/REML/REMOVELIST/CLEARLIST - Removes all the spawned coins positions that haven't been added to the NEWLIST, useful if you spawned 300 coins somewhere but don't wanna use those positions\n" +
				   "ITEMSPAWNER NL/NEW/NLIST/NEWLIST [EDIT/REMOVE/CONFIRM] - Displays the current list that will be added to the items.txt file, which you can modify\n" +
				   "ITEMSPAWNER SL/SPL/SPAWNS/SPAWNLIST [EDIT/REMOVE] - Displays or modifies the current spawnlist, so you can modify it\n" +
				   "ITEMSPAWNER RL/ROOMS/ROOMLIST - Displays every RoomType in the game. Non-unique rooms like hallways will probably not work, so don't count on that.";
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
					if (args.Length > 1)
					{
						switch (args[1].ToUpper())
						{
							case "+":
							case "AC":
							case "ADDC":
							case "ADDCOINS":
							case "TONEWLIST":
								return new string[] { "ITS +/AC/ADDC/ADDCOINS <RoomType> - Adds the coins to spawn relatively to a roomtype from the list in ITEMSPAWNER ROOMLIST to a new list in ITEMSPAWNER NEWLIST so you can modify them one by one," +
									" then removes that coin position from the current list, and then you can use ITEMSPAWNER NEWLIST to modify their parameters (such as it's probability, etc.).\n<b>Please note: coins are visual queues to see more or less where the item will be spawned</b>" };
							case "CL":
							case "REML":
							case "REMOVELIST":
							case "CLEARLIST":
								return new string[] { "ITS CL/REML/REMOVELIST/CLEARLIST - Removes all the spawned coins positions" };
							case "NL":
							case "NEW":
							case "NLIST":
							case "NEWLIST":
								return new string[] { "ITS NL/NEW/NLIST/NEWLIST - Displays the current list that will get added when you do ITEMSPAWNER NEWLIST CONFIRM",
									"ITS NL E/ED/EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]- Edits the element with it's id when those arguments are passed.\nExample: ITEMSPAWNER NEWLIST EDIT 2 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
									"ITS NL -/R/REM/REMOVE <id> - Removes the element at the given id",
									"ITS NL C/CF/CON/CONFIRM - Saves the current list to items.txt" };
							case "SL":
							case "SPL":
							case "SPAWNS":
							case "SPAWNLIST":
								return new string[] { "ITS SL/SPL/SPAWNS/SPAWNLIST - Displays the current spawnlist",
									"ITS SL E/ED/EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z] - Edits the element with it's id when those arguments are passed.\nExample: ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
									"ITS SL -/R/REM/REMOVE <id> - Removes the element at the given id" };
							case "RL":
							case "ROOMS":
							case "ROOMLIST":
								return new string[] { "ITS RL/ROOMS/ROOMLIST - Displays every RoomType in the game. Non-unique rooms like hallways will not work as intended, tho." };
							default:
								return new string[] { GetUsage() };
						}
					}
					return new string[] { GetUsage() };
				#endregion
				case "CL":
				case "REML":
				case "REMOVELIST":
				case "CLEARLIST":
					spawnedCoins.Clear();
					return new string[] { "Cleared the list of spawned coins" };
				case "+":
				case "AC":
				case "ADDC":
				case "ADDCOINS":
				case "TONEWLIST":
					if (args.Length < 2)
					{
						return new string[] { "Usage: ITEMSPAWNER ADDCOINS <RoomType> - Adds the coin spawned through the newpos command to a list you can later modify, then removes them from the map" };
					}
					if(!Enum.TryParse(args[1].ToUpper(), out RoomType muhRoomType)){
						return new string[]{ "Introduce a valid RoomType." };
					}
					if(spawnedCoins.Count == 0)
					{
						return new string[] { "Currently, the spawned coin list is empty" };
					}
					Room muhRoom = Spawner.rooms.Where(x => x.RoomType.Equals(muhRoomType)).First();
					int lines = FileManager.ReadAllLines("./items.txt").Length - 1;
					foreach(PosVector3Pair pair in spawnedCoins)
					{
						addList.Add(new SpawnInfo(muhRoomType, lines, new ItemType[] { ItemType.COIN }, new int[] { }, 100f, Spawner.GetRelativePosition(muhRoom, pair.position), Spawner.GetRelativeRotation(muhRoom, pair.rotation)));
						lines++;
					}
					spawnedCoins.Clear();
					return new string[] { "Added coins to the NEWLIST and cleared the old list" };
				case "NL":
				case "NEW":
				case "NLIST":
				case "NEWLIST":
					if (args.Length > 1)
					{
						#region Newlist Region
						switch (args[1].ToUpperInvariant())
						{
							case "C":
							case "CF":
							case "CON":
							case "CONFIRM":
								foreach (SpawnInfo finalSpawnInfo in addList)
								{
									FileManager.AppendFile(ItemsFileManager.SpawnInfoToStr(finalSpawnInfo), "./items.txt", true);
									ItemsFileManager.spawnlist.Add(finalSpawnInfo);
								}
								addList.Clear();
								return new string[] { "New spawns succesfully written to the file items.txt" };
							case "E":
							case "ED":
							case "EDIT":
								if (args.Length < 3)
								{
									return new string[] { "Usage: IS " + args[0] + " " + args[1] + " <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]\nExample: 'ITEMSPAWNER " + args[0] + " " + args[1] + " 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5'. Items CAN'T be separated with spaces." };
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
								if(args.Length < 4)
								{
									return new string[] { "Please, introduce another argument." };
								}
								SpawnInfo spawnInfo = addList.ElementAt(id - 1);
								addList.RemoveAt(id - 1);
								string returningString = "Item with ID " + args[2];
								string[] editArgs = args.Skip(3).ToArray();
								for (int i = 0; i < editArgs.Length; i++)
								{
									if (editArgs[i].ToUpper().StartsWith("ITEMS="))
									{
										string[] probablyItems = editArgs[i].Substring(6).Split(',');
										ItemType[] itemsToAdd = new ItemType[probablyItems.Length];
										int[] customItemsToAdd = new int[probablyItems.Length];
										List<int> invalidCustomIds = new List<int>();
										int j = 0, z = 0;
										foreach (string item in probablyItems)
										{
											string itemDataValue = item.Trim();
											#region ItemManager Region
											if (item.StartsWith("IM_"))
											{
												if (int.TryParse(itemDataValue.Substring(3), out int customItem))
												{
													if (ItemManager.Items.Handlers.ContainsKey(customItem))
													{
														customItemsToAdd[z] = (customItem);
														z++;
													}
													else
													{
														invalidCustomIds.Add(customItem);
													}
												}
											}
											else  
											#endregion
											if (Enum.TryParse(item, out ItemType itemType))
											{
												itemsToAdd[j] = itemType;
												j++;
											}
											else if(int.TryParse(item, out int idParsed))
											{
												if (idParsed >= -1 && idParsed <= 30)
												{
													itemsToAdd[j] = (ItemType)idParsed;
													j++;
												}
											}
										}
										foreach (int ID in invalidCustomIds)
										{
											returningString += "\nInvalid Custom Item ID: " + ID;
										}
										if (j + z == 0)
										{
											returningString += "\nPlease, introduce valid items.";
										}
										else
										{
											spawnInfo.items = itemsToAdd.Take(j).ToArray();
											spawnInfo.CustomItems = customItemsToAdd.Take(z).ToArray();
											returningString += "\nModified to use items " + ItemsFileManager.ParseItems(spawnInfo.items, spawnInfo.CustomItems);
										}
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
										Vector3 vec = ParseRot(editArgs[i].Substring(9));
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
										Vector3 vec = ParseRot(editArgs[i].Substring(9));
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
								addList.Add(spawnInfo);
								addList = addList.OrderBy(x => x.line).ToList();
								return new string[] { Environment.NewLine + returningString };
							case "-":
							case "R":
							case "REM":
							case "REMOVE":
								if (addList.Count == 0)
								{
									return new string[] { "There are no items in the NEWLIST." };
								}
								if (args.Length < 3)
								{
									return new string[] { "Usage: ITEMSPAWNER " + args[0] + " " + args[1] + " <id>" };
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
							addListString += Environment.NewLine + i + ": - Roomtype:" + spawnInfo.RoomType.ToString()
								+ "\n - Items: " + ItemsFileManager.ParseItems(spawnInfo.items, spawnInfo.CustomItems)
								+ "\n - Probability: " + spawnInfo.probability.ToString()
								+ "\n - Position: " + spawnInfo.position.ToString()
								+ "\n - Rotation: " + spawnInfo.rotation.ToString();
						}
						return new string[] { addListString };
					}
					break; // Don't ask why I have to place this break here
				case "RL":
				case "ROOMS":
				case "ROOMLIST":
					string retValue = "List of ROOMTYPES:\n";
					foreach (RoomType room in Enum.GetValues(typeof(RoomType)))
					{
						retValue += room.ToString() + ", ";
					}
					return new string[] { retValue };
				case "SL":
				case "SPL":
				case "SPAWNS":
				case "SPAWNLIST":
					if (args.Length == 1)
					{
						// RoomType:ItemType, ItemType2...:Probability:Vector3:Rotation
						string spawnlistString = "List:\n";
						int i = 0;
						foreach (SpawnInfo spawnInfo in ItemsFileManager.spawnlist)
						{
							i++;
							spawnlistString += Environment.NewLine +  i + ":\n - Roomtype:" + spawnInfo.RoomType.ToString()
								+ "\n - Items: " + ItemsFileManager.ParseItems(spawnInfo.items, spawnInfo.CustomItems)
								+ "\n - Probability: " + spawnInfo.probability.ToString()
								+ "\n - Position: " + spawnInfo.position.ToString()
								+ "\n - Rotation: " + spawnInfo.rotation.ToString();
						}
						return new string[] { spawnlistString }; 
					}
					else switch(args[1].ToUpper())
						{
							case "E":
							case "ED":
							case "EDIT":
								if (args.Length < 3)
								{
									return new string[] { "Usage: ITEMSPAWNER " + args[0] + " " + args[1] + " <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]\nExample: 'ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5'. Items CAN'T be separated with spaces." };
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
								if (args.Length < 4)
								{
									return new string[] { "Please, introduce another argument." };
								}
								SpawnInfo spawnInfoRef = ItemsFileManager.spawnlist.ElementAt(id - 1);
								SpawnInfo spawnInfo = new SpawnInfo(spawnInfoRef.RoomType, spawnInfoRef.line, spawnInfoRef.items, spawnInfoRef.CustomItems, spawnInfoRef.probability, spawnInfoRef.position, spawnInfoRef.rotation);
								string returningString = "Item with ID " + args[2];
								string[] editArgs = args.Skip(3).ToArray();
								for (int i = 0; i < editArgs.Length; i++)
								{
									if (editArgs[i].ToUpperInvariant().StartsWith("ITEMS="))
									{
										string[] probablyItems = editArgs[i].Substring(6).Split(',');
										ItemType[] itemsToAdd = new ItemType[probablyItems.Length];
										int[] customItemsToAdd = new int[probablyItems.Length];
										List<int> invalidCustomIds = new List<int>();
										int j = 0, z = 0;
										foreach (string item in probablyItems)
										{
											string itemDataValue = item.Trim();
											#region ItemManager Region
											if (item.StartsWith("IM_"))
											{
												if (int.TryParse(itemDataValue.Substring(3), out int customItem))
												{
													if (ItemManager.Items.Handlers.ContainsKey(customItem))
													{
														customItemsToAdd[z] = (customItem);
														z++;
													}
													else
													{
														invalidCustomIds.Add(customItem);
													}
												}
											}
											else  
											#endregion
											if (Enum.TryParse(item, out ItemType itemType))
											{
												itemsToAdd[j] = itemType;
												j++;
											}
											else if (int.TryParse(item, out int idParsed))
											{
												if(idParsed >=-1 && idParsed <= 30)
												{
													itemsToAdd[j] = (ItemType)idParsed;
													j++;
												}
											}
										}
										if (j + z == 0)
										{
											returningString += "\nPlease, introduce valid items.";
										}
										foreach(int ID in invalidCustomIds)
										{
											returningString += "\nInvalid Custom Item ID: " + ID;
										}
										spawnInfo.items = itemsToAdd.Take(j).ToArray();
										spawnInfo.CustomItems = customItemsToAdd.Take(z).ToArray();
										returningString += "\nModified to use items " + ItemsFileManager.ParseItems(spawnInfo.items, spawnInfo.CustomItems);
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
										Vector3 vec = ParseRot(editArgs[i].Substring(9));
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
										Vector3 vec = ParseRot(editArgs[i].Substring(9));
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
							case "-":
							case "R":
							case "REM":
							case "REMOVE":
								if (ItemsFileManager.spawnlist.Count == 0)
								{
									return new string[] { "There are no items in the Spawnlist." };
								}
								if (args.Length < 2)
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
				Vector3 plyRot = scp049Component.plyCam.transform.forward;
				Physics.Raycast(scp049Component.plyCam.transform.position, plyRot, out RaycastHit where, 40f, scp106Component.teleportPlacementMask);
				if (where.point.Equals(Vector3.zero))
				{
					ev.ReturnMessage = "Failed to get the Vector3, and to spawn the coin. Try another place.";
				}
				else
				{
					Vector3 rotation = new Vector3(-plyRot.x, plyRot.y, -plyRot.z), position = Spawner.Vec3ToVector3(where.point) + (Vector3.Up * 0.1f);
					PluginManager.Manager.Server.Map.SpawnItem(ItemType.COIN, position, rotation);
					spawnedCoins.Add(new PosVector3Pair(position, rotation));
					Room room = ClosestRoom(where.point);
					ev.ReturnMessage = "Added " + where.point.ToString() + " to the list."
						+ "\nYou're probably (maybe not) looking for the RoomType: " + room.RoomType.ToString() + "\nIf that's not the room you're looking for, check ITS RL through the R.A. console";
				}
			}
		}
		public Room ClosestRoom(Vector3 yourpos)
		{
			float closestDist = 10000f;
			Room room = null;
			foreach (Room r in Spawner.rooms)
			{
				float curDist = Vector3.Distance(Spawner.Vec3ToVector3(yourpos), r.Position);
				if (curDist < closestDist)
				{
					closestDist = curDist;
					room = r;
				}
			}
			return room;
		}
		private Vector3 ParseRot(string Vector3Data)
		{
			string[] Vector3 = Vector3Data.Split(',');
			if (Vector3.Length != 3)
			{
				plugin.Info("Bad format for a Vector3 (" + Vector3Data + ')');
				return null;
			}
			if (!float.TryParse(Vector3[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
				|| !float.TryParse(Vector3[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
				|| !float.TryParse(Vector3[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
			{
				plugin.Info("Error parsing Vector3: (" + Vector3Data + ')');
				return null;
			}
			return new Vector3(x, y, z);
		}
	}
}
