using Ceras;
using Ceras.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Playerdom.Server
{
    public class ServerClient
    {
        readonly TcpClient _tcpClient;
        readonly NetworkStream _netStream;
        readonly CerasSerializer _sendCeras;
        readonly CerasSerializer _receiveCeras;


        public DateTime LastUpdate { get; set; }

        public Guid FocusedObjectID { get; set; }
        public bool HasMap { get; set; } = false;
        public bool NeedsAllInfo { get; set; } = true;

        public bool IsInitialized { get; set; } = false;

        public KeyboardState InputState { get; set; }

        public ServerClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _netStream = tcpClient.GetStream();


            _sendCeras = new CerasSerializer(PlayerdomCerasSettings.config);
            _receiveCeras = new CerasSerializer(PlayerdomCerasSettings.config);

            Log("Player Joined");

            Guid newGuid = Guid.NewGuid();
            FocusedObjectID = newGuid;
            InputState = new KeyboardState();
            LastUpdate = DateTime.Now;


            StartReceivingMessages();
            StartSendingMessages();
        }

        public void InitializePlayer()
        {
            Program.level.gameObjects.Add(FocusedObjectID, new Player(new Point(0, 0), new Vector2(Tile.SIZE_X, Tile.SIZE_Y), displayName: "Player"));
        }

        public void RemovePlayer()
        {

        }

        void StartReceivingMessages()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Keep receiving packets from the client and respond to them
                    // Eventually when the client disconnects we'll just get an exception and end the thread...
                    while (true)
                    {
                        var obj = await _receiveCeras.ReadFromStream(_netStream);
                        Log("Recieved client input");
                        HandleMessage(obj);
                    }
                }
                catch (Exception e)
                {
                    Log($"Error while handling client '{_tcpClient.Client.RemoteEndPoint}': {e}");
                    RemovePlayer();
                    Program.leavingPlayers.Add(this);
                }
            });
        }


        void StartSendingMessages()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        lock (_sendCeras)
                        {
                            if (NeedsAllInfo)
                            {
                                NeedsAllInfo = false;
                                //Tile Data
                                MapColumn[] lc = new MapColumn[64];

                                for(int i = 0; i < 16; i++)
                                {
                                    lc[i] = new MapColumn(0, new ushort[Map.SIZE_X], new byte[Map.SIZE_Y]);
                                }



                                for (int i = 0; i < Map.SIZE_X; i++)
                                {

                                    lc[15 - (i % 16)].ColumnNumber = i;
                                    for (int j = 0; j < Map.SIZE_Y; j++)
                                    {
                                        lc[15 - (i % 16)].TypesColumn[j] = Program.level.tiles[i, j].typeID;
                                        lc[15 - (i % 16)].VariantsColumn[j] = Program.level.tiles[i, j].variantID;
                                    }

                                    if (i % 16 == 15)
                                    {
                                        Send(lc);

                                        Log("Sent column packet");

                                        Thread.Sleep(5);
                                    }
                                }


                                //Focused Object
                                Send(new KeyValuePair<Guid, GameObject>(FocusedObjectID, Program.level.gameObjects.GetValueOrDefault(FocusedObjectID)));

                                Log("Sent focused object");
                            }


                            if (HasMap)
                            {
                                //All Objects
                                Send(Program.level.gameObjects);
                                Log("Sent game objects");


                                //All Entities
                                Send(Program.level.gameEntities);

                                Log("Sent game entities");
                            }

                        }
                    }
                    catch(Exception e)
                    {
                        Log($"Error while handling client '{_tcpClient.Client.RemoteEndPoint}': {e}");
                        RemovePlayer();
                        Program.leavingPlayers.Add(this);
                    }
                    

                    Thread.Sleep(30);
                }
            });
        }


        void HandleMessage(object obj)
        {
            if (obj is Keys[])
            {
                InputState = new KeyboardState(obj as Keys[]);

            }
            else if (obj is string)
            {
                if ((string)obj == "MapAffirmation")
                {
                    HasMap = true;
                }
            }
            else new Exception("Unknown object type");

            LastUpdate = DateTime.Now;
        }

        public void Log(string text) => Console.WriteLine("[Server] " + text);

        public void Send(object obj)
        {
            lock(_sendCeras)
                _sendCeras.WriteToStream(_netStream, obj);

        }
    }
}
