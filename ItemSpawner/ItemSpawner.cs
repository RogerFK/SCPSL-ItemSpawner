using System.Collections.Generic;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.Events;

namespace ItemSpawner
{
	[PluginDetails(
		author = "RogerFK",
		name = "Item Spawner",
		description = "A plugin that acts as an API too to spawn items and fetch custom positions easily",
		id = "rogerfk.spawner",
		version = "2.0",
		SmodMajor = 3,
		SmodMinor = 4,
		SmodRevision = 0,
		configPrefix = "is"
		)]
	public class ItemSpawner : Plugin
	{
		public override void OnDisable()
		{
			Info("Thank god you disabled me. Your CPU will surely thank you tbh");
		}

		public override void OnEnable()
		{
			Info("Stuff spawner enabled");
		}
		[ConfigOption]
		public string[] allowedranks = new string[] { "owner", "admin" };
		public override void Register()
		{
			AddEventHandlers(new ItemsFileManager(this), Priority.Low);
			AddEventHandlers(new ItemSpawnerCommand(this), Priority.Low);
			Spawner.Init(this);
			AddCommand("itemspawner", new ItemSpawnerCommand(this));
		}
	}
	public struct SpawnInfo
	{
		public readonly RoomType RoomType;
		public readonly int line; // This saves the line to later modify it

		public ItemType[] items;
		public float probability;
		public Vector position;
		public Vector rotation;

		public SpawnInfo(RoomType roomType, int line, ItemType[] itemType, float probability, Vector position, Vector rotation)
		{
			RoomType = roomType;
			items = itemType;
			this.probability = probability;
			this.line = line;
			this.position = position;
			this.rotation = rotation;
		}
	}
	public struct PosVectorPair
	{
		public readonly Vector position;
		public readonly Vector rotation;
		public PosVectorPair(Vector position, Vector rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}
	}
}
