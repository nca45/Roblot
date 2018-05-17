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
            Console.WriteLine("current datetime of message is " + currentMessage + " while the stored datetime is " + previousCommandTime);

            var timeElapsed = currentMessage.Subtract(previousCommandTime).TotalSeconds;

            Console.WriteLine("total seconds elapsed from last command is " + timeElapsed);
            if (allowCommand || timeElapsed >= cooldownTime) //we are allowed to run the command!
            {
                Console.WriteLine("allowing the command");
                allowCommand = false;
                return true;
            }
            else
            {
                Console.WriteLine("disallowing the command");
                return false;
            }
        }

        public void startCooldown(DateTime time)
        {
            previousCommandTime = time; //store the time of when the command is finished
        }
    }
}
