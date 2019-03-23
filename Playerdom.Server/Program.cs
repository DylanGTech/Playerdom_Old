//using LiteNetLib;
//using LiteNetLib.Utils;
//using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using Playerdom.Shared;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Sockets;
using System.Reflection;
using Ceras;
using System.Text;
using Newtonsoft.Json;

namespace Playerdom.Server
{
    public class Program
    {

        public static List<ServerClient> leavingPlayers = new List<ServerClient>();


        static void AcceptClients(Map m, List<ServerClient> c)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 25565);
            listener.Start();

            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();

                ServerClient sc = new ServerClient(tcpClient, m);
                c.Add(sc);
            }

        }

        static void UpdateAll(Map level, List<ServerClient> clients)
        {
            while(true)
            {
                foreach(ServerClient sc in clients)
                {
                    if(!sc.IsInitialized)
                    {
                        sc.InitializePlayer(level);
                    }
                    if(sc.LastUpdate.AddSeconds(5) > DateTime.Now)
                    {
                        leavingPlayers.Add(sc);
                        sc.RemovePlayer(level);
                    }
                }

                foreach (ServerClient sc in leavingPlayers)
                {
                    clients.Remove(sc);
                }
                leavingPlayers.Clear();


                GameTime gameTime = new GameTime();
                foreach (KeyValuePair<Guid, Entity> ent in level.gameEntities)
                {
                    ent.Value.Update(gameTime, level);
                }

                List<Guid> playerGuids = new List<Guid>();
                foreach (ServerClient pc in clients)
                {
                    level.gameObjects[pc.FocusedObjectID].Update(gameTime, level, pc.InputState);
                    playerGuids.Add(pc.FocusedObjectID);
                }

                foreach (KeyValuePair<Guid, GameObject> g in level.gameObjects)
                {
                    if (!playerGuids.Contains(g.Key))
                        g.Value.Update(gameTime, level, new KeyboardState());
                }
                foreach (KeyValuePair<Guid, GameObject> g1 in level.gameObjects)
                {
                    foreach (KeyValuePair<Guid, GameObject> g2 in level.gameObjects)
                    {
                        if (g1.Value != g2.Value)
                        {
                            if (g1.Value.CheckCollision(g2.Value))
                            {
                                g1.Value.HandleCollision(g2.Value, level);
                                g2.Value.HandleCollision(g1.Value, level);

                            }
                        }
                    }
                    foreach (KeyValuePair<Guid, Entity> ent in level.gameEntities)
                    {
                        Vector2 depth = CollisionService.GetIntersectionDepth(g1.Value.BoundingBox, ent.Value.BoundingBox);

                        if (depth.X != 0 && depth.Y != 0)
                        {
                            g1.Value.HandleCollision(ent.Value, level);
                        }
                    }
                }
                foreach (KeyValuePair<Guid, GameObject> o in level.gameObjects)
                {
                    if (o.Value.MarkedForDeletion)
                    {
                        level.objectsMarkedForDeletion.Add(o.Key);
                    }
                }
                foreach (KeyValuePair<Guid, Entity> ent in level.gameEntities)
                {
                    if (ent.Value.MarkedForDeletion)
                    {
                        level.entitiesMarkedForDeletion.Add(ent.Key);
                    }
                }

                foreach (Guid g in level.objectsMarkedForDeletion)
                {
                    level.gameObjects.Remove(g);
                }
                level.objectsMarkedForDeletion.Clear();

                foreach (Guid g in level.entitiesMarkedForDeletion)
                {
                    level.gameEntities.Remove(g);
                }
                level.entitiesMarkedForDeletion.Clear();


                foreach (ServerClient pc in clients)
                {
                    SendUpdates(pc, level);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Playerdom Test Server");
            PlayerdomCerasSettings.Initialize();

            List<ServerClient> clients = new List<ServerClient>();
            Map level;


            level = MapService.CreateMap("World");

            TcpListener tcp = new TcpListener(IPAddress.Any, 25565);
            tcp.Start();

            new Thread(() => AcceptClients(level, clients)).Start();
            new Thread(() => UpdateAll(level, clients)).Start();



            while (!Console.KeyAvailable)
            {
            }
        }

        public static void SendUpdates(ServerClient player, Map level)
        {

            if (player.NeedsAllInfo)
            {
                player.NeedsAllInfo = false;
                //Tile Data
                MapColumn[] lc = new MapColumn[32];

                MapColumn col;

                for (int i = 0; i < Map.SIZE_X; i++)
                {
                    col = new MapColumn();

                    col.columnNumber = i;
                    for (int j = 0; j < Map.SIZE_Y; j++)
                    {
                        col.typesColumn[j] = level.tiles[i, j].typeID;
                        col.variantsColumn[j] = level.tiles[i, j].variantID;
                    }

                    lc[31 - (i % 32)] = col;

                    if (i % 32 == 31)
                    {
                        player.Send(lc);
                        
                        player.Log("Sent column packet");
                    }
                }


                //Focused Object
                player.Send(new KeyValuePair<Guid, GameObject>(player.FocusedObjectID, level.gameObjects.GetValueOrDefault(player.FocusedObjectID)));

                player.Log("Sent focused object");
            }


            if (player.HasMap)
            {
                //All Objects
                player.Send(level.gameObjects);
                player.Log("Sent game objects");


                //All Entities
                player.Send(level.gameEntities);

                player.Log("Sent game entities");
            }
        }
    }
}
