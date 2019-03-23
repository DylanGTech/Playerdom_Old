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

        public ServerClient(TcpClient tcpClient, Map m)
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
        }

        public void InitializePlayer(Map m)
        {
            m.gameObjects.Add(FocusedObjectID, new Player(new Point(0, 0), new Vector2(Tile.SIZE_X, Tile.SIZE_Y), displayName: "Player"));
        }

        public void RemovePlayer(Map m)
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
                }
            });
        }

        void HandleMessage(object obj)
        {
            if(obj is KeyboardState)
            {
                InputState = (KeyboardState)obj;
            }
            else if(obj is string)
            {
                if((string)obj == "MapAffrimation")
                {
                    HasMap = true;
                }
            }

            LastUpdate = DateTime.Now;
        }

        public void Log(string text) => Console.WriteLine("[Server] " + text);

        public void Send(object obj) => _sendCeras.WriteToStream(_netStream, obj);
    }
}
