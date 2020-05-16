using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;

namespace Roblot
{
    public sealed class Cooldown
    {

        public int cooldownTime = 10; //in seconds - time for each cooldown period TODO: Change to minutes

        public DateTime previousCommandTime; //time the command was sent

        private bool allowCommand = true;

        public double CheckCoolDown(DSharpPlus.Entities.DiscordMessage msg) 
        {
            var currentMessage = msg.Timestamp.DateTime;
            var timeElapsed = currentMessage.Subtract(previousCommandTime).TotalSeconds;

            if (allowCommand || timeElapsed >= cooldownTime) //we are allowed to run the command!
            {
                allowCommand = false;
                return -1;
            }
            else
            {
                return Math.Round(cooldownTime - timeElapsed);
            }
        }

        public void startCooldown(DateTime time)
        {
            previousCommandTime = time; //store the time of when the command is finished
        }
    }
}
