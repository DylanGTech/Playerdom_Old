using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ceras;
using Ceras.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Server.Core;
using Playerdom.Server.Core.Data;
using Playerdom.Shared;
using Playerdom.Shared.Models;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;

namespace Playerdom.Server.Core
{
    public sealed class ServerClient : IDisposable
    {

        public bool IsLoggedIn { get; set; } = false;

        public static LocalDatabase ldb = null;

        readonly TcpClient _tcpClient;
        readonly NetworkStream _netStream;
        readonly CerasSerializer _sendCeras;
        readonly CerasSerializer _receiveCeras;
        readonly CerasSerializer _fileCeras;

        public long? UserID { get; set; }




        public string EndPointString => _tcpClient.Client.RemoteEndPoint.ToString();

        public DateTime LastUpdate { get; set; }

        public Guid FocusedObjectID { get; set; }
        public bool HasMap { get; set; }
        public bool NeedsAllInfo { get; set; } = true;

        public KeyboardState InputState { get; set; }

        public ServerClient(TcpClient tcpClient)
        {

            if(ldb == null)
            {
                ldb = new LocalDatabase(Environment.CurrentDirectory);
            }

            _tcpClient = tcpClient;
            _netStream = tcpClient.GetStream();

            _sendCeras = new CerasSerializer(PlayerdomCerasSettings.Config);
            _receiveCeras = new CerasSerializer(PlayerdomCerasSettings.Config);
            _fileCeras = new CerasSerializer(PlayerdomCerasSettings.Config);

            Log("Player Joined");
            //TODO: Parse dates into shorter HH:mm formats.
            Program.ChatLog.Enqueue(new ChatMessage { senderID = 0, message = DateTime.Now.ToString("HH:mm") + " [SERVER]: Player Joined ", timeSent = DateTime.Now, textColor = Color.Red });

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
            Player loadedPlayer = null;

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Players")))
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Players"));

            try
            {
                loadedPlayer = _fileCeras.Deserialize<Player>(File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "Players", UserID.Value + ".bin")));
                Log("Loaded Player Profile");
            }
            catch(Exception e)
            {
                loadedPlayer = new Player(MapService.GenerateSpawnPoint(Program.level), new Vector2(Tile.SIZE_X, Tile.SIZE_Y), displayName: "Player");


                File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "Players", UserID.Value + ".bin"), _fileCeras.Serialize(loadedPlayer));
                Log("Created Player Profile");
            }

            Program.level.gameObjects.TryAdd(FocusedObjectID, loadedPlayer);
        }

        private void StartReceivingMessages()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Keep receiving packets from the client and respond to them
                    // Eventually when the client disconnects we'll just get an exception and end the thread...
                    while (Program.Clients.ContainsKey(EndPointString))
                    {
                        var obj = await _receiveCeras.ReadFromStream(_netStream);
                        HandleMessage(obj);
                    }
                }
                catch (Exception e)
                {
                    if (!Program.LeavingPlayers.Contains(EndPointString))
                        Program.LeavingPlayers.Enqueue(EndPointString);
                    LogServerException(e);
                }
            });
        }

        public void SavePlayerStats()
        {
            if(UserID != null)
                File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "Players", UserID.Value + ".bin"), _fileCeras.Serialize(Program.level.gameObjects[FocusedObjectID] as Player));
        }


        private void StartSendingMessages()
        {
            Task.Run(() =>
            {
                while (Program.Clients.ContainsKey(EndPointString))
                {
                    try
                    {
                        if(IsLoggedIn)
                        {
                            if (NeedsAllInfo)
                            {
                                NeedsAllInfo = false;
                                //Tile Data
                                MapColumn[] lc = new MapColumn[32];

                                for (int i = 0; i < 32; i++)
                                {
                                    lc[i] = new MapColumn(0, new ushort[Map.SizeX], new byte[Map.SizeY]);
                                }

                                for (uint i = 0; i < Map.SizeX; i++)
                                {

                                    lc[i % 32].ColumnNumber = i;
                                    for (int j = 0; j < Map.SizeY; j++)
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
                                Send(Program.ChatLog.ToList());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //Log($"Error while handling client '{_tcpClient.Client.RemoteEndPoint}': {e}");
                        if (!Program.LeavingPlayers.Contains(EndPointString))
                            Program.LeavingPlayers.Enqueue(EndPointString);

                        LogServerException(e);
                    }
                    // NEVER USE THREAD.SLEEP
                    Thread.Sleep(20);
                }
            });
        }

        private void HandleMessage(object obj)
        {
            switch (obj)
            {
                case KeyboardState state:
                    InputState = state;
                    break;
                case string s:
                    if (s == "MapAffirmation")
                    {
                        HasMap = true;
                    }

                    break;
                case KeyValuePair<string, string> pair:
                    switch (pair.Key)
                    {
                        default:
                            throw new Exception("Unknown object type");
                        case "ChatMessage":
                            if (UserID != null)
                            {

                                string pairValue = pair.Value;
                                PlayerEntry current = ldb.GetPlayer(UserID.Value);



                                if (pairValue.Length > 256)

                                    pairValue = pairValue.Substring(0, 256);

                                if (pairValue[0] == '/')
                                    ProcessCommand(pairValue, current.IsAdmin);
                                else
                                    Program.ChatLog.Enqueue(new ChatMessage
                                    {
                                        message =
                                            $"{DateTime.Now:HH:mm} [{current.Username}]: {pairValue}",
                                        senderID = UserID.Value,
                                        timeSent = DateTime.Now,
                                        textColor = (current.IsAdmin) ? Color.Gold : Color.White
                                    });
                            }
                            break;
                    }
                    break;
                case Guid token:
                    if(!IsLoggedIn)
                    {
                        Guid newToken = Guid.NewGuid();
                        long? potentialID = ldb.GetPlayerID(token);
                        if (potentialID != null)
                        {
                            UserID = potentialID.Value;
                            ldb.UpdatePlayerToken(UserID.Value, newToken);
                        }
                        else
                        {
                            UserID = ldb.CreateNewPlayer(newToken);
                        }
                        Send(newToken);
                    }
                    break;
                default:
                    throw new Exception("Unknown object type");
            }

            LastUpdate = DateTime.Now;
        }

        public static void Log(string text) => Console.WriteLine("{0:HH:mm}", DateTime.Now.ToString("HH:mm") + " [Server] " + text);

        private void Send(object obj)
        {
            if (Program.Clients.ContainsKey(EndPointString))
                _sendCeras.WriteToStream(_netStream, obj);
        }

        private static void LogServerException(Exception e)
        {
            string logPath = Path.Combine(Environment.CurrentDirectory, "Logs", "error_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss-tt") + ".txt");

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Logs")))
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Logs"));

            FileStream logFile = File.Create(logPath);
            StreamWriter logWriter = new StreamWriter(logFile);
            logWriter.WriteLine(e.GetType().ToString());
            logWriter.WriteLine(e.Message);
            logWriter.WriteLine(e.StackTrace);
            logWriter.Close();
            logFile.Close();
            logWriter.Dispose();
        }

        public void Dispose()
        {
            _tcpClient.Close();
            _tcpClient.Dispose();
        }

        private void ProcessCommand(string command, bool asAdministrator = false)
        {
            command = command.Substring(1);

            string[] args = command.Split(' ');

            if (args[0] != "nick" || args.Length != 2 || args[1].Length > 48) return;
            if (ldb.CheckUsernameExistance(args[1]) || UserID == null) return;

            //Extremely basic username registration filter
            //This prevents players from screwing up the new user registration
            if (args[1].Length >= 6 && args[1].Substring(0, 6) == "Player") return;


            string oldName = ldb.GetPlayer(UserID.Value).Username;


            ldb.UpdatePlayerUsername(UserID.Value, args[1]);

            Program.level.gameObjects[FocusedObjectID].SetDisplayName(args[1]);

            if (UserID != null)
                Program.ChatLog.Enqueue(new ChatMessage
                {
                    message =
                        $"{DateTime.Now:HH:mm} [Server]: {oldName} is now known as {args[1]}",
                    senderID = UserID.Value,
                    timeSent = DateTime.Now,
                    textColor = Color.Red
                });
        }
    }
}
