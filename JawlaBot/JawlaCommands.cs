﻿using System;
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

        [Description("Gives a record of which people the user owes and how much")]
        [Command("whoiowe")]
        public async Task WhoIOwe(CommandContext ctx)
        {
            List<IOwe> usersIOwe = dbConnect.WhoIowe(ctx.Member.Id.ToString());
            int count = 0;
            string finalString = "";
            for(int i = 0; i < usersIOwe.Count; i++)
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
            await ctx.RespondAsync(embed:embed);
        }

        [Description("Gives a record of who owes the user and how much")]
        [Command("whoowesme")]
        public async Task WhoOwesMe(CommandContext ctx)
        {
            List<OwesMe> usersOweMe = dbConnect.WhoOwesMe(ctx.Member.Id.ToString());
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

        [Description("Records an amount of money the user owes to")]
        [Command("iowe")] // testing for IOwe - for debugging it will owe a fake person
        public async Task Iowe(CommandContext ctx, [Description("The person who the user owes money to")] DiscordMember member, double amount)
        {
            if(ctx.Member.Username == member.Username)
            {
                await ctx.RespondAsync("You owe yourself money? :thinking:");
            }
            else if (member.IsBot)
            {
                await ctx.RespondAsync("You owe money to a bot? :thinking:");
            }
            else if(amount <= 0)
            {
                await ctx.RespondAsync("You can't owe someone $0 or less c'mon man.");
            }
            else
            {
               // await ctx.RespondAsync($"you called the member {member.Mention} with the amount of {amount}");

                dbConnect.UserExists(ctx.Member.Id.ToString()); //check if calling user already has a document, create it if not
                dbConnect.UserOwes(ctx.Member.Id.ToString(), member.Id.ToString(), amount);

                //then update the payee

                dbConnect.UserExists(member.Id.ToString());
                dbConnect.UserIsOwed(member.Id.ToString(), ctx.Member.Id.ToString(), amount);
                // present the poll
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Member.DisplayName} has added ${amount} to the amount they owe you."
                };
                var msg = await ctx.RespondAsync($"{member.Mention}", embed: embed);
            }
        }
        [Description("Pays the user x amount of money if owed")]
        [Command("pay")]
        public async Task Pay(CommandContext ctx, DiscordMember payee, string amount)
        {
            bool isNum = Double.TryParse(amount, out double amountToBePaid);

            //If user types in 'full', pay the amount in full whatever owed
            //else, subtract the amount from amount owed, so long as it's less than the amount owed
            //if it is more than the amount owed, send back an error
            //can't pay himself or a bot, also check 'amount' to make sure it is either a number or 'full'
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
            else if(!isNum && amount != "full")
            {
                await ctx.RespondAsync("Error: Invalid amount");
            }
            else
            {
                double amountOwed = 0;
                List<IOwe> usersIOwe = dbConnect.WhoIowe(ctx.Member.Id.ToString());
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
                            await ctx.RespondAsync("Updating database...");

                            dbConnect.UserOwes(ctx.Member.Id.ToString(), payee.Id.ToString(), amountToBePaid*(-1));

                            dbConnect.UserIsOwed(payee.Id.ToString(), ctx.Member.Id.ToString(), amountToBePaid*(-1));

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
        [Description("Requests an amount of money from a user")]
        public async Task Oweme(CommandContext ctx, DiscordMember user, double amount)
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
            else if(user.Presence == null)
            {
                await ctx.RespondAsync($"{user.DisplayName} is offline right now!");
            }
            else
            {
                //get poll or confirmation by the person who is being requested
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

                if(poll_result != null && poll_result.Emoji.Name == confirmEmoji)
                {
                    await ctx.RespondAsync("Updating database...");
                    dbConnect.UserExists(user.Id.ToString()); 
                    dbConnect.UserOwes(user.Id.ToString(), ctx.Member.Id.ToString(), amount);

                    dbConnect.UserExists(ctx.Member.Id.ToString());
                    dbConnect.UserIsOwed(ctx.Member.Id.ToString(), user.Id.ToString(), amount);

                    await ctx.RespondAsync("Confirmed!");
                }
                else
                {
                    await ctx.RespondAsync("Request Declined");
                }
            }
        }

        [Command("test")]
        public async Task TestCommand(CommandContext ctx, DiscordMember member)
        {
            var testBool = Program.cooldown.CheckCoolDown(ctx.Message);
            await ctx.RespondAsync(member.Mention);
            if (testBool)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"This test is successful! {ctx.Message.Content.ToString()}");

                Program.cooldown.startCooldown(DateTime.Now);
            }
            else
            {
                await ctx.RespondAsync("You're on cooldown, wait a moment!");
            }
        }

        [Command("hooyah")] //this command is not needed anymore? Already in program.cs
        public async Task HooyahAsync(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
            var text = "test";
            await ctx.RespondAsync("Respond with 'test' to recieve a reaction!");

            var msg = await interactivity.WaitForMessageAsync(xm => xm.Content.Contains(text) && xm.Author.IsBot == false, TimeSpan.FromSeconds(15));
            if(msg != null)
            {
                var email = msg.User.Mention;
                await ctx.RespondAsync($"{email} has typed 'test!'");
            }
            else
            {
                await ctx.RespondAsync("Nobody typed 'test' :( I sad");
            }
        }
        [Command("greet")]
        public async Task Greet(CommandContext ctx, [Description("The user to say hi to.")] DiscordMember member) // this command takes a member as an argument; you can pass one by username, nickname, id, or mention
        {

            //let the channel know the bot is working
            await ctx.TriggerTypingAsync();

            // let's make the message a bit more colourful
            var emoji = DiscordEmoji.FromName(ctx.Client, ":wave:");

            // and finally, let's respond and greet the user.
            await ctx.RespondAsync($"{emoji} Hello, {member.Mention}!");
        }
        [Command("waitfortyping"), Description("Waits for a typing indicator.")] //might not need this, just for testing
        public async Task WaitForTyping(CommandContext ctx)
        {
            // first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();

            // then wait for author's typing
            await ctx.RespondAsync("type something");
            var chn = await interactivity.WaitForTypingChannelAsync(ctx.User, TimeSpan.FromSeconds(60));
            if (chn != null)
            {
                // got 'em
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{ctx.User.Mention}, you typed in {chn.Channel.Mention}!");
            }
            else
            {
                await ctx.RespondAsync("*yawn*");
            }
        }
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
                await ctx.RespondAsync("Already connected in this guild.");

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
                await ctx.RespondAsync("You need to be in a voice channel.");

            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync($"Joining Channel {ctx.Channel.Name}");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                await ctx.RespondAsync("Not connected in this guild.");

            vnc.Disconnect();
            await ctx.RespondAsync($"Leaving Channel {ctx.Channel.Name}");
        }

        [Command("yeahboi")]
        public async Task YeahBoy(CommandContext ctx)
        {
            var testBool = Program.cooldown.CheckCoolDown(ctx.Message);
            if (testBool)
            {
                await Join(ctx);

                var vnc = ctx.Client.GetVoiceNextClient().GetConnection(ctx.Guild);
                await vnc.SendSpeakingAsync(true);
                string file = @"C:\temp\yeahboi.mp3";
                var psi = new ProcessStartInfo
                {
                    FileName = @"C:\Users\Naysan\Source\Repos\JawlaBot\JawlaBot\ffmpeg.exe",
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
                        for (var i = br; i< buff.Length; i++)
                        {
                            buff[i] = 0;
                        }
                    await vnc.SendAsync(buff, 20);
                }
                await vnc.SendSpeakingAsync(false);
                await Leave(ctx); //leave right after the command is done
                Program.cooldown.startCooldown(DateTime.Now); //start the cooldown now
            }
            else
            {
                await ctx.RespondAsync("You're on cooldown, wait a moment!");
            }
        }
    }
}
