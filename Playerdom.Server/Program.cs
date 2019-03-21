using LiteNetLib;
using LiteNetLib.Utils;
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
        static EventBasedNetListener listener;
        static NetManager server;
        static List<NetPeer> peers = new List<NetPeer>();
        static List<PlayerClient> clients = new List<PlayerClient>();

        static Assembly asm = Assembly.GetEntryAssembly();

        static void Main(string[] args)
        {
            Console.WriteLine("Playerdom Test Server");
            PlayerdomCerasSettings.Initialize();
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(25565);


            Map level;
            //try
            //{
            //    level = Task.Run(async () => await MapService.LoadMapAsync("World")).Result;
            //}
            //catch (Exception e)
            //{
                level = MapService.CreateMap("World");
            //}

            if (level == null)
                level = MapService.CreateMap("World");

            server.DisconnectTimeout = 5000;

            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
                Console.WriteLine(socketError.ToString());
            };

            listener.ConnectionRequestEvent += request =>
            {
                if (server.PeersCount < 16)
                    request.AcceptIfKey("Test");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("Player Joined: {0}", peer.EndPoint);

                Guid newGuid = Guid.NewGuid();

                level.gameObjects.Add(newGuid, new Player(new Point(0, 0), new Vector2(Tile.SIZE_X, Tile.SIZE_Y), displayName: "Player " + clients.Count.ToString()));

                clients.Add(new PlayerClient { PeerID = peer.Id, FocusedObjectID = newGuid, HasMap = false, InputState = new KeyboardState() });

                server.GetPeersNonAlloc(peers, ConnectionState.Incoming | ConnectionState.Connected);

                SendUpdates(clients.Last(), level, true);

            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("Player Left: {0} - Reason: {1}", peer.EndPoint, disconnectInfo.Reason);
                level.gameObjects[clients.First(x => x.PeerID == peer.Id).FocusedObjectID].MarkedForDeletion = true;

                clients.Remove(clients.First(x => x.PeerID == peer.Id));

                server.GetPeersNonAlloc(peers, ConnectionState.Connected);
            };

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                try
                {
                    CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.config);

                    string s = dataReader.GetString();

                    switch(s)
                    {
                        default:
                            throw new Exception("Not recognized operation");
                            break;
                        case "MapAffirmation":
                            clients.First(x => x.PeerID == fromPeer.Id).HasMap = true;
                            break;
                        case "Input":
                            //byte[] bytes = dataReader.GetBytesWithLength();
                            //Keys[] ks = serializer.Deserialize<Keys[]>(bytes);

                            Keys[] ks = JsonConvert.DeserializeObject<Keys[]>(dataReader.GetString(), PlayerdomJsonSettings.jsonSettings);

                            clients.First(x => x.PeerID == fromPeer.Id).InputState = new KeyboardState(ks);

                            break;
                    }
                }
                catch(Exception e)
                {
                    fromPeer.Disconnect();
                }
            };



            GameTime gameTime = new GameTime();

            System.Timers.Timer inputTimer = new System.Timers.Timer(60);
            System.Timers.Timer updateTimer = new System.Timers.Timer(40);

            inputTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                server.PollEvents();
            };

            updateTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                    foreach (KeyValuePair<Guid, Entity> ent in level.gameEntities)
                    {
                        ent.Value.Update(gameTime, level);
                    }

                    List<Guid> playerGuids = new List<Guid>();
                    foreach (PlayerClient pc in clients)
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




                    server.GetPeersNonAlloc(peers, ConnectionState.Connected);

                    foreach (PlayerClient pc in clients)
                    {
                        SendUpdates(pc, level);
                    }
            };

            updateTimer.Start();
            inputTimer.Start();

            while (!Console.KeyAvailable)
            {
            }

            inputTimer.Stop();
            updateTimer.Stop();

            server.Stop();
        }

        public static void SendUpdates(PlayerClient player, Map level, bool sendAllData = false)
        {

            NetDataWriter writer = new NetDataWriter();

            NetPeer peerToSend = peers.FirstOrDefault(x => x.Id == player.PeerID);
            if (peerToSend == null) return;

            CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.config);
            byte[] serializerBuffer = null;
            int bufferLength = 0;

            if (sendAllData)
            {

                ushort[] typesColumn = new ushort[Map.SIZE_X];
                byte[] variantsColumn = new byte[Map.SIZE_X];

                writer.Put("Tiles");
                writer.Put(0); //Column number
                for (int i = 0; i < Map.SIZE_X; i++)
                {
                    for (int j = 0; j < Map.SIZE_Y; j++)
                    {
                        typesColumn[j] = level.tiles[i, j].typeID;
                        variantsColumn[j] = level.tiles[i, j].variantID;
                    }

                    
                    //int typesLength = 0;
                    //int variantsLength = 0;

                    /*
                    typesLength = serializer.Serialize(typesColumn, ref compressedTypes);
                    variantsLength = serializer.Serialize(variantsColumn, ref compressedVariants);

                    byte[] subarray = new byte[typesLength];
                    Array.Copy(compressedTypes, subarray, typesLength);
                    writer.Put(typesLength);
                    writer.Put(subarray);

                    subarray = new byte[variantsLength];
                    Array.Copy(compressedVariants, subarray, variantsLength);
                    writer.Put(variantsLength);
                    writer.Put(subarray);
                    */

                    writer.PutArray(typesColumn);
                    writer.PutBytesWithLength(variantsColumn);

                    if (i % 32 == 31)
                    {

                        peerToSend.Send(writer, DeliveryMethod.ReliableOrdered);

                        Console.WriteLine("Sent {0} bytes to {1} with MTU at {2}", writer.Length, peerToSend.EndPoint, peerToSend.Mtu);
                        writer.Reset();

                        writer.Put("Tiles");
                        writer.Put(i + 1); //Column number
                    }
                }
                writer.Reset();

                writer.Put("FocusedObject");
                //writer.Put(JsonConvert.SerializeObject(pair.Value, PlayerdomJsonSettings.jsonSettings));
                //writer.Put(JsonConvert.SerializeObject(level.gameObjects.GetValueOrDefault(pair.Value), PlayerdomJsonSettings.jsonSettings));

                bufferLength = serializer.Serialize(player.FocusedObjectID, ref serializerBuffer);
                writer.PutBytesWithLength(serializerBuffer, 0, bufferLength);

                bufferLength = serializer.Serialize(level.gameObjects.GetValueOrDefault(player.FocusedObjectID), ref serializerBuffer);
                writer.PutBytesWithLength(serializerBuffer, 0, bufferLength);
                //writer.Put(LZ4MessagePackSerializer.Serialize(level.gameObjects.GetValueOrDefault(pair.Value), MessagePack.Resolvers.ContractlessStandardResolver.Instance));


                //serializer.Serialize(pair.Value, ref serializerBuffer);
                //writer.Put(serializerBuffer);
                //serializer.Serialize(level.gameEntities.GetValueOrDefault(pair.Value), ref serializerBuffer);
                //writer.Put(serializerBuffer);
                peerToSend.Send(writer, DeliveryMethod.ReliableOrdered);

                Console.WriteLine("Sent {0} bytes to {1} with MTU at {2}", writer.Length, peerToSend.EndPoint, peerToSend.Mtu);
                writer.Reset();
            }


            if (player.HasMap)
            {
                writer.Put("AllObjects");

                bufferLength = serializer.Serialize(level.gameObjects, ref serializerBuffer);

                writer.PutBytesWithLength(serializerBuffer, 0, bufferLength);
                peerToSend.Send(writer, DeliveryMethod.ReliableOrdered);

                Console.WriteLine("Sent {0} bytes to {1} with MTU at {2}", writer.Length, peerToSend.EndPoint, peerToSend.Mtu);
                writer.Reset();

                /*
                writer.Put("Objects");

                Dictionary<Guid, GameObject> newObjects = new Dictionary<Guid, GameObject>();
                Dictionary<Guid, Dictionary<string, byte[]>> objectDeltas = new Dictionary<Guid, Dictionary<string, byte[]>>();
                foreach (KeyValuePair<Guid, GameObject> g in level.gameObjects)
                {
                    if (g.Value.isNew) newObjects.Add(g.Key, g.Value);H

                    objectDeltas.Add(g.Key, g.Value.GetDelta());
                    g.Value.ResetDelta();
                }



                //bufferLength = serializer.Serialize(level.gameObjects, ref serializerBuffer);
                //writer.PutBytesWithLength(serializerBuffer);
                bufferLength = serializer.Serialize(newObjects, ref serializerBuffer);
                writer.PutBytesWithLength(serializerBuffer, 0, bufferLength);
                bufferLength = serializer.Serialize(objectDeltas, ref serializerBuffer);
                writer.PutBytesWithLength(serializerBuffer, 0, bufferLength);

                peerToSend.Send(writer, DeliveryMethod.ReliableOrdered);

                Console.WriteLine("Sent {0} new objects", newObjects.Count);
                Console.WriteLine("Sent {0} object changes", objectDeltas.Count);
                Console.WriteLine("Sent {0} bytes to {1} with MTU at {2}", writer.Length, peerToSend.EndPoint, peerToSend.Mtu);
                writer.Reset();
                */
                writer.Put("Entities");

                bufferLength = serializer.Serialize(level.gameEntities, ref serializerBuffer);
                writer.PutBytesWithLength(serializerBuffer, 0, bufferLength);
                peerToSend.Send(writer, DeliveryMethod.ReliableOrdered);

                Console.WriteLine("Sent {0} bytes to {1} with MTU at {2}", writer.Length, peerToSend.EndPoint, peerToSend.Mtu);
                writer.Reset();
            }
        }
    }
}
