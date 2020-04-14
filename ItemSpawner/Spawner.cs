using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EXILED;
using EXILED.ApiObjects;
using EXILED.Extensions;
using UnityEngine;

namespace ItemSpawner
{
	/// <summary>
	/// Defines the Spawner class, to provide an easy to use API to get positions based on the current room.
	/// </summary>
	public static class Spawner
	{

		public static List<Room> DistinctRooms = null;
		
		public static Vector3 GetRelativePosition(this Room room, Vector3 position)
		{
			return room.Transform.InverseTransformPoint(position);
		}
		
		public static Vector3 GetRelativeRotation(this Room room, Vector3 rotation)
		{
			return room.Transform.InverseTransformDirection(rotation);
		}

		public static void SpawnItem(this Room room, ItemType item, Vector3 position, Vector3 direction, int sight = 0, int barrel = 0, int other = 0)
		{
			if (room == null) throw new ArgumentNullException("position", "Tried to spawn an item with in a non-existing EXILED.ApiObject.Room");

			Transform transform = room.Transform;
			Vector3 relativeDirection = transform.TransformDirection(direction);
			Quaternion relativeRotation = Quaternion.Euler(relativeDirection);
			Vector3 relativePosition = transform.TransformPoint(position);

			Map.SpawnItem(item, float.PositiveInfinity, relativePosition, relativeRotation, sight, barrel, other);
			
			if (SpawnerConfig.Configs.Debug)
			{
				ServerConsole.AddLog($"[ItemSpawner DEBUG] Spawned {item.ToString() } in {room.Name}" +
										 $"{Environment.NewLine}\t- Position: [{relativePosition.x}, {relativePosition.y}, {relativePosition.z}] (world space: {transform.position.ToString()})" +
										 $"{Environment.NewLine}\t- Rotation: [{relativeRotation.x}, {relativeRotation.y}, {relativeRotation.z}]" +
										 $"{Environment.NewLine}\t- Attachments: sight: {sight}, barrel: {barrel}, other: {other}" +
										 $"{Environment.NewLine}\t- Spawned by:{Assembly.GetCallingAssembly().GetName().Name}");
			}
			else if (SpawnerConfig.Configs.Verbose) Log.Info("Spawned " + item.ToString() + " in: " + room.Name);
		}
		#region Old ItemSpawner stuff
		/* 
		 * To be implemented when custom items are a thing again
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
		#endregion
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
		internal static void OnWaitingForPlayers()
		{
			/**
			 * You have multicore/multithread support? Nice, the secondary GC thread will take
			 * care of this. You don't? Then LOL, because this is wasting precious CPU cycles.
			 * Say goodbye to your nanoseconds.
			 * (This just "cleans" the old list by making a new one. The other gets deleted.)
			 * */

			DistinctRooms = new List<Room>(Map.Rooms.Distinct(new DistinctRoomComparer()));
		}
	}
}