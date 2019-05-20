using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared.Models
{
    public struct ChatMessage
    {
        public long senderID;
        public string message;
        public DateTime timeSent;
        public Color textColor;


        public ChatMessage(long senderID, string message, DateTime timeSent, Color textColor)
        {
            this.senderID = senderID;
            this.message = message;
            this.timeSent = timeSent;
            this.textColor = textColor;
        }
    }
}
