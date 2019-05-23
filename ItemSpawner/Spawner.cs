using System;
using System.Collections.Generic;
using System.Linq;

using Smod2;
using Smod2.API;
using Smod2.Commands;
using Smod2.EventHandlers;
using Smod2.Events;
using UnityEngine;
using UnityEngine.Networking;

namespace RogerFKspawner
{
    internal class Spawner : IEventHandlerWaitingForPlayers, ICommandHandler, IEventHandlerCallCommand
    {
        private static Plugin ploogin;

        private List<GameObject> items = new List<GameObject>();

        private List<SpawnInfo> addList = new List<SpawnInfo>();

        public static void Init(Smod2.Plugin plugin, Priority priority = Priority.Highest)
        {
            ploogin = (Plugin)plugin;
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
        public static void AddItem(Room room, ItemType item, Vector vector, Vector rotation = null)
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
            PluginManager.Manager.Server.Map.SpawnItem(item, ZriToVector((room.GetGameObject() as GameObject).transform.TransformPoint(VectorTo3(vector))),
            ZriToVector((room.GetGameObject() as GameObject).transform.TransformDirection(VectorTo3(rotation))));
            ploogin.Info("Spawned " + item.ToString() + " in: " + room.RoomType.ToString());
        }

        public static void AddItem(RoomType room, ItemType item, Vector vector, Vector rotation = null)
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
                if (!ploogin.allowedranks.Contains(p.GetUserGroup().Name))
                {
                    return new string[] { "You're not allowed to run this command." };
                }
            }
            if (args.Length == 0)
            {
                return new string[] { "Please introduce a second argument", "<ADDCOINS/DELCOINS/ADDLIST/SPAWNLIST/ROOMLIST>" };
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
                            case "ADDLIST":
                                return new string[] { "ITEMSPAWNER ADDLIST - Displays the current list that will get added when you CONFIRM the addlist",
                                    "ITEMSPAWNER ADDLIST EDIT <position> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z]- Edits the element with it's position when those arguments are passed.\nExample: ITEMSPAWNER ADDLIST EDIT 2 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
                                    "ITEMSPAWNER ADDLIST REMOVE <position> - Removes the element at the given position",
                                    "ITEMSPAWNER ADDLIST CONFIRM - Saves the current list to items.txt" };
                            case "SPAWNLIST":
                                return new string[] { "ITEMSPAWNER SPAWNLIST - Displays the current spawnlist",
                                    "ITEMSPAWNER SPAWNLIST EDIT <position> [items=ITEM1,ITEM2/probability=XX.X/rotation=X,Y,Z]- Edits the element with it's position when those arguments are passed.\nExample: ITEMSPAWNER SPAWNLIST EDIT 4 items=COIN,MEDKIT rotation=1,0,0 probability=12.5",
                                    "ITEMSPAWNER SPAWNLIST REMOVE <position> - Removes the element at the given position" };
                            case "ROOMLIST":
                                return new string[] { "ITEMSPAWNER ROOMLIST - Displays every RoomType in the game. Non-unique rooms like hallways will probably not work, tho." };
                            default:
                                return new string[] { GetUsage() };
                        }
                    }
                    return new string[] { GetUsage() };
                #endregion
                case "ROOMLIST":
                    string retValue = "List:\n";
                    foreach (RoomType room in Enum.GetValues(typeof(RoomType)))
                    {
                        retValue += room.ToString() + ", ";
                    }
                    return new string[] { retValue };
                case "ADDLIST":
                    if(args.Count() > 1)
                    {

                    }
                    return new string[] { GetUsage() };
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
                    returnValueLocal += "Your pos to: " + r.RoomType.ToString() + aux3.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ", " + aux3.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ", " + aux3.z.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n";
                }
            }
            ploogin.Info(returnValueLocal);
            return new string[] { returnValueLocal };
        }

        public void OnCallCommand(PlayerCallCommandEvent ev)
        {
            if (ev.Command.StartsWith("newpos"))
            {
                if (!ploogin.allowedranks.Contains(ev.Player.GetUserGroup().Name))
                {
                    ev.ReturnMessage = "You can't use this command.";
                    return;
                }
                //ploogin.Info(ev.Player.GetRotation().ToString() + "<-- if this is 0,0,0 multiple times, Smod2 is trash");
                var aux = ((GameObject)ev.Player.GetGameObject()).GetComponent<Scp049PlayerScript>();
                ploogin.Info(aux.plyCam.transform.forward.ToString());
                Vector3 plyRot = aux.plyCam.transform.forward;
                UnityEngine.Physics.Raycast(aux.plyCam.transform.position, plyRot, out RaycastHit where, 40f);

                if (where.point.Equals(Vector3.zero))
                {
                    ev.ReturnMessage = "Failed to get the position. Try another place.";
                }
                else
                {
                    ploogin.Info(where.point + " vs  " + where.transform.position);

                    Inventory component = GameObject.Find("Host").GetComponent<Inventory>();
                    if (component != null)
                    {
                        GameObject auxItem = component.SetPickup((int)ItemType.COIN, -4.6566467E+11f, where.point, Quaternion.Euler(new Vector3(-plyRot.x, plyRot.y, -plyRot.z)), 0, 0, 0);
                        items.Add(auxItem);
                    }
                    Room room = null;
                    Vector3 closestVec = new Vector3(100, 40000, 100);
                    ClosestRoom(where.point, out room, ref closestVec);
                    ploogin.Info(room.RoomType.ToString() + " detected as the closest (" + where.point + ')');
                    try
                    {
                        ev.ReturnMessage = "Added " + where.point.ToString() + " to the list. Its pos-ID is " + (items.Count - 1)
                            + "\nTo check the vectors, type \"itemspawner list\" in the R.A. console.\nTo add them, type \"itemspawner add <pos-id> <RoomType> <your items>\""
                            + "\nYou're probably looking for the RoomType: " + room.RoomType.ToString();
                    }
                    catch
                    {
                        ev.ReturnMessage = "Unexpected error. Try running this command again."; // not really hehhehehe make this a meme
                    }
                }
            }
        }
        public void ClosestRoom(Vector3 yourpos, out Room room, ref Vector3 closest)
        {
            float closestDist = 10000f;
            room = null;
            foreach (Room r in rooms)
            {
                float curDist = Vector.Distance(ZriToVector(yourpos), r.Position);
                if (curDist < closestDist)
                {
                    closestDist = curDist;
                    closest = (r.GetGameObject() as GameObject).transform.position;
                    room = r;
                }
            }
        }
        public void DeleteItems()
        {
            foreach (var aux in items)
            {
                NetworkServer.Destroy(aux);
            }
            items.Clear();
        }

    }
}