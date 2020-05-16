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
using Roblot.JSON_Classes;

namespace Roblot
{
    //BUG: WHEN A PERSON LEAVES THE DISCORD SERVER, IF THAT PERSON IS IN ANOTHER USER'S LIST OF OWEME/IOWE, THEN THE COMMANDS WILL NOT WORK BECAUSE WE CALL MEMBER.DISPLAYNAME AND THERE IS NO MEMBER ANYMORE IF THEY LEAVE
    //      User is still within the database but bot cannot parse the user's id because it doesn't exist

    //SOLUTION: Hide the user that left until they /if they return. When the user is back, we still have their outstanding debt.
    // Only have to modify whoiowe and whoowesme. If the member is gone there is no member variable to use for iowe, owesme and pay
    public class MoneyTrackerCommands:BaseCommandModule
    {
        private dbConnection Connection { get;}

        public MoneyTrackerCommands(dbConnection connection)
        {
            this.Connection = connection;
        }

        [Description("Gives a list of people who you owe")]
        [Command("whoiowe")]
        public async Task WhoIOwe(CommandContext ctx)
        {
            List<IOwe> usersIOwe = Connection.WhoIowe(ctx.Member.Id.ToString());
            int count = 0;
            string finalString = "";

            for (int i = 0; i < usersIOwe.Count; i++)
            {
                DiscordMember member;
                try
                {
                    member = await ctx.Guild.GetMemberAsync(UInt64.Parse(usersIOwe[i].id));
                }
                catch
                {
                    member = null;
                }
                if (usersIOwe[i].amount > 0 && member != null)
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

            await ctx.RespondAsync($"{ctx.Member.Mention}", embed: embed);
        }

        [Description("Gives a list of people who owe you")]
        [Command("whoowesme")]
        public async Task WhoOwesMe(CommandContext ctx)
        {
            List<OwesMe> usersOweMe = Connection.WhoOwesMe(ctx.Member.Id.ToString());
            int count = 0;
            string finalString = "";

            for (int i = 0; i < usersOweMe.Count; i++)
            {
                DiscordMember member;
                try
                {
                    member = await ctx.Guild.GetMemberAsync(UInt64.Parse(usersOweMe[i].id));
                }
                //Catch if the member doesn't exist in the channel anymore
                catch
                {
                    member = null;
                }

                if (usersOweMe[i].amount > 0 && member != null)
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

            await ctx.RespondAsync($"{ctx.Member.Mention}", embed: embed);
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
                Connection.UserExists(ctx.Member.Id.ToString()); //check if calling user already has a document, create it if not
                await Connection.UserOwes(ctx.Member.Id.ToString(), member.Id.ToString(), amount);
                //then update payee
                Connection.UserExists(member.Id.ToString());
                await Connection.UserIsOwed(member.Id.ToString(), ctx.Member.Id.ToString(), amount);
                // present the poll
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Member.DisplayName} has added ${amount} to the amount they owe you."
                };
                var msg = await ctx.RespondAsync($"{member.Mention}", embed: embed);
                await WhoIOwe(ctx);
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
                List<IOwe> usersIOwe = Connection.WhoIowe(ctx.Member.Id.ToString());
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
                        var interactivity = ctx.Client.GetInteractivity();

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
                        var poll_result = await interactivity.WaitForReactionAsync(xm => xm.Emoji == confirmEmoji || xm.Emoji == declineEmoji, msg, payee, pollDuration); //wait for reaction on this particular message
                        if (poll_result.Result != null && poll_result.Result.Emoji.Name == confirmEmoji)
                        {
                            await Connection.UserOwes(ctx.Member.Id.ToString(), payee.Id.ToString(), amountToBePaid * (-1));
                            await Connection.UserIsOwed(payee.Id.ToString(), ctx.Member.Id.ToString(), amountToBePaid * (-1));

                            await ctx.RespondAsync("Payment Confirmed!");
                            await msg.DeleteAsync();
                            await WhoIOwe(ctx);
                        }
                        else
                        {
                            await msg.DeleteAsync();
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
                var interactivity = ctx.Client.GetInteractivity();

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
                var poll_result = await interactivity.WaitForReactionAsync(xm => xm.Emoji == confirmEmoji || xm.Emoji == declineEmoji, msg, user, pollDuration); //wait for reaction on this particular message

                if (poll_result.Result != null && poll_result.Result.Emoji.Name == confirmEmoji)
                {
                    Connection.UserExists(user.Id.ToString());
                    await Connection.UserOwes(user.Id.ToString(), ctx.Member.Id.ToString(), amount);

                    Connection.UserExists(ctx.Member.Id.ToString());
                    await Connection.UserIsOwed(ctx.Member.Id.ToString(), user.Id.ToString(), amount);

                    await ctx.RespondAsync("Confirmed!");
                    await msg.DeleteAsync();
                    await WhoOwesMe(ctx);
                }
                else
                {
                    await msg.DeleteAsync();
                    await ctx.RespondAsync("Request Declined");
                }
            }
        }
    }
}
