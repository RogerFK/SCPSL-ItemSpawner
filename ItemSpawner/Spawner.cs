using System;
using System.Collections.Generic;
using System.Linq;
using EXILED;
using EXILED.ApiObjects;
using EXILED.Extensions;
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

		public static List<Room> Rooms = null;

		public static Vector3 Vector3To3(Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		public static Vector3 Vec3ToVector3(Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
		
		public static Vector3 GetRelativePosition(Room room, Vector3 position)
		{
			return Vec3ToVector3(room.Transform.InverseTransformPoint(Vector3To3(position)));
		}
		
		public static Vector3 GetRelativeRotation(Room room, Vector3 rotation)
		{
			return Vec3ToVector3(room.Transform.InverseTransformDirection(Vector3To3(rotation)));
		}

		public static void SpawnItem(Room room, ItemType item, Vector3 position, Quaternion rotation = default, int sight = 0, int barrel = 0, int other = 0)
		{
			if (room == null) throw new ArgumentNullException("position", "Tried to spawn an item with a null EXILED.ApiObject.Room");
			if (position == null)
			{
				throw new ArgumentNullException("position", "Tried to spawn an item with a null UnityEngine.Vector3");
			}
			// i don't trust unity
			if (rotation == default)
			{
				rotation = new Quaternion(0f, 0f, 0f, 0f);
			}
			var transf = room.Transform;
			Quaternion epicDirection = new Quaternion(transf.rotation.x + rotation.x, transf.rotation.y + rotation.y, transf.rotation.z + rotation.z, transf.rotation.z + rotation.z);
			Map.SpawnItem(item, float.PositiveInfinity, transf.TransformPoint(position), epicDirection, sight, barrel, other);
			if (ploogin.verbose) Log.Info("Spawned " + item.ToString() + " in: " + room.Name);
		}

		/* To be implemented when custom items are a thing again
		public static void TrySpawnCustomItem(Room room, dynamic identifier, Vector3 position, Quaternion rotation = default)
		{
			try 
			{
				SpawnCustomItem(room, identifier, position, rotation);
			}
			catch (System.IO.FileNotFoundException ex) 
			{
				EXILED.Log.Error($"ItemManager not found! {ex.Message}");
			}
		}
		 
		public static void SpawnCustomItem(Room room, dynamic identifier, Vector3 position, Quaternion rotation = default);
		*/
		//copypasted from Stack Overflow
		private class DistinctRoomComparer : IEqualityComparer<Room>
		{
			public bool Equals(Room x, Room y)
			{
				return x.Name == y.Name;
			}
			public int GetHashCode(Room obj)
			{
				return obj.Name.GetHashCode();
			}
		}
		// This thing below fetches the rooms each different round
		public void OnWaitingForPlayers()
		{
			Rooms = new List<Room>(Map.GetRooms().Distinct(new DistinctRoomComparer()));
		}
	}
}