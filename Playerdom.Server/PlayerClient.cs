using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Server
{
    public class PlayerClient
    {
        public int PeerID { get; set; }
        public Guid FocusedObjectID { get; set; }
        public bool HasMap { get; set; }
        public KeyboardState InputState { get; set; }
    }
}
