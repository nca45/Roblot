using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using MongoDB.Driver.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using JawlaBot.JSON_Classes;

namespace JawlaBot
{
    class JawlaCommands
    {

        [Description("Gives a list of people who you owe")]
        [Command("whoiowe")]
        public async Task WhoIOwe(CommandContext ctx)
        {
            List<IOwe> usersIOwe = dbConnection.WhoIowe(ctx.Member.Id.ToString());
            int count = 0;
            string finalString = "";

            for (int i = 0; i < usersIOwe.Count; i++)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(UInt64.Parse(usersIOwe[i].id));
                if (usersIOwe[i].amount > 0)
                {
                    finalString += member.DisplayName + $": ${usersIOwe[i].amount} \n";
                    count++;
                }
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = (count == 1) ? "You currently owe 1 person" : $"You currently owe {count} people",
                Description = finalString
            };

            await ctx.RespondAsync(embed: embed);
        }

        [Description("Gives a list of people who owe you")]
        [Command("whoowesme")]
        public async Task WhoOwesMe(CommandContext ctx)
        {
            List<OwesMe> usersOweMe = dbConnection.WhoOwesMe(ctx.Member.Id.ToString());
            int count = 0;
            string finalString = "";

            for (int i = 0; i < usersOweMe.Count; i++)
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(UInt64.Parse(usersOweMe[i].id));
                if (usersOweMe[i].amount > 0)
                {
                    finalString += member.DisplayName + $": ${usersOweMe[i].amount} \n";
                    count++;
                }
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = (count == 1) ? "1 person currently owes you" : $"{count} people currently owe you",
                Description = finalString
            };

            await ctx.RespondAsync(embed: embed);
        }

        [Description("Records an amount of money you owe someone")]
        [Command("iowe")] 
        public async Task Iowe(CommandContext ctx, [Description("The person who the user owes money to")] DiscordMember member, [Description("The amount of money owed")] double amount)
        {
            if (ctx.Member.Username == member.Username)
            {
                await ctx.RespondAsync("You owe yourself money? :thinking:");
            }
            else if (member.IsBot)
            {
                await ctx.RespondAsync("You owe money to a bot? :thinking:");
            }
            else if (amount <= 0)
            {
                await ctx.RespondAsync("You can't owe someone $0 or less c'mon man.");
            }
            else
            {
                dbConnection.UserExists(ctx.Member.Id.ToString()); //check if calling user already has a document, create it if not
                dbConnection.UserOwes(ctx.Member.Id.ToString(), member.Id.ToString(), amount);
                //then update payee
                dbConnection.UserExists(member.Id.ToString());
                dbConnection.UserIsOwed(member.Id.ToString(), ctx.Member.Id.ToString(), amount);
                // present the poll
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Member.DisplayName} has added ${amount} to the amount they owe you."
                };
                var msg = await ctx.RespondAsync($"{member.Mention}", embed: embed);
            }
        }

        [Description("Pays someone x amount of money if owed")]
        [Command("pay")]
        public async Task Pay(CommandContext ctx, [Description("Who you are paying")] DiscordMember payee, [Description("The amount of money paid")] string amount)
        {
            bool isNum = Double.TryParse(amount, out double amountToBePaid);

            if (ctx.Member.Username == payee.Username)
            {
                await ctx.RespondAsync("You can't pay yourself money!");
            }
            else if (payee.IsBot)
            {
                await ctx.RespondAsync("You can't pay a bot!");
            }
            else if (isNum && (amountToBePaid == 0 || amountToBePaid < 0))
            {
                await ctx.RespondAsync("Error: Amount paid cannot be less than or equal to 0");
            }
            else if (payee.Presence == null)
            {
                await ctx.RespondAsync($"{payee.DisplayName} is offline right now!");
            }
            else if (!isNum && amount != "full")
            {
                await ctx.RespondAsync("Error: Invalid amount");
            }
            else
            {
                double amountOwed = 0;
                List<IOwe> usersIOwe = dbConnection.WhoIowe(ctx.Member.Id.ToString());
                bool userFound = false;
                int count = 0;

                while (!userFound && count < usersIOwe.Count)
                {
                    if (payee.Id.ToString() == usersIOwe[count].id && usersIOwe[count].amount > 0)
                    {
                        amountOwed = usersIOwe[count].amount;
                        userFound = true;
                    }
                    count++;
                }
                if (!userFound)
                {
                    await ctx.RespondAsync($"I can't find {payee.DisplayName} in the list of people you owe");
                }
                else
                {
                    if (amountToBePaid > amountOwed)
                    {
                        await ctx.RespondAsync("Error: Amount to be paid exceeds the amount you owe. Try using ```!pay (name) full``` if you wish to pay the full amount.");
                    }
                    else
                    {
                        amountToBePaid = (amount == "full") ? amountOwed : amountToBePaid;

                        var pollDuration = TimeSpan.FromSeconds(30);
                        var interactivity = ctx.Client.GetInteractivityModule();

                        var confirmEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
                        var declineEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
                        // present the poll
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{ctx.Member.DisplayName} is requesting to pay you ${amountToBePaid}. Is this amount correct?"
                        };
                        var msg = await ctx.RespondAsync($"{payee.Mention}", embed: embed);

                        // add the options as reactions
                        await msg.CreateReactionAsync(confirmEmoji);
                        await msg.CreateReactionAsync(declineEmoji);

                        // get reactions
                        var poll_result = await interactivity.WaitForMessageReactionAsync(xm => xm == confirmEmoji || xm == declineEmoji, msg, payee, pollDuration); //wait for reaction on this particular message
                        if (poll_result != null && poll_result.Emoji.Name == confirmEmoji)
                        {
                            dbConnection.UserOwes(ctx.Member.Id.ToString(), payee.Id.ToString(), amountToBePaid * (-1));
                            dbConnection.UserIsOwed(payee.Id.ToString(), ctx.Member.Id.ToString(), amountToBePaid * (-1));

                            await ctx.RespondAsync("Payment Confirmed!");
                        }
                        else
                        {
                            await ctx.RespondAsync("Request Declined");
                        }
                    }
                }
            }
        }

        [Command("owesme")]
        [Aliases("oweme")]
        [Description("Requests an amount of money from someone")]
        public async Task Oweme(CommandContext ctx, [Description("The person you are requesting money from")] DiscordMember user, [Description("The amount of money requested")] double amount)
        {
            if (ctx.Member.Username == user.Username)
            {
                await ctx.RespondAsync("You owe yourself money? :thinking:");
            }
            else if (user.IsBot)
            {
                await ctx.RespondAsync("A bot owes you money? :thinking:");
            }
            else if (amount <= 0)
            {
                await ctx.RespondAsync("Someone can't owe you $0 or less c'mon man.");
            }
            else if (user.Presence == null)
            {
                await ctx.RespondAsync($"{user.DisplayName} is offline right now!");
            }
            else
            {
                var pollDuration = TimeSpan.FromSeconds(30);
                var interactivity = ctx.Client.GetInteractivityModule();

                var confirmEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
                var declineEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
                // present the poll
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Member.DisplayName} is requesting ${amount} from you. Is this amount correct?"
                };
                var msg = await ctx.RespondAsync($"{user.Mention}", embed: embed);

                // add the options as reactions
                await msg.CreateReactionAsync(confirmEmoji);
                await msg.CreateReactionAsync(declineEmoji);

                // get reactions
                var poll_result = await interactivity.WaitForMessageReactionAsync(xm => xm == confirmEmoji || xm == declineEmoji, msg, user, pollDuration); //wait for reaction on this particular message

                if (poll_result != null && poll_result.Emoji.Name == confirmEmoji)
                {
                    dbConnection.UserExists(user.Id.ToString());
                    dbConnection.UserOwes(user.Id.ToString(), ctx.Member.Id.ToString(), amount);

                    dbConnection.UserExists(ctx.Member.Id.ToString());
                    dbConnection.UserIsOwed(ctx.Member.Id.ToString(), user.Id.ToString(), amount);

                    await ctx.RespondAsync("Confirmed!");
                }
                else
                {
                    await ctx.RespondAsync("Request Declined");
                }
            }
        }

        [Command("listcooldowns")]
        [Aliases("listcooldown", "cooldowntimes")]
        [Description("Lists the current cooldowns for each command")]
        public async Task ListCooldown(CommandContext ctx)
        {
            string listofCooldowns = "";
            var keys = JawlaBot.audioCategories.Keys.ToArray();

            foreach (var cooldown in keys)
            {
                listofCooldowns += $"{cooldown}: " + JawlaBot.audioCategories[cooldown].cooldownTime + " seconds\n";
            }
            DiscordEmbed embed = new DiscordEmbedBuilder
            {
                Title = "Here's the current list of cooldown times for each command.",
                Description = listofCooldowns
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("setcooldown")]
        [Aliases("setcooldowntime", "cooldown")]
        [Description("Sets the cooldown of the voice memes. You must be an admin to change this setting")]
        public async Task SetCooldown(CommandContext ctx, [Description("The command you want to change")] string command, [Description("The new cooldown time in seconds. Maximum time is 600 seconds (10 minutes)")] int newtime)
        {
            Cooldown currCooldown = null;
            JawlaBot.audioCategories.TryGetValue(command, out currCooldown);
            DiscordEmbed embed = null;
            var roles = ctx.Member.Roles;
            string finalmsg = "";
            bool isadmin = false;
            foreach(var currRole in roles)
            {

                isadmin = (currRole.Name == "Admin") ? true : false;
            }
            if (isadmin && currCooldown != null)
            {
                if(newtime < 0 || newtime > 600)
                {
                    finalmsg = "Error: Cooldown time can't be less than 0 or greater than 10 minutes (600 seconds)";
                }
                else
                {
                    currCooldown.cooldownTime = newtime;
                    finalmsg = $"New cooldown time set for command '{command}' - {newtime} seconds.";
                }
            }
            else
            {
                finalmsg = "Error: Either you're not an admin or that command doesn't exist.";
                var arrayOfKeys = JawlaBot.audioCategories.Keys.ToArray();
                var listofkeys = "";
                foreach(var keys in arrayOfKeys)
                {
                    listofkeys += keys + "\n";
                }
                embed = new DiscordEmbedBuilder
                {
                    Title = "Here's the current list of audio command ids",
                    Description = listofkeys
                };
             }
                await ctx.RespondAsync(finalmsg, embed:embed);

        }

        [Command("pubgdrop")]
        [Aliases("drop")]
        [Description("Get a random location to drop in PUBG")]
        public async Task List(CommandContext ctx, [Description("The map you are playing on - Erangel/Forest or Miramar/Desert")] string map)
        {
            string location = "";

            if (map.ToLower() == "erangel" || map.ToLower() == "forest")
            {
                location = Lists.PUBGDrops("Erangel");
            }
            if (map.ToLower() == "miramar" || map.ToLower() == "desert")
            {
                location = Lists.PUBGDrops("Miramar");
            }
            await ctx.RespondAsync((location == "") ? ("That's not a valid map!") : ($"You should drop at {location}!"));
        }

        [Command("listrestaurants")]
        [Aliases("listeats")]
        [Description("Gets a list of restaurants frequently visited")]
        public async Task ListRestaurants(CommandContext ctx)
        {
            string finalList = "";
            string[] restaurants = Lists.ListRestaurants();

            for (int i = 0; i < restaurants.Length; i++)
            {
                finalList += restaurants[i] + "\n";
            }
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Here's the current list of restaurants I have",
                Description = finalList
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("pickrestaurant")]
        [Aliases("imhungry", "eats", "pickfood", "dinner")]
        [Description("Gets a random restaurant from the list")]
        public async Task PickRestaurant(CommandContext ctx)
        {
            await ctx.RespondAsync($"Let's go eat at {Lists.Restaurants()}.");
        }

        private async Task Join(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();
            var vnc = vnext.GetConnection(ctx.Guild);

            if (vnc != null)
                await ctx.RespondAsync("Already connected in this guild.");

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
                await ctx.RespondAsync("You need to be in a voice channel.");

            vnc = await vnext.ConnectAsync(chn);
        }

        private async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();
            var vnc = vnext.GetConnection(ctx.Guild);

            if (vnc == null)
                await ctx.RespondAsync("Not connected in this guild.");

            vnc.Disconnect();
        }

        private async Task StreamAudio(CommandContext ctx, string audiofile, string command)
        {
            var remainingTime = JawlaBot.audioCategories[command].CheckCoolDown(ctx.Message);
            if (remainingTime == -1)
            {
                await Join(ctx);
                var vnc = ctx.Client.GetVoiceNextClient().GetConnection(ctx.Guild);
                await vnc.SendSpeakingAsync(true);
                string file = audiofile;

                var psi = new ProcessStartInfo
                {
                    FileName = JawlaBot.currentDirectory + @"\ffmpeg.exe",
                    Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1 ",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var ffmpeg = Process.Start(psi);
                var ffout = ffmpeg.StandardOutput.BaseStream;
                var buff = new byte[3840];
                var br = 0;

                while ((br = ffout.Read(buff, 0, buff.Length)) > 0)
                {
                    if (br < buff.Length) //not a full sample, mute the rest
                        for (var i = br; i < buff.Length; i++)
                        {
                            buff[i] = 0;
                        }
                    await vnc.SendAsync(buff, 20);
                }
                await vnc.SendSpeakingAsync(false);
                await Leave(ctx); //leave right after the command is done
                JawlaBot.audioCategories[command].startCooldown(DateTime.Now); //start the cooldown now
            }
            else
            {
                await ctx.RespondAsync($"That command ({command}) is on cooldown, please wait {remainingTime} more seconds!");
            }
        }

        [Command("yeahboi")]
        [Description("Ask the bot to show you its longest yeah boy ever")]
        public async Task LongestYeahBoi(CommandContext ctx)
        {

            await StreamAudio(ctx, JawlaBot.currentDirectory + @"\yeahboi.ogg", "yeahboi");
        }

        [Command("stop")]
        [Aliases("timetostop", "frankstop", "itstimetostop", "notokay")]
        [Description("Ask the bot to let your friends know that this is not okay, and this needs to stop. Now.")]
        public async Task TimetoStop(CommandContext ctx)
        {
            await StreamAudio(ctx, GetRandomFile("frankstop"), "stop");
        }

        [Command("wow")]
        [Aliases("omgwow", "omg")]
        [Description("Express your excitement and surprise!")]
        public async Task Wow(CommandContext ctx)
        {
            await StreamAudio(ctx, GetRandomFile("wow"), "wow");
        }

        [Command("pranked")]
        [Aliases("frankprank", "prank", "gotem")]
        [Description("Ask the bot to let your friends know that they just got pranked.")]
        public async Task Pranked(CommandContext ctx)
        {
            await StreamAudio(ctx, GetRandomFile("frankprank"), "pranked");
        }

        [Command("frank")]
        [Aliases("filthyfrank", "idubbbz")]
        [Description("Just some random Filthy Frank + Idubbbz audio bites")]
        public async Task Frank(CommandContext ctx)
        {
            await StreamAudio(ctx, GetRandomFile("frank"), "frank");
        }

        private string GetRandomFile(string directory) //grabs random file from the directory
        {
            Random rnd = new Random();
            var fileName = System.IO.Directory.GetFiles(JawlaBot.currentDirectory + $@"\{directory}", "*ogg");
            return fileName[rnd.Next(0, fileName.Length)];
        }

        //some copy pasta memes
        [Group("memes", CanInvokeWithoutSubcommand = true)] // this makes the class a group, but with a twist; the class now needs an ExecuteGroupAsync method
        [Aliases("copypasta","meme")]
        [Description("Displays a random copypasta meme. No arguments will pick a random meme for you.")]
        public class ExampleExecutableGroup
        {
            public async Task ExecuteGroupAsync(CommandContext ctx) //no subcommandd will execute this method automatically
            {
                // random meme
                var rnd = new Random();
                var nxt = rnd.Next(0, 2);

                switch (nxt)
                {
                    case 0:
                        await Despacito(ctx);
                        return;

                    case 1:
                        await Fortnite(ctx);
                        return;

                    case 2:
                        await RickandMorty(ctx);
                        return;
                }
            }

            [Command("Despactio")]
            public async Task Despacito(CommandContext ctx)
            {
                await ctx.TriggerTypingAsync();

                await ctx.RespondAsync("So today in Spanish class, my teacher told us that we would be listening to a song in Spanish. Already, I began to tremble. I had a bad feeling about this. “Which one?” I ask shakily, not wanting to hear the answer. “Despacito” She responds. I begin to hyperventilate. My worst fears have been realized. I fade in and out of conciseness. I clamp my palms over my ears, but I know it’s futile. The song plays. I’m crying now, praying. God, Allah, Buddha please help me. I curl up on the floor. There’s nothing I can do now. And then it happens. The chorus plays. The girls in my class open their mouths. The screams of the damned, the shrieks of the tortured fill my ears and bounce around my skull. My eardrums rupture, blood leaking out. I try to scream, but no sound comes out. I can only sit there, violently shaking as it happens to me. After what seems like hours, it’s finally over. I try to move, but I cannot make myself. My brain shuts down as my vision fades to black. I muster the last of my energy, uttering the accursed word. \n“Despacito”");
            }

            [Command("Fortnite")]
            public async Task Fortnite(CommandContext ctx)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync("```Fortnite takes place in the same universe as The Powerpuff Girls. Now, before you start ranting, hear me out. Fortnite has Thanos in it. Thanos is in Marvel Vs. Capcom Infinite. Mega Man X is in Marvel vs. Capcom, and Mega Man X in the same universe as Mega Man, since Dr. Light created both of them. Mega Man is in Smash Bros, and so is Pac Man, who appears in Wreck-it Ralph. One scene in Wreck-it Ralph shows a Teenage Mutant Ninja Turtles arcade machine, and the Teenage Mutant Ninja Turtles have a comic book crossover with Batman. Scooby Doo and the Mystery Gang show up in an episode of Batman. Scooby Doo, of course, has a crossover with Johnny Bravo. Johnny Bravo is in an obscure Smash Bros. ripoff for the Wii, called Cartoon Network: Punch Time Explosion. All three Powerpuff Girls show up in this game. Therefore, my conclusion here is that Fortnite does in fact take place in the same universe as The Powerpuff Girls.```");
            }

            [Command("RickandMorty")]
            public async Task RickandMorty(CommandContext ctx)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync("My teacher said to my I'm a failure, that I'll never amount to anything. I scoffed at him. Shocked, my teacher asked what's so funny, my future is on the line. 'Well...you see professor' I say as the teacher prepares to laugh at my answer, rebuttal at hand. 'I watch Rick and Morty.' The class is shocked, they merely watch pleb shows like the big bang theory to feign intelligence, not grasping the humor. '...how ? I can't even understand it's sheer nuance and subtlety.' 'Well you see...WUBBA LUBBA DUB DUB!' One line student laughs in the back, I turn to see a who this fellow genius is. It's none other than Albert Einstein.");
            }
        }
    }
}
