using Microsoft.Xna.Framework;
using System;

namespace Playerdom.Shared.Models
{
    public struct ChatMessage
    {
        public ulong senderID;
        public string message;
        public DateTime timeSent;
        public Color textColor;

        public ChatMessage(ulong senderID, string message, DateTime timeSent, Color textColor)
        {
            this.senderID = senderID;
            this.message = message;
            this.timeSent = timeSent;
            this.textColor = textColor;
        }
    }
}
