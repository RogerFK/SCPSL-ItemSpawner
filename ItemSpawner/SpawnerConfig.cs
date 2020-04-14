using System;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using EXILED;

namespace ItemSpawner
{
	public class SpawnerConfig
	{
		[ThreadStatic]
		private static SpawnerConfig _instance;
		private static readonly object _lock = new object();

		/// <summary>
		/// Singleton instance of ItemSpawner configs (Thread-Safe).
		/// </summary>
		public static SpawnerConfig Configs
		{
			// Based on the Singleton pattern. Based on the Thread-Safe version.
			// https://refactoring.guru/design-patterns/singleton/csharp/example
			// The lock
			get
			{
				if (_instance == null)
				{
					lock (_lock)
					{
						if (_instance == null)
						{
							_instance = new SpawnerConfig();
						}
					}
				}
				return _instance;
			}
		}
		private bool _debug, _verbose, _fromFeet;

		/// <summary>
		/// Defines if ItemSpawner should print basic stuff to the console.
		/// </summary>
		public bool Verbose
		{
			get => _verbose;
			set
			{
				var callingAssembly = Assembly.GetCallingAssembly();
				if (callingAssembly == Assembly.GetExecutingAssembly())
				{
					if (value) Log.Info($"{callingAssembly.GetName().Name} enabled the Verbose variable for ItemSpawner. Hello!");
					else Log.Info($"{callingAssembly.GetName().Name} disabled the Verbose variable for ItemSpawner. Bye bye!");
				}
				_verbose = value;
			}
		}

		/// <summary>
		/// Defines if ItemSpawner should print debug lines. Useful to debug other plugins.
		/// </summary>
		public bool Debug
		{
			get => _debug;
			set
			{
				if (value) Log.Info($"{Assembly.GetCallingAssembly().GetName().Name} enabled the Debug variable for ItemSpawner. Hello! Prepare to get spammed!");
				else Log.Info($"{Assembly.GetCallingAssembly().GetName().Name} disabled the Debug variable for ItemSpawner. Bye bye! No more spam!");
				_debug = value;
			}
		}

		public bool FromFeet
		{
			get => _fromFeet;
			set
			{
				var callingAssembly = Assembly.GetCallingAssembly();
				if (callingAssembly == Assembly.GetExecutingAssembly())
				{
					if (value) Log.Info($"{Assembly.GetCallingAssembly().GetName().Name} enabled the FromFeet variable for ItemSpawner. Hello!");
					else Log.Info($"{Assembly.GetCallingAssembly().GetName().Name} disabled the FromFeet variable for ItemSpawner. Bye bye!");
				}
				_fromFeet = value;
			}
		}

		private SpawnerConfig()
		{
			UpdateConfigs();
		}
		/// <summary>
		/// Update the configs on the <see cref="SpawnerConfig"/> singleton
		/// </summary>
		public static void RefreshConfigs()
		{
			Configs.UpdateConfigs();
		}
		/// <summary>
		/// Update the configs on the current <see cref="SpawnerConfig"/> instance
		/// </summary>
		public void UpdateConfigs()
		{
			_verbose = Plugin.Config.GetBool("its_verbose", false);
			_debug = Plugin.Config.GetBool("its_debug", false);
			StringBuilder sb = new StringBuilder("ItemSpawner configs reloaded.");
			if (_debug)
			{
				sb.Append(" Loaded with the debug variable set to true.\n [ITEMSPAWNER WARNING] If you experience massive lag while executing ItemSpawner commands, please type: ITS DEBUG FALSE.");
			}
			else if (_verbose)
			{
				sb.Append(" Loaded as verbose: you'll get basic info on your console.");
			}
			else
			{
				sb.Append(" Loaded ItemSpawner silently.");
			}
			_fromFeet = Plugin.Config.GetBool("its_fromfeet", false);
			if (_debug) sb.AppendFormat(" Config of \"its_fromfeet\" set to {0}", _fromFeet);

			Log.Debug($"ItemSpawner loaded with the following configs:" +
					  $"{Environment.NewLine}\t\t- its_verbose: { _verbose }" +
					  $"{Environment.NewLine}\t\t- its_debug: { _debug }" +
					  $"{Environment.NewLine}\t\t- its_fromfeet: { _fromFeet }");
		}
	}
}