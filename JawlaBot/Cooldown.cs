using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;

namespace JawlaBot
{
    class Cooldown
    {
        public int cooldownTime = 10; //in seconds


        public String CheckCoolDown(DSharpPlus.EventArgs.MessageCreateEventArgs msg)
        {
            return msg.Message.Timestamp.ToString();
        }
    }
}
