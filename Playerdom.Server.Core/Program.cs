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
using System.Collections.Concurrent;
using System.IO;
using Playerdom.Shared.Models;

namespace Playerdom.Server
{
    public class Program
    {

        public static ConcurrentQueue<ChatMessage> chatLog = new ConcurrentQueue<ChatMessage>();

        public static ConcurrentQueue<string> leavingPlayers = new ConcurrentQueue<string>();
        public static Map level = new Map();
        public static ConcurrentDictionary<string, ServerClient> clients = new ConcurrentDictionary<string, ServerClient>();

        private static System.Timers.Timer aTimer;
        /*private static ElapsedEventHandler OnTimedEvent;

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
           aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }*/

        static void AcceptClients()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 25565);
            listener.Start();

            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();

                ServerClient sc = new ServerClient(tcpClient);
                clients.TryAdd(sc.EndPointString, sc);
                sc.Start();
            }

        }

        static void UpdateAll()
        {
            while (true)
            {
                foreach (KeyValuePair<string, ServerClient> sc in clients)
                {
                    if (!sc.Value.IsInitialized)
                    {
                        sc.Value.InitializePlayer();
                    }

                    if (sc.Value.LastUpdate.AddSeconds(45) <= DateTime.Now)
                    {
                        if (!leavingPlayers.Contains(sc.Value.EndPointString))
                            leavingPlayers.Enqueue(sc.Value.EndPointString);
                    }

                }

                while (leavingPlayers.TryDequeue(out string endpoint))
                {
                    clients.TryRemove(endpoint, out ServerClient sc);

                    if (sc != null)
                    {
                        sc.Log("Player left");
                        level.gameObjects.TryRemove(sc.FocusedObjectID, out GameObject player);
                        Program.chatLog.Enqueue(new ChatMessage() { senderID = 0, message = "[SERVER]: Player Left", timeSent = DateTime.Now, textColor = Color.Orange });
                        sc.Dispose();
                    }
                }


                GameTime gameTime = new GameTime();
                foreach (KeyValuePair<Guid, Entity> ent in level.gameEntities)
                {
                    ent.Value.Update(gameTime, level);
                }

                foreach (KeyValuePair<Guid, GameObject> g in level.gameObjects)
                {
                    if (!clients.Any(cl => cl.Value.FocusedObjectID == g.Key))
                        g.Value.Update(gameTime, level, new KeyboardState(), g.Key);
                    else g.Value.Update(gameTime, level, clients.First(cl => cl.Value.FocusedObjectID == g.Key).Value.InputState, g.Key);
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
                    level.gameObjects.TryRemove(g, out GameObject o);
                }
                level.objectsMarkedForDeletion.Clear();

                foreach (Guid g in level.entitiesMarkedForDeletion)
                {
                    level.gameEntities.TryRemove(g, out Entity o);
                }
                level.entitiesMarkedForDeletion.Clear();

                //TODO: Auto clear text based on amount of messages and time
                //System.Timers.Timer chatTimer = new System.Timers.Timer(6000);


                while (chatLog.Count > 12) //&& chatTimer == 6000)
                {
                    chatLog.TryDequeue(out ChatMessage message);
                }

                Thread.Sleep(10);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Playerdom Test Server started at {0:HH:mm}", DateTime.Now);
            PlayerdomCerasSettings.Initialize();


            try
            {
                level = LoadMap("World");
                Console.WriteLine("{0:HH:mm}", DateTime.Now + " Loaded Map");
            }
            catch(Exception e)
            {
                Console.WriteLine("{0:HH:mm}", DateTime.Now + " [ERROR] Loading Map");
                level = MapService.CreateMap("World");
                Console.WriteLine("{0:HH:mm}", DateTime.Now + " Creating Map");
            }


            new Thread(() => AcceptClients()).Start();
            new Thread(() => UpdateAll()).Start();

            System.Timers.Timer t = new System.Timers.Timer(60000); //Save every minute
            t.Elapsed += (sender, e) =>
            {
                try
                {
                    SaveMap(level.Clone(), clients);
                }
                catch(Exception exception)
                {
                    Console.WriteLine(exception.GetType().ToString());
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);
                }
            };

            t.Start();

            while (!Console.KeyAvailable)
            {
            }
        }
        public static void SaveMap(Map mapToSave, ConcurrentDictionary<string, ServerClient> clients)
        {


            //Clear player objects and data from world
            foreach (KeyValuePair<string, ServerClient> client in clients)
            {
                mapToSave.objectsMarkedForDeletion.Add(client.Value.FocusedObjectID);
            }

            foreach (Guid g in mapToSave.objectsMarkedForDeletion)
            {
                mapToSave.gameObjects.TryRemove(g, out GameObject o);
            }
            mapToSave.objectsMarkedForDeletion.Clear();

            foreach (Guid g in mapToSave.entitiesMarkedForDeletion)
            {
                mapToSave.gameEntities.TryRemove(g, out Entity o);
            }
            mapToSave.entitiesMarkedForDeletion.Clear();


            CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.config);

            if (!Directory.Exists(mapToSave.levelName))
            {
                Directory.CreateDirectory(mapToSave.levelName);
            }
            string worldPath = mapToSave.levelName + Path.DirectorySeparatorChar;



            using (FileStream stream = new FileStream(worldPath + "tiles.bin", FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    for (int y = 0; y < Map.SIZE_Y; y++)
                    {
                        for (int x = 0; x < Map.SIZE_X; x++)
                        {
                            bw.Write(mapToSave.tiles[x, y].typeID);
                            bw.Write(mapToSave.tiles[x, y].variantID);
                        }
                    }
                }
            }

            File.WriteAllBytes(worldPath + "objects.bin", serializer.Serialize(mapToSave.gameObjects));
            File.WriteAllBytes(worldPath + "entities.bin", serializer.Serialize(mapToSave.gameEntities));
        }

        public static Map LoadMap(string name)
        {
            CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.config);
            Map m = new Map();
            m.levelName = name;


            if (!Directory.Exists(m.levelName))
            {
                Directory.CreateDirectory(m.levelName);
            }
            string worldPath = m.levelName + Path.DirectorySeparatorChar;


            using (FileStream stream = new FileStream(worldPath + "tiles.bin", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    for (int y = 0; y < Map.SIZE_Y; y++)
                    {
                        for (int x = 0; x < Map.SIZE_X; x++)
                        {
                            m.tiles[x, y].typeID = br.ReadUInt16();
                            m.tiles[x, y].variantID = br.ReadByte();
                        }
                    }
                }
            }

            m.gameObjects = serializer.Deserialize<ConcurrentDictionary<Guid, GameObject>>(File.ReadAllBytes(worldPath + "objects.bin"));
            m.gameEntities = serializer.Deserialize<ConcurrentDictionary<Guid, Entity>>(File.ReadAllBytes(worldPath + "entities.bin"));

            return m;
        }
    }
}
