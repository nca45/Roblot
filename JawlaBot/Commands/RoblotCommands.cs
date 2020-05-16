using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;

namespace Roblot
{
    public class RoblotCommands:BaseCommandModule
    {
        private Lists Lists { get; }
        private bool uwu { get; set; } = false;
        private CommandHelpers helper { get; }

        public RoblotCommands(Lists lists)
        {
            this.Lists = lists;
            helper = new CommandHelpers();
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

        [Command("soshlism")]
        [Aliases("socialism")]
        [Description("Sip away the threat of soshlism")]
        public async Task Soshlism(CommandContext ctx)
        {
            string output = string.Empty;
            for(int i = 0; i < 2; i++)
            {
                for(int j = 1; j < 3; j++)
                {
                    output += $"{DiscordEmoji.FromName(ctx.Client, $":soshlism_{j}_{i}:")}";
                }
                if(i < 2)
                {
                    output += "\n";
                }
            }

            await ctx.RespondAsync(output);
        }

        [Command("dongasm")]
        [Description("Wowza!")]
        public async Task Dongasm(CommandContext ctx)
        {
            string output = string.Empty;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    output += $"{DiscordEmoji.FromName(ctx.Client, $":dongasm_{j}_{i}:")}";
                }
                if (i < 2)
                {
                    output += "\n";
                }
            }

            await ctx.RespondAsync(output);
        }

        [Command("pickrestaurant")]
        [Aliases("imhungry", "eats", "pickfood", "dinner")]
        [Description("Gets a random restaurant from the list")]
        public async Task PickRestaurant(CommandContext ctx)
        {
            await ctx.RespondAsync($"Let's go eat at {Lists.Restaurants()}.");
        }

        [Command("uwu")]
        [Description("Toggles the uwu translator on/off")]
        public async Task UwuToggle(CommandContext ctx, [Description("'on' or 'off'")] string toggle)
        {
            if (toggle.ToLower() == "on")
            {
                uwu = true;
                ctx.Client.MessageCreated += EventHandlers.UwuTranslator;
                await ctx.RespondAsync("uwu on");
            }
            else if (toggle.ToLower() == "off")
            {
                uwu = false;
                ctx.Client.MessageCreated -= EventHandlers.UwuTranslator;
                await ctx.RespondAsync("uwu off");
            }
        }

        [Command("uwuthat")]
        [Description("Uwus the last message sent")]
        public async Task UwuThat(CommandContext ctx)
        {

            ulong id = ctx.Channel.LastMessageId;

            var messages = await ctx.Channel.GetMessagesBeforeAsync(id, 20);
            // look at all messages before
            foreach (var message in messages)
            {
                // grab the first previous message that isn't a command or a bot talking
                if (message.Content[0] != '!' && !message.Author.IsBot)
                {
                    Console.WriteLine(message.Content);
                    var candidateMessage = helper.Uwuify(message.Content);
                    await ctx.RespondAsync(candidateMessage);
                    return;
                }
            }
        }
        ////some copy pasta memes
        //// this makes the class a group, but with a twist; the class now needs an ExecuteGroupAsync method
        //[Group("memes")]
        //[Aliases("copypasta", "meme")]
        //[Description("Displays a random copypasta meme. No arguments will pick a random meme for you.")]
        //public class ExampleExecutableGroup
        //{
        //    [GroupCommand]
        //    public async Task ExecuteGroupAsync(CommandContext ctx) //no subcommandd will execute this method automatically
        //    {
        //        // random meme
        //        var rnd = new Random();
        //        var nxt = rnd.Next(0, 2);

        //        switch (nxt)
        //        {
        //            case 0:
        //                await Despacito(ctx);
        //                return;

        //            case 1:
        //                await Fortnite(ctx);
        //                return;

        //            case 2:
        //                await RickandMorty(ctx);
        //                return;
        //        }
        //    }

        //    [Command("Despactio")]
        //    public async Task Despacito(CommandContext ctx)
        //    {
        //        await ctx.TriggerTypingAsync();

        //        await ctx.RespondAsync("So today in Spanish class, my teacher told us that we would be listening to a song in Spanish. Already, I began to tremble. I had a bad feeling about this. “Which one?” I ask shakily, not wanting to hear the answer. “Despacito” She responds. I begin to hyperventilate. My worst fears have been realized. I fade in and out of conciseness. I clamp my palms over my ears, but I know it’s futile. The song plays. I’m crying now, praying. God, Allah, Buddha please help me. I curl up on the floor. There’s nothing I can do now. And then it happens. The chorus plays. The girls in my class open their mouths. The screams of the damned, the shrieks of the tortured fill my ears and bounce around my skull. My eardrums rupture, blood leaking out. I try to scream, but no sound comes out. I can only sit there, violently shaking as it happens to me. After what seems like hours, it’s finally over. I try to move, but I cannot make myself. My brain shuts down as my vision fades to black. I muster the last of my energy, uttering the accursed word. \n“Despacito”");
        //    }

        //    [Command("Fortnite")]
        //    public async Task Fortnite(CommandContext ctx)
        //    {
        //        await ctx.TriggerTypingAsync();
        //        await ctx.RespondAsync("```Fortnite takes place in the same universe as The Powerpuff Girls. Now, before you start ranting, hear me out. Fortnite has Thanos in it. Thanos is in Marvel Vs. Capcom Infinite. Mega Man X is in Marvel vs. Capcom, and Mega Man X in the same universe as Mega Man, since Dr. Light created both of them. Mega Man is in Smash Bros, and so is Pac Man, who appears in Wreck-it Ralph. One scene in Wreck-it Ralph shows a Teenage Mutant Ninja Turtles arcade machine, and the Teenage Mutant Ninja Turtles have a comic book crossover with Batman. Scooby Doo and the Mystery Gang show up in an episode of Batman. Scooby Doo, of course, has a crossover with Johnny Bravo. Johnny Bravo is in an obscure Smash Bros. ripoff for the Wii, called Cartoon Network: Punch Time Explosion. All three Powerpuff Girls show up in this game. Therefore, my conclusion here is that Fortnite does in fact take place in the same universe as The Powerpuff Girls.```");
        //    }

        //    [Command("RickandMorty")]
        //    public async Task RickandMorty(CommandContext ctx)
        //    {
        //        await ctx.TriggerTypingAsync();
        //        await ctx.RespondAsync("My teacher said to my I'm a failure, that I'll never amount to anything. I scoffed at him. Shocked, my teacher asked what's so funny, my future is on the line. 'Well...you see professor' I say as the teacher prepares to laugh at my answer, rebuttal at hand. 'I watch Rick and Morty.' The class is shocked, they merely watch pleb shows like the big bang theory to feign intelligence, not grasping the humor. '...how ? I can't even understand it's sheer nuance and subtlety.' 'Well you see...WUBBA LUBBA DUB DUB!' One line student laughs in the back, I turn to see a who this fellow genius is. It's none other than Albert Einstein.");
        //    }
        //}
    }
    public static class EventHandlers
    {
        //TODO: Keep all caps
        public static async Task UwuTranslator(MessageCreateEventArgs e)
        {
            String message = e.Message.Content;
            DiscordMember user = (DiscordMember)e.Author;
            CommandHelpers helper = new CommandHelpers();

            if(message[0] == '!') // if message starts with the command prefix
            {
                return;
            }


            // Change the message

            message = helper.Uwuify(message);


            // if message sent is not sent by the bot
            if (e.Author.Id != e.Client.CurrentUser.Id)
            {
                String finalMessage = $"{Formatter.Bold($"\n{user.Nickname} said:")}\n{message}\n";
                await e.Message.DeleteAsync();
                await e.Channel.SendMessageAsync(finalMessage);
            }
        }
    }

    public sealed class CommandHelpers
    {
        public string Uwuify(string content)
        {
            string message = content;
            string[] uwuArray = { "uwu", "owo", "OwO", "UwU", "(´・ω・`)" };
            Random rand = new Random();

            var num = rand.Next(0, uwuArray.Length);

            var comparer = StringComparer.OrdinalIgnoreCase;
            IDictionary<string, string> map = new Dictionary<string, string>(comparer)
            {
                {"the","da"},
                {"th","d"},
                {"with","wif"},
                {"yes","yesh"},
                {"your","yuw"},
                {"you","yuw"},
                {"r","w"},
                {"l", "w"},
                {"these", "dese"}
            };

            var regex = new Regex("(?i)" + String.Join("|", map.Keys));
            var finalMessage = regex.Replace(message, m => map[m.Value]);

            return $"{finalMessage} {uwuArray[num]}";
        }
    }
}
