/* Find an updated version of this class inside: https://github.com/RogerFK/SMOD2-ItemSpawner */
// Inspired by old Androx's Timing.cs class

using System.Collections.Generic;
using System.Linq;

using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Piping;
using UnityEngine;

namespace ItemSpawner
{
	internal class Spawner : IEventHandlerWaitingForPlayers
	{
		private static ItemSpawner ploogin;

		public static void Init(ItemSpawner plugin, Priority priority = Priority.Highest)
		{
			ploogin = plugin;
			plugin.AddEventHandlers(new Spawner(), priority);
		}

		public static List<Room> rooms = null;

		public static Vector3 VectorTo3(Vector v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		public static Vector Vec3ToVector(Vector3 v)
		{
			return new Vector(v.x, v.y, v.z);
		}
		[PipeMethod]
		public static Vector GetRelativePosition(Room room, Vector position)
		{
			return Vec3ToVector((room.GetGameObject() as GameObject).transform.InverseTransformPoint(VectorTo3(position)));
		}
		[PipeMethod]
		public static Vector GetRelativeRotation(Room room, Vector rotation)
		{
			return Vec3ToVector((room.GetGameObject() as GameObject).transform.InverseTransformDirection(VectorTo3(rotation)));
		}
		[PipeMethod]
		public static void SpawnItem(Room room, ItemType item, Vector vector, Vector rotation = null)
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
			/* Thanks to Laserman for pointing out there's a TransformPoint inside Unity so I didn't have to use my slight knowledge in vectorial calculus */
			PluginManager.Manager.Server.Map.SpawnItem(item, Vec3ToVector((room.GetGameObject() as GameObject).transform.TransformPoint(VectorTo3(vector))),
			Vec3ToVector((room.GetGameObject() as GameObject).transform.TransformDirection(VectorTo3(rotation))));
			if (ploogin.verbose) ploogin.Info("Spawned " + item.ToString() + " in: " + room.RoomType.ToString());
		}
		[PipeMethod] // according to Androx, piped methods don't allow overloads
		public static void SpawnInRoomType(RoomType room, ItemType item, Vector vector, Vector rotation = null)
		{
			if (rotation == null)
			{
				rotation = Vector.Zero;
			}

			foreach (Room r in rooms)
			{
				if (r.RoomType == room)
				{
					PluginManager.Manager.Server.Map.SpawnItem(item, Vec3ToVector((r.GetGameObject() as GameObject).transform.TransformPoint(VectorTo3(vector))),
					Vec3ToVector((r.GetGameObject() as GameObject).transform.TransformDirection(VectorTo3(rotation))));
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