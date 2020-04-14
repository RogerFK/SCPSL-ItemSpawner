using System.Collections.Generic;
using EXILED;
using EXILED.ApiObjects;
using UnityEngine;

namespace ItemSpawner
{
	public class ItemSpawnerPlugin : Plugin
	{
		public static ItemSpawnerPlugin Instance { private set; get; }

		public override string getName => "ItemSpawner";

		public override void OnDisable()
		{
			Events.WaitingForPlayersEvent += Spawner.OnWaitingForPlayers;
		}

		public override void OnEnable()
		{
			Events.WaitingForPlayersEvent += Spawner.OnWaitingForPlayers;
		}

		public string[] allowedranks = new string[] { "owner", "admin" };

		public override void OnReload()
		{
			// this should be used by the plugin, btw, so it saves everything into a file *just in case*
		}
	}
	public struct SpawnInfo
	{
		public readonly Room RoomType;
		public readonly int line; // This saves the line to later modify it

		public ItemType[] items;
		public int[] CustomItems;
		public float probability;
		public Vector3 position;
		public Vector3 rotation;

		public SpawnInfo(Room roomType, int line, ItemType[] itemType, int[] CustomItems, float probability, Vector3 position, Vector3 rotation)
		{
			RoomType = roomType;
			items = itemType;
			this.CustomItems = CustomItems;
			this.probability = probability;
			this.line = line;
			this.position = position;
			this.rotation = rotation;
		}
	}
	public struct PosVector3Pair
	{
		public readonly Vector3 position;
		public readonly Vector3 rotation;
		public PosVector3Pair(Vector3 position, Vector3 rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}
	}
}
