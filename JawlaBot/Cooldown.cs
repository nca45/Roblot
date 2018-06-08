using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;

namespace JawlaBot
{
    class Cooldown
    {
        private int cooldownTime = 10; //in seconds - time for each cooldown period TODO: Change to minutes

        public DateTime previousCommandTime; //time the command was sent

        private bool allowCommand = true;

        public bool CheckCoolDown(DSharpPlus.Entities.DiscordMessage msg) 
        {
            var currentMessage = msg.Timestamp.DateTime;
            var timeElapsed = currentMessage.Subtract(previousCommandTime).TotalSeconds;

            if (allowCommand || timeElapsed >= cooldownTime) //we are allowed to run the command!
            {
                allowCommand = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void startCooldown(DateTime time)
        {
            previousCommandTime = time; //store the time of when the command is finished
        }
    }
}
