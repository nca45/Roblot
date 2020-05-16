using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Roblot.Events;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

namespace Roblot
{
    class AudioCommands:BaseCommandModule
    {
        
        String currAudioSample = null;

        #region Cooldown Settings
        [Command("listcooldowns")]
        [Aliases("listcooldown", "cooldowntimes")]
        [Description("Lists the current cooldowns for each command")]
        public async Task ListCooldown(CommandContext ctx)
        {
            string listofCooldowns = "";
            var keys = Roblot.audioCategories.Keys.ToArray();

            foreach (var cooldown in keys)
            {
                listofCooldowns += $"{cooldown}: " + Roblot.audioCategories[cooldown].cooldownTime + " seconds\n";
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
            Roblot.audioCategories.TryGetValue(command, out currCooldown);
            DiscordEmbed embed = null;
            var roles = ctx.Member.Roles;
            string finalmsg = "";
            bool isadmin = false;
            foreach (var currRole in roles)
            {
                isadmin = (currRole.Permissions.HasPermission(Permissions.Administrator)) ? true : false;
            }
            if (isadmin && currCooldown != null)
            {
                if (newtime < 0 || newtime > 600)
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
                var arrayOfKeys = Roblot.audioCategories.Keys.ToArray();
                var listofkeys = "";
                foreach (var keys in arrayOfKeys)
                {
                    listofkeys += keys + "\n";
                }
                embed = new DiscordEmbedBuilder
                {
                    Title = "Here's the current list of audio command ids",
                    Description = listofkeys
                };
            }
            await ctx.RespondAsync(finalmsg, embed: embed);

        }
        #endregion

        #region Private functions

        private string GetRandomFile(string directory) //grabs random file from the directory
        {
            Random rnd = new Random();
            var fileName = System.IO.Directory.GetFiles(Roblot.currentDirectory + $"/{directory}", "*ogg");
            return fileName[rnd.Next(0, fileName.Length)];
        }

        private async Task Join(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
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
            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);

            if (vnc == null)
            {
                await ctx.RespondAsync("Not connected in this guild.");
            }

            else
            {
                Roblot.audioCategories[currAudioSample].startCooldown(DateTime.Now); //start the cooldown now
                vnc.Disconnect();
            }
        }

        private async Task StreamAudio(CommandContext ctx, string audiofile, string command)
        {

            var remainingTime = Roblot.audioCategories[command].CheckCoolDown(ctx.Message);
            if (remainingTime == -1)
            {
                currAudioSample = command; //Set the current audio category playing

                await Join(ctx);
                var vnc = ctx.Client.GetVoiceNext().GetConnection(ctx.Guild);
                await vnc.SendSpeakingAsync(true);
                string file = audiofile;

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
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
            }
            else
            {
                await ctx.RespondAsync($"That command ({command}) is on cooldown, please wait {remainingTime} more seconds!");
            }
        }
        #endregion

        #region Stream Audio
        [Command("yeahboi")]
        [Description("Ask the bot to show you its longest yeah boy ever")]
        public async Task LongestYeahBoi(CommandContext ctx)
        {

            await StreamAudio(ctx, Roblot.currentDirectory + "\\yeahboi.mp3", "yeahboi");
        }

        [Command("timetostop")]
        [Aliases("frankstop", "itstimetostop", "notokay")]
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

        [Command("stop")]
        [Description("Stops the player and leaves the channel")]
        public async Task Stop(CommandContext ctx)
        {
            await Leave(ctx);
        }
        #endregion

        public void OncooldownStarted(object source, EventArgs args)
        {
            Console.WriteLine("Hello world");
        }
    }
}
