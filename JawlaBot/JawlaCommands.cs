using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

namespace JawlaBot
{
    class JawlaCommands
    {
        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync("This test is successful!");
        }

        [Command("hooyah")]
        public async Task HooyahAsync(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();
            var text = "test";
            await ctx.RespondAsync("Respond with 'test' to recieve a reaction!");

            var msg = await interactivity.WaitForMessageAsync(xm => xm.Content.Contains(text), TimeSpan.FromSeconds(15));
            if(msg != null)
            {
                await ctx.RespondAsync("you typed 'test!");
            }
            else
            {
                await ctx.RespondAsync("Nobody typed 'test' :( I sad");
            }
        }
        [Command("waitfortyping"), Description("Waits for a typing indicator.")]
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

        [Command("poll"), Description("Run a poll with reactions.")]
        public async Task Poll(CommandContext ctx, [Description("How long should the poll last.")] TimeSpan duration, [Description("What options should people have.")] params DiscordEmoji[] options)
        {
            // first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();
            var poll_options = options.Select(xe => xe.ToString());

            // then let's present the poll
            var embed = new DiscordEmbedBuilder
            {
                Title = "Poll time!",
                Description = string.Join(" ", poll_options)
            };
            var msg = await ctx.RespondAsync(embed: embed);

            // add the options as reactions
            for (var i = 0; i < options.Length; i++)
                await msg.CreateReactionAsync(options[i]);

            // collect and filter responses
            var poll_result = await interactivity.CollectReactionsAsync(msg, duration);
            var results = poll_result.Reactions.Where(xkvp => options.Contains(xkvp.Key))
                .Select(xkvp => $"{xkvp.Key}: {xkvp.Value}");

            // and finally post the results
            await ctx.RespondAsync(string.Join("\n", results));
        }
        //Setup for longest yeah boi ever and richtard

        //Join will become command of both yeahboi and richtard
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                await ctx.RespondAsync("Already Connected");
            }
            var chn = ctx.Member.VoiceState?.Channel;
            if (chn == null)
            {
                await ctx.RespondAsync("You are not in a voice channel!");
            }

            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync($"Joined {ctx.Channel.Name}");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if(vnc == null)
            {
                await ctx.RespondAsync("Not connected to any voice channel");
            }
            vnc.Disconnect();
            await ctx.RespondAsync($"Left {ctx.Channel.Name}");
        }
    }
}
