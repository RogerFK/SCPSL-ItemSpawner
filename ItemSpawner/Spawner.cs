using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ItemSpawner
{
	internal class Spawner
	{
		private static ItemSpawnerPlugin ploogin;
		public static void Init(ItemSpawnerPlugin plugin, Priority priority = Priority.Highest)
		{
			ploogin = plugin;
			plugin.AddEventHandlers(new Spawner(), priority);
		}

		public static List<Room> rooms = null;

		public static Vector3 Vector3To3(Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		public static Vector3 Vec3ToVector3(Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		[PipeMethod]
		public static Vector3 GetRelativePosition(Room room, Vector3 position)
		{
			return Vec3ToVector3((room.GetGameObject() as GameObject).transform.InverseTransformPoint(Vector3To3(position)));
		}
		[PipeMethod]
		public static Vector3 GetRelativeRotation(Room room, Vector3 rotation)
		{
			return Vec3ToVector3((room.GetGameObject() as GameObject).transform.InverseTransformDirection(Vector3To3(rotation)));
		}
		[PipeMethod]
		public static void SpawnItem(Room room, ItemType item, Vector3 Vector3, Vector3 rotation = null)
		{
			if (rotation == null)
			{
				rotation = Vector3.Zero;
			}

			if (Vector3 == null)
			{
				ploogin.Info("You gave one null Vector3, somewhere");
				return;
			}
			/* Thanks to Laserman for pointing out there's a TransformPoint inside Unity so I didn't have to use my slight knowledge in Vector3ial calculus */
			PluginManager.Manager.Server.Map.SpawnItem(item, Vec3ToVector3((room.GetGameObject() as GameObject).transform.TransformPoint(Vector3To3(Vector3))),
			Vec3ToVector3((room.GetGameObject() as GameObject).transform.TransformDirection(Vector3To3(rotation))));
			if (ploogin.verbose) ploogin.Info("Spawned " + item.ToString() + " in: " + room.RoomType.ToString());
		}
		[PipeMethod]
		public static void SpawnCustomItem(Room room, int id, Vector3 Vector3, Vector3 rotation = null)
		{
			if (rotation == null)
			{
				rotation = Vector3.Zero;
			}

			if (Vector3 == null)
			{
				ploogin.Info("You gave one null Vector3, somewhere");
				return;
			}
			/* Thanks to Laserman for pointing out there's a TransformPoint inside Unity so I didn't have to use my slight knowledge in Vector3ial calculus */
			var rotationConv = (room.GetGameObject() as GameObject).transform.TransformDirection(Vector3To3(rotation));

			try
			{
				ItemManager.Items.Handlers[id].Create((room.GetGameObject() as GameObject).transform.TransformPoint(Vector3To3(Vector3)), Quaternion.Euler(rotationConv.x, rotationConv.y, rotationConv.z));
				if (ploogin.verbose) ploogin.Info("Spawned IM_" + id.ToString() + " in: " + room.RoomType.ToString());
			}
			catch (Exception e)
			{
				ploogin.Info(e.ToString());
			}
		}
		[PipeMethod] // according to Androx, piped methods don't allow overloads
		public static void SpawnInRoomType(RoomType room, ItemType item, Vector3 Vector3, Vector3 rotation = null)
		{
			if (rotation == null)
			{
				rotation = Vector3.Zero;
			}

			if(Vector3 == null)
			{
				ploogin.Info("You gave one null Vector3 inside a SpawnInRoomType method, somewhere");
				return;
			}

			foreach (Room r in rooms)
			{
				if (r.RoomType == room)
				{
					PluginManager.Manager.Server.Map.SpawnItem(item, Vec3ToVector3((r.GetGameObject() as GameObject).transform.TransformPoint(Vector3To3(Vector3))),
					Vec3ToVector3((r.GetGameObject() as GameObject).transform.TransformDirection(Vector3To3(rotation))));
					if (ploogin.verbose) ploogin.Info("Spawned " + item.ToString() + " in: " + room.ToString());
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
		// This thing below fetches the rooms each different round
		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			rooms = ev.Server.
				Map
				.Get079InteractionRooms(Scp079InteractionType.SPEAKER) // So it uses the SPEAKER one first, as it appears it works better
				.Concat(PluginManager.Manager.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA)) // So the remaining ones get the CAMERA ones which appear to have no issue
				.Distinct(new DistinctRoomComparer()) // So you don't ever get two spawns in the same place
				.ToList(); // So you don't have to iterate over an IEnumerable over and over, which works way worse that if it wasn't a list
		}
	}
}