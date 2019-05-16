using System;
using System.Collections.Generic;
using System.Linq;

using Smod2;
using Smod2.API;
using Smod2.Commands;
using Smod2.EventHandlers;
using Smod2.Events;
using UnityEngine;

namespace RogerFKspawner
{
	internal class Spawner : IEventHandlerWaitingForPlayers, ICommandHandler
	{
		private static Smod2.Plugin ploogin;

		public static void Init(Smod2.Plugin plugin, Priority priority = Priority.Highest)
		{
			ploogin = plugin;
			plugin.AddEventHandlers(new Spawner(), priority);
		}

		public static List<Room> rooms = null;

		public static Vector3 VectorTo3(Vector v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		public static Vector ZriToVector(Vector3 v)
		{
			return new Vector(v.x, v.y, v.z);
		}
		public static Vector LocalToGlobalPos(Room room, Vector position)
		{
			return ZriToVector((room.GetGameObject() as GameObject).transform.TransformPoint(VectorTo3(position)));
		}

		public static Vector LocalToGlobalRot(Room room, Vector rotation)
		{
			return ZriToVector((room.GetGameObject() as GameObject).transform.TransformDirection(VectorTo3(rotation)));
		}
		public static void AddItemToRoomPos(Room room, ItemType item, Vector vector, Vector rotation = null)
		{
			if (rotation == null)
			{
				rotation = Vector.Zero;
			}

			if (vector == null)
			{
				ploogin.Info("You gave one null vector, somewhere");
				return;
			}
			PluginManager.Manager.Server.Map.SpawnItem(item, ZriToVector((room.GetGameObject() as GameObject).transform.TransformPoint(VectorTo3(vector))),
			ZriToVector((room.GetGameObject() as GameObject).transform.TransformDirection(VectorTo3(rotation))));
			ploogin.Info("Spawneado " + item.ToString() + " en: " + room.RoomType.ToString());
		}

		public static void AñadeItemPosRT(RoomType room, ItemType item, Vector vector, Vector rotation = null)
		{
			if (rotation == null)
			{
				rotation = Vector.Zero;
			}

			foreach (Smod2.API.Room r in rooms)
			{
				if (r.RoomType == room)
				{
					PluginManager.Manager.Server.Map.SpawnItem(item, ZriToVector((r.GetGameObject() as GameObject).transform.TransformPoint(VectorTo3(vector))),
					ZriToVector((r.GetGameObject() as GameObject).transform.TransformDirection(VectorTo3(rotation))));
					break;
				}
			}
		}

		//copypasted from Stack Overflow
		private class DistinctRoomComparer : IEqualityComparer<Room>
		{
			public bool Equals(Room x, Room y)
			{
				return x.RoomType == y.RoomType;
			}
			public int GetHashCode(Room obj)
			{
				return obj.RoomType.GetHashCode();
			}
		}
		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			rooms = ev.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA).Distinct(new DistinctRoomComparer()).ToList();
		}

		public string GetCommandDescription()
		{
			return "pa sacar la posicion en base a esa sala lol";
		}

		public string GetUsage()
		{
			return "COINFECTHER <la sala>";
		}

		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (args.Length == 0)
			{
				string retValue = "List:\n";
				foreach (RoomType room in Enum.GetValues(typeof(RoomType)))
				{
					retValue += room.ToString() + "\n";
				}
				retValue += "Use: COINFECTHER <RoomType>";
				return new string[] { retValue };
			}
			if(sender is Player p)
			{
				if(p.GetUserGroup().Name != "owner" || p.GetUserGroup().Name != "admin")
				{
					return new string[] { "You don't have permissions to use this command. Download the plugin yourself and do it on your own machine."};
				}
			}
			string returnValueLocal = "Posiciones locales inversas:";
			returnValueLocal += "\n";
			foreach (Room r in rooms)
			{
				if (r.RoomType.ToString() == args[0])
				{
					foreach (Smod2.API.Item item in PluginManager.Manager.Server.Map.GetItems(ItemType.COIN, true))
					{
						Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(VectorTo3(item.GetPosition()));
						returnValueLocal += args[1] + ":COIN:";
						returnValueLocal += aux3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
						',' + aux3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
						',' + aux3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":0,0,0\n";
					}
					if (sender is Server)
					{
						foreach (Player rata in PluginManager.Manager.Server.GetPlayers())
						{
							Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(VectorTo3(rata.GetPosition()));
							returnValueLocal += rata.Name + "'s position: " + aux3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
							',' + aux3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
							',' + aux3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + '\n';
						}
					}
					else if (sender is Player puta)
					{
						Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(VectorTo3(puta.GetPosition()));
						returnValueLocal += "Your position: " + aux3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
						',' + aux3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
						',' + aux3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + '\n';
					}
					ploogin.Info(returnValueLocal);
					return new string[] { returnValueLocal };
				}
			}
			foreach (Room r in rooms)
			{
				if (sender is Server)
				{
					foreach (Player rata in PluginManager.Manager.Server.GetPlayers())
					{
						Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(VectorTo3(rata.GetPosition()));
						returnValueLocal += rata.Name + "'s pos to " + r.RoomType.ToString() + ": " + aux3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
						',' + aux3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
						',' + aux3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + '\n';
					}
				}
				if (sender is Player tomto)
				{
					Vector3 aux3 = (r.GetGameObject() as GameObject).transform.InverseTransformPoint(VectorTo3(tomto.GetPosition()));
					returnValueLocal += "Your pos to: "+ r.RoomType.ToString() + aux3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
					", " + aux3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
					", " + aux3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n";
				}
			}
			ploogin.Info(returnValueLocal);
			return new string[] { returnValueLocal };
		}
	}
}