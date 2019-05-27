using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ceras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared;
using Playerdom.Shared.Entities;
using Playerdom.Shared.Models;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;

//Wrong namespace
namespace Playerdom.Server.Core
{
    public sealed class Program
    {
        public static readonly ConcurrentQueue<ChatMessage> ChatLog = new ConcurrentQueue<ChatMessage>();

        public static readonly ConcurrentQueue<string> LeavingPlayers = new ConcurrentQueue<string>();
        public static Map level = new Map();
        public static readonly ConcurrentDictionary<string, ServerClient> Clients = new ConcurrentDictionary<string, ServerClient>();

        private static void AcceptClients()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 25565);
            listener.Start();

            // While true? Use a cancellation token.
            while (true)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();

                ServerClient sc = new ServerClient(tcpClient);
                Clients.TryAdd(sc.EndPointString, sc);
                sc.Start();
            }
        }

        private static void UpdateAll()
        {
            // While true? Use a cancellation token.
            while (true)
            {
                foreach (var (_, value) in Clients)
                {
                    if (!value.IsLoggedIn && value.UserID != null)
                    {
                        value.IsLoggedIn = true;
                        value.InitializePlayer();
                    }

                    if (value.LastUpdate.AddSeconds(45) > DateTime.Now) continue;
                    if (!LeavingPlayers.Contains(value.EndPointString))
                        LeavingPlayers.Enqueue(value.EndPointString);
                }

                while (LeavingPlayers.TryDequeue(out string endpoint))
                {
                    Clients.TryRemove(endpoint, out ServerClient sc);

                    sc.SavePlayerStats();

                    if (sc == null) continue;
                    ServerClient.Log("Player left");
                    level.gameObjects.TryRemove(sc.FocusedObjectID, out GameObject player);
                    ChatLog.Enqueue(new ChatMessage() { senderID = 0, message = DateTime.Now.ToString("HH:mm") + " [SERVER]: Player Left ", textColor = Color.Red });
                    sc.Dispose();
                }

                GameTime gameTime = new GameTime();
                foreach (KeyValuePair<Guid, Entity> ent in level.gameEntities)
                {
                    ent.Value.Update(gameTime, level);
                }

                foreach (var (key, value) in level.gameObjects)
                {
                    value.Update(gameTime, level,
                        Clients.All(cl => cl.Value.FocusedObjectID != key)
                            ? new KeyboardState()
                            : Clients.First(cl => cl.Value.FocusedObjectID == key).Value.InputState, key);
                }

                foreach (var (_, gameObject) in level.gameObjects)
                {
                    foreach (var (_, value) in level.gameObjects)
                    {
                        if (gameObject == value) continue;
                        if (!gameObject.CheckCollision(value)) continue;
                        gameObject.HandleCollision(value, level);
                        value.HandleCollision(gameObject, level);
                    }
                    foreach (var (_, value) in level.gameEntities)
                    {
                        var (x, y) = gameObject.BoundingBox.GetIntersectionDepth(value.BoundingBox);

                        if (x != 0 && y != 0)
                        {
                            gameObject.HandleCollision(value, level);
                        }
                    }
                }
                foreach (var (key, value) in level.gameObjects)
                {
                    if (value.MarkedForDeletion)
                    {
                        level.objectsMarkedForDeletion.Add(key);
                    }
                }
                foreach (var (key, value) in level.gameEntities)
                {
                    if (value.MarkedForDeletion)
                    {
                        level.entitiesMarkedForDeletion.Add(key);
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

                while (ChatLog.Count > 12) //&& chatTimer == 6000)
                {
                    ChatLog.TryDequeue(out ChatMessage message);
                }
                // NEVER EVER EVER USE THREAD.SLEEP
                Thread.Sleep(10);
            }
        }

        private static void Main()
        {
            Console.WriteLine("Playerdom Test Server started at {0:HH:mm:ss}", DateTime.Now);
            PlayerdomCerasSettings.Initialize();

            try
            {
                level = LoadMap("World");
                Console.WriteLine("{0:HH:mm}", DateTime.Now.ToString("HH:mm") + " Loaded Map");
            }
            catch (Exception)
            {
                Console.WriteLine("{0:HH:mm}", DateTime.Now.ToString("HH:mm") + " [ERROR] Loading Map");
                level = MapService.CreateMap("World");
                Console.WriteLine("{0:HH:mm}", DateTime.Now.ToString("HH:mm") + " Creating Map");
            }

            new Thread(AcceptClients).Start();
            new Thread(UpdateAll).Start();

            System.Timers.Timer t = new System.Timers.Timer(60000); //Save every minute
            t.Elapsed += (sender, e) =>
            {
                try
                {
                    SaveMap(level.Clone(), Clients);
                }
                catch (Exception exception)
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

            CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.Config);

            if (!Directory.Exists(mapToSave.levelName))
            {
                Directory.CreateDirectory(mapToSave.levelName);
            }
            string worldPath = mapToSave.levelName + Path.DirectorySeparatorChar;

            using (FileStream stream = new FileStream(worldPath + "tiles.bin", FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    for (int y = 0; y < Map.SizeY; y++)
                    {
                        for (int x = 0; x < Map.SizeX; x++)
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
            CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.Config);
            Map m = new Map { levelName = name };

            if (!Directory.Exists(m.levelName))
            {
                Directory.CreateDirectory(m.levelName);
            }
            string worldPath = m.levelName + Path.DirectorySeparatorChar;

            using (FileStream stream = new FileStream(worldPath + "tiles.bin", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    for (int y = 0; y < Map.SizeY; y++)
                    {
                        for (int x = 0; x < Map.SizeX; x++)
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
