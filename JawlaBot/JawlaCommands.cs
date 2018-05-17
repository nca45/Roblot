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

namespace JawlaBot
{
    class JawlaCommands
    {

        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            var testBool = Program.cooldown.CheckCoolDown(ctx.Message);
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

        [Command("votePoints"), Description("Run a poll with reactions.")]
        public async Task Poll(CommandContext ctx, [Description("Who should be subjected to this poll?")] DiscordMember member)
        {
           
            var pollDuration = TimeSpan.FromSeconds(30);
            var interactivity = ctx.Client.GetInteractivityModule();

            // present the poll
            var embed = new DiscordEmbedBuilder
            {
                Title = $"Poll time! Vote to change {member.DisplayName}'s ranking"
            };
            var msg = await ctx.RespondAsync(embed: embed);

            // add the options as reactions
            await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
            // get reactions
            var poll_result = await interactivity.CollectReactionsAsync(msg, pollDuration);
            await ctx.RespondAsync("The results are in!");

            //TODO: Grab the reaction numbers from thumbs up and thumbs down and see which ones is higher

            //var results = poll_result.Reactions.Where(xkvp => options.Contains(xkvp.Key))
            //    .Select(xkvp => $"{xkvp.Key}: {xkvp.Value}");

            // post results
            //await ctx.RespondAsync(string.Join("\n", results));
            await ctx.RespondAsync("done with this command");
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
