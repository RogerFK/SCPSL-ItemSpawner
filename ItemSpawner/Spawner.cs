using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
		/// <summary>
		/// The list of Distinct Rooms that ItemSpawner will use
		/// </summary>
		public static List<Room> distinctRooms = null;

		/// <summary>
		/// The <see cref="Vector3"/> of the relative position (from the center of the room) to the <paramref name="room"/> based on the <paramref name="position"/>
		/// </summary>
		/// <param name="room">The <see cref="Room"/> you want to get the position from</param>
		/// <param name="position">The position you want to get the relative of</param>
		/// <returns>The <see cref="Vector3"/> from the center of the room</returns>
		public static Vector3 GetRelativePosition(this Room room, Vector3 position)
		{
			if (room == null) throw new ArgumentNullException("room", "Tried to get the position of a non-existing \"EXILED.ApiObject.Room\"");

			return room.Transform.InverseTransformPoint(position);
		}
		/// <summary>
		/// The <see cref="Vector3"/> of the relative rotation based on the the <paramref name="room"/>'s rotation, based on the <paramref name="position"/>
		/// </summary>
		/// <param name="room">The <see cref="Room"/> you want to get the rotation from</param>
		/// <param name="rotation">The rotation you want to get the relative of</param>
		/// <returns>The direction (euler angles), relative from the room's current rotation</returns>
		public static Vector3 GetRelativeRotation(this Room room, Vector3 rotation)
		{
			if (room == null) throw new ArgumentNullException("room", "Tried to get the rotation of a non-existing \"EXILED.ApiObject.Room\"");

			return room.Transform.InverseTransformDirection(rotation);
		}
		// Lol, no. I'm not documenting this method.
		/// <summary>
		/// Spawns an item based on the room's center, when used with <see cref="GetRelativePosition(Room, Vector3)"/> you'll be able to spawn items in positions consistently.
		/// </summary>
		/// <param name="item">The type of the item to be spawned</param>
		/// <param name="durability">The durability (or ammo, depends on the weapon) of the item</param>
		/// <param name="position">Where the item will be spawned</param>
		/// <param name="direction">The direction. We recommend you to use <see cref="Quaternion.Euler(float, float, float)"/></param>
		/// <param name="sight">The sight the weapon will have (0 is nothing, 1 is the first sight available in the weapon manager, and so on)</param>
		/// <param name="barrel">The barrel of the weapon (0 is no custom barrel, 1 is the first barrel available, and so on)</param>
		/// <param name="other">Other attachments like flashlight, laser or ammo counter</param>
		/// <returns>The <see cref="Pickup"/></returns>
		public static Pickup SpawnItem(this Room room, ItemType item, float durability, Vector3 position, Vector3 direction, int sight = 0, int barrel = 0, int other = 0)
		{
			if (room == null) throw new ArgumentNullException("room", "Tried to spawn an item with in a non-existing EXILED.ApiObject.Room");

			#region Geometry and position conversions
			Transform transform = room.Transform;
			Vector3 relativeDirection = transform.TransformDirection(direction);
			Quaternion relativeRotation = Quaternion.Euler(relativeDirection);
			Vector3 relativePosition = transform.TransformPoint(position);
			#endregion

			Pickup pickup = Map.SpawnItem(item, durability, relativePosition, relativeRotation, sight, barrel, other);

			#region Debug/Verbose logging

			if (SpawnerConfig.Configs.Debug)
			{
				ServerConsole.AddLog($"[ItemSpawner DEBUG] Spawned {item.ToString() } in {room.Name}" +
										 $"{Environment.NewLine}\t- Position: [{relativePosition.x}, {relativePosition.y}, {relativePosition.z}] (world space: {transform.position.ToString()})" +
										 $"{Environment.NewLine}\t- Rotation: [{relativeRotation.x}, {relativeRotation.y}, {relativeRotation.z}]" +
										 $"{Environment.NewLine}\t- Attachments: sight: {sight}, barrel: {barrel}, other: {other}" +
										 $"{Environment.NewLine}\t- Spawned by:{Assembly.GetCallingAssembly().GetName().Name}");
			}
			else if (SpawnerConfig.Configs.Verbose)
			{
				Log.Info("Spawned " + item.ToString() + " in: " + room.Name);
			}

			#endregion

			return pickup;
		}
		#region Old ItemManager stuff
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
			public bool Equals(Room x, Room y) => x.Name == y.Name;
			public int GetHashCode(Room obj) => obj.Name.GetHashCode();
		}
		/**
		 * You have multicore/multithread support? Nice, the secondary GC thread will take
		 * care of this. You don't? Then LOL, because this is wasting precious CPU cycles.
		 * Say goodbye to your nanoseconds.
		 * (This just "cleans" the old list by making a new one. The other gets deleted.)
		 * 
		 * Useful to not spawn stuff repeatedly.
		 */
		internal static void OnWaitingForPlayers() =>
			distinctRooms = new List<Room>(Map.Rooms.Distinct(new DistinctRoomComparer()));
	}
}