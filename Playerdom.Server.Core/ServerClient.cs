using Ceras;
using Ceras.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared;
using Playerdom.Shared.Models;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Playerdom.Server
{
    public class ServerClient : IDisposable
    {

        private static ulong userIDCounter = 0; //TODO: Remove and replace with a system for users to identify themselves or log in 

        readonly TcpClient _tcpClient;
        readonly NetworkStream _netStream;
        readonly CerasSerializer _sendCeras;
        readonly CerasSerializer _receiveCeras;

        public ulong? UserID { get; set; } = null;

        public string EndPointString
        {
            get { return _tcpClient.Client.RemoteEndPoint.ToString(); }
        }

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

            UserID = userIDCounter;
            userIDCounter++;

            _sendCeras = new CerasSerializer(PlayerdomCerasSettings.config);
            _receiveCeras = new CerasSerializer(PlayerdomCerasSettings.config);

            Log("Player Joined");
            Program.chatLog.Enqueue(new ChatMessage() { senderID = 0, message = "[SERVER]: Player Joined", timeSent = DateTime.Now, textColor = Color.Orange });

            FocusedObjectID = Guid.NewGuid();
            InputState = new KeyboardState();
            LastUpdate = DateTime.Now;

        }

        public void Start()
        {
            StartReceivingMessages();
            StartSendingMessages();
        }

        public void InitializePlayer()
        {
            Program.level.gameObjects.TryAdd(FocusedObjectID, new Player(new Point(0, 0), new Vector2(Tile.SIZE_X, Tile.SIZE_Y), displayName: "Player"));
        }

        void StartReceivingMessages()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Keep receiving packets from the client and respond to them
                    // Eventually when the client disconnects we'll just get an exception and end the thread...
                    while (Program.clients.ContainsKey(this.EndPointString))
                    {
                        var obj = await _receiveCeras.ReadFromStream(_netStream);
                        HandleMessage(obj);
                    }
                }
                catch (Exception e)
                {
                    if (!Program.leavingPlayers.Contains(EndPointString))
                        Program.leavingPlayers.Enqueue(EndPointString);
                    LogServerException(e);
                }
            });
        }


        void StartSendingMessages()
        {
            Task.Run(async () =>
            {
                while (Program.clients.ContainsKey(this.EndPointString))
                {
                    try
                    {
                        //lock (_sendCeras)
                        {
                            if (NeedsAllInfo)
                            {
                                NeedsAllInfo = false;
                                //Tile Data
                                MapColumn[] lc = new MapColumn[32];

                                for(int i = 0; i < 32; i++)
                                {
                                    lc[i] = new MapColumn(0, new ushort[Map.SIZE_X], new byte[Map.SIZE_Y]);
                                }



                                for (uint i = 0; i < Map.SIZE_X; i++)
                                {

                                    lc[i % 32].ColumnNumber = i;
                                    for (int j = 0; j < Map.SIZE_Y; j++)
                                    {
                                        lc[i % 32].TypesColumn[j] = Program.level.tiles[i, j].typeID;
                                        lc[i % 32].VariantsColumn[j] = Program.level.tiles[i, j].variantID;
                                    }

                                    if (i % 32 == 31)
                                    {
                                        Send(lc);

                                    }
                                }



                                //Focused Object
                                Program.level.gameObjects.TryGetValue(FocusedObjectID, out GameObject g);
                                Send(new KeyValuePair<Guid, GameObject>(FocusedObjectID, g));


                            }


                            if (HasMap)
                            {
                                //All Objects
                                Send(Program.level.gameObjects);


                                //All Entities
                                Send(Program.level.gameEntities);

                                //All chat messages
                                Send(Program.chatLog.ToList());
                            }

                        }
                    }
                    catch(Exception e)
                    {
                        //Log($"Error while handling client '{_tcpClient.Client.RemoteEndPoint}': {e}");
                        if (!Program.leavingPlayers.Contains(EndPointString))
                            Program.leavingPlayers.Enqueue(EndPointString);

                        LogServerException(e);
                    }


                    Thread.Sleep(20);
                }
            });
        }


        void HandleMessage(object obj)
        {
            if (obj is KeyboardState)
            {
                InputState = (KeyboardState)obj;

            }
            else if (obj is string)
            {
                if ((string)obj == "MapAffirmation")
                {
                    HasMap = true;
                }
            }
            else if(obj is KeyValuePair<string, string> pair)
            {
                string pairValue;
                switch (pair.Key)
                {
                    default:
                        throw new Exception("Unknown object type");
                    case "ChatMessage":
                        if (UserID != null)
                        {

                            pairValue = pair.Value;
                            if (pairValue.Length > 256)
                                pairValue = pairValue.Substring(0, 256);



                            if (pairValue[0] == '/')
                                ProcessCommand(pairValue);
                            else
                                Program.chatLog.Enqueue(new ChatMessage { message = string.Format("[{0}]: {1}", GetUsername(), pairValue), senderID = UserID.Value, timeSent = DateTime.Now, textColor = Color.White });
                        }
                        break;
                }
            }
            else throw new Exception("Unknown object type");

            LastUpdate = DateTime.Now;
        }

        public void Log(string text) => Console.WriteLine("{0:HH:mm}", DateTime.Now + " [Server] " + text);

        public void Send(object obj)
        {
            if(Program.clients.ContainsKey(this.EndPointString))
                _sendCeras.WriteToStream(_netStream, obj);

        }




        static void LogServerException(Exception e)
        {
            string logPath = Environment.CurrentDirectory + "\\Logs\\error_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss-tt") + ".txt";

            if (!Directory.Exists(Environment.CurrentDirectory + "\\Logs"))
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\Logs");

            FileStream logFile = System.IO.File.Create(logPath);
            StreamWriter logWriter = new System.IO.StreamWriter(logFile);
            logWriter.WriteLine(e.GetType().ToString());
            logWriter.WriteLine(e.Message);
            logWriter.WriteLine(e.StackTrace);
            logWriter.Close();
            logFile.Close();
            logWriter.Dispose();
        }



        private string nickName = null;

        public string GetUsername()
        {
            //TODO: Create Sysstem to retrieve a username or profile

            if (UserID == null)
                throw new NullReferenceException("User doesn't have an ID");


            if (nickName == null)
                return "Player " + UserID.Value;
            else return nickName;
        }

        public void Dispose()
        {
            _tcpClient.Close();
            _tcpClient.Dispose();
        }



        private void ProcessCommand(string command)
        {
            command = command.Substring(1);

            string[] args = command.Split(' ');


            if (args[0] == "nick" && args.Length == 2 && args[1].Length <= 48)
            {

                if(Program.clients.Count(c => c.Value.nickName == args[1]) == 0)
                {
                    string oldName = GetUsername();


                    nickName = args[1];

                    try
                    {
                        Program.level.gameObjects[this.FocusedObjectID].SetDisplayName(args[1]);

                        Program.chatLog.Enqueue(new ChatMessage { message = string.Format("[Server]: {0} is now known as {1}", oldName, nickName), senderID = UserID.Value, timeSent = DateTime.Now, textColor = Color.Yellow });
                    }
                    catch(Exception e)
                    {

                    }
                }
            }
        }
    }
}
