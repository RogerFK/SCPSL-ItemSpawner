using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		private List<GameObject> spawnedCoins = new List<GameObject>();

		private List<SpawnInfo> addList = new List<SpawnInfo>();
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
			return "You can type ITEMSPAWNER HELP [COMMAND] for more info about one specific command. <> means it's mandatory, [] is optional\n" +
				   "ITEMSPAWNER ADDCOINS <RoomType> - Adds the coin spawned through the newpos command to a list you can later modify, then removes them from the map\n" +
				   "ITEMSPAWNER DELCOINS - Deletes all coins that have been spawned through the newpos command\n" +
				   "ITEMSPAWNER ADDLIST [EDIT/REMOVE/CONFIRM] - Displays the current list that will be added to the items.txt file\n" +
				   "ITEMSPAWNER SPAWNLIST [EDIT/REMOVE] - Displays or modifies the current spawnlist, so you can modify it\n" +
				   "ITEMSPAWNER ROOMLIST - Displays every RoomType in the game. Non-unique rooms like hallways will probably not work, tho.";
		}
		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (sender is Player p)
			{
				if (!plugin.allowedranks.Contains(p.GetUserGroup().Name))
				{
					return new string[] { "You're not allowed to run this command." };
				}
			}
			if (args.Length == 0)
			{
				return new string[] { "Please, introduce a second argument", "<ADDCOINS/DELCOINS/ADDLIST/SPAWNLIST/ROOMLIST>" };
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
								return new string[] { "ITEMSPAWNER ADDCOINS <RoomType> - Adds the coins to spawn relatively to a roomtype from the list in ITEMSPAWNER ROOMLIST to a new list in ITEMSPAWNER ADDLIST so you can modify them one by one," +
									" then removes them from the map, and then you can use ITEMSPAWNER ADDLIST to modify their parameters (such as it's probability, etc.)." };
							case "DELCOINS":
								return new string[] { "ITEMSPAWNER DELCOINS - Deletes all coins that have been spawned through the newpos command" };
							case "ADDLIST": // currently being developed
								return new string[] { "ITEMSPAWNER ADDLIST - Displays the current list that will get added when you do ITEMSPAWNER ADDLIST CONFIRM",
									"ITEMSPAWNER ADDLIST EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]- Edits the element with it's id when those arguments are passed.\nExample: ITEMSPAWNER ADDLIST EDIT 2 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
									"ITEMSPAWNER ADDLIST REMOVE <id> - Removes the element at the given id",
									"ITEMSPAWNER ADDLIST CONFIRM - Saves the current list to items.txt" };
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
				case "ADDLIST":
					if (args.Count() > 1)
					{
						switch (args[1].ToUpper())
						{
							case "CONFIRM":
								foreach(SpawnInfo finalSpawnInfo in addList)
								{
									FileManager.AppendFile(ItemsFileManager.SpawnInfoToStr(finalSpawnInfo), "./items.txt", true);
								}
								addList.Clear();
								return new string[] { "New spawns succesfully written to the file items.txt" };
							case "EDIT":
								if(args.Count() < 3)
								{
									return new string[] { "Usage: ITEMSPAWNER EDIT <id> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z/position=X,Y,Z]\nExample: 'ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5'. Items CAN'T be separated with spaces." };
								}
								// Here comes the fun part.
								if(addList.Count == 0)
								{
									return new string[] { "There are no items in the ADDLIST." };
								}
								if(!int.TryParse(args[2], out int id))
								{
									return new string[] { "Please, enter a numerical ID." };
								}
								if(addList.Count < id)
								{
									return new string[] { "Please, enter a valid ID." };
								}
								SpawnInfo spawnInfo = addList.ElementAt(id-1);
								string returningString = "Item with ID " + args[2];
								string[] editArgs = args.Skip(3).ToArray();
								for(int i = 0; i < editArgs.Count(); i++)
								{
									if (editArgs[i].ToUpper().StartsWith("ITEMS="))
									{
										string[] probablyItems = editArgs[i].Substring(6).Split(',');
										ItemType[] itemsToAdd = new ItemType[probablyItems.Count()];
										foreach(string item in probablyItems)
										{
											if(Enum.TryParse(item, out ItemType itemType))
											{
												itemsToAdd.Append(itemType);
											}
										}
										if(itemsToAdd.Count() == 0)
										{
											returningString += "\nPlease, introduce valid items.";
										}
										spawnInfo.items = itemsToAdd;
										returningString += "\nModified to use items " + itemsToAdd;
									}
									else if (editArgs[i].ToUpper().StartsWith("PROBABILITY="))
									{
										string prob = editArgs[i].Substring(12);
										if(float.TryParse(prob, out float probParsed))
										{
											spawnInfo.probability = probParsed;
											returningString += "\nModified to use probability " + prob;
										}
										else
										{
											returningString += "\nPlease, introduce a valid probability." ;
										}
									}
									else if (editArgs[i].ToUpper().StartsWith("ROTATION="))
									{
										Vector vec = ParseRot(editArgs[i].Substring(9));
										if(vec != null)
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
								return new string[] { returningString };
							case "REMOVE":
								if (addList.Count == 0)
								{
									return new string[] { "There are no items in the ADDLIST." };
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
					}
					else
					{
						// RoomType:ItemType, ItemType2...:Probability:Vector:Rotation
						string addListString = "List:\n";
						int i = 0;
						foreach (SpawnInfo spawnInfo in addList)
						{
							i++;
							addListString += i + ". - " + spawnInfo.RoomType.ToString()
								+ " - " + spawnInfo.items.ToString()
								+ " - " + spawnInfo.probability.ToString()
								+ " - " + spawnInfo.position.ToString()
								+ " - " + spawnInfo.rotation.ToString();
						}
						return new string[] { addListString };
					}
					return new string[] { GetUsage() };
				case "ROOMLIST":
					string retValue = "List of ROOMTYPES:\n";
					foreach (RoomType room in Enum.GetValues(typeof(RoomType)))
					{
						retValue += room.ToString() + ", ";
					}
					return new string[] { retValue };
			}
			string returnValueLocal = "Posiciones locales inversas:";
			returnValueLocal += "\n";
			foreach (Room r in Spawner.rooms)
			{
				if (r.RoomType.ToString() == args[0])
				{
					foreach (Smod2.API.Item item in PluginManager.Manager.Server.Map.GetItems(ItemType.COIN, true))
					{
						Vector aux3 = Spawner.GetRelativePosition(r, item.GetPosition());
						returnValueLocal += args[1] + ":COIN:";
						returnValueLocal += aux3.x.ToString(CultureInfo.InvariantCulture) +
						',' + aux3.y.ToString(CultureInfo.InvariantCulture) +
						',' + aux3.z.ToString(CultureInfo.InvariantCulture) + ":0,0,0\n";
					}
					if (sender is Server)
					{
						foreach (Player player in PluginManager.Manager.Server.GetPlayers())
						{
							Vector aux3 = Spawner.GetRelativePosition(r, player.GetPosition());
							returnValueLocal += player.Name + "'s position: " + aux3.x.ToString(CultureInfo.InvariantCulture) +
							',' + aux3.y.ToString(CultureInfo.InvariantCulture) +
							',' + aux3.z.ToString(CultureInfo.InvariantCulture) + '\n';
						}
					}
					else if (sender is Player player)
					{
						Vector aux3 = Spawner.GetRelativePosition(r, player.GetPosition());
						returnValueLocal += "Your position: " + aux3.x.ToString(CultureInfo.InvariantCulture) +
						',' + aux3.y.ToString(CultureInfo.InvariantCulture) +
						',' + aux3.z.ToString(CultureInfo.InvariantCulture) + '\n';
					}
					plugin.Info(returnValueLocal);
					return new string[] { returnValueLocal };
				}
			}
			foreach (Room r in Spawner.rooms)
			{
				if (sender is Server)
				{
					foreach (Player rata in PluginManager.Manager.Server.GetPlayers())
					{
						Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(Spawner.VectorTo3(rata.GetPosition()));
						returnValueLocal += rata.Name + "'s pos to " + r.RoomType.ToString() + ": " + aux3.x.ToString(CultureInfo.InvariantCulture) +
						',' + aux3.y.ToString(CultureInfo.InvariantCulture) +
						',' + aux3.z.ToString(CultureInfo.InvariantCulture) + '\n';
					}
				}
				if (sender is Player tomto)
				{
					Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(Spawner.VectorTo3(tomto.GetPosition()));
					returnValueLocal += "Your pos to: " + r.RoomType.ToString() + aux3.x.ToString(CultureInfo.InvariantCulture) +
					", " + aux3.y.ToString(CultureInfo.InvariantCulture) +
					", " + aux3.z.ToString(CultureInfo.InvariantCulture) + "\n";
				}
			}
			plugin.Info(returnValueLocal);
			return new string[] { returnValueLocal };
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
					Inventory component = GameObject.Find("Host").GetComponent<Inventory>();
					if (component != null)
					{
						GameObject auxItem = component.SetPickup((int)ItemType.COIN, -4.6566467E+11f, where.point, Quaternion.Euler(new Vector3(-plyRot.x, plyRot.y, -plyRot.z)), 0, 0, 0);
						spawnedCoins.Add(auxItem);
					}
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
		public void DeleteCoins()
		{
			foreach (GameObject aux in spawnedCoins)
			{
				NetworkServer.Destroy(aux);
			}
			spawnedCoins.Clear();
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
