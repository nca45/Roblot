using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.Lavalink;
using MongoDB.Driver.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Roblot.JSON_Classes;
using Roblot.Services;
using Roblot.Data;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.EventArgs;

namespace Roblot
{
    public sealed class Roblot
    {
        /// <summary>
        /// Gets the discord client instance
        /// </summary>
        public DiscordClient Discord { get; }

        /// <summary>
        /// Gets the commandsnext instance
        /// </summary>
        public CommandsNextExtension Commands { get; }

        /// <summary>
        /// Gets the interactivity instance
        /// </summary>
        public InteractivityExtension Interactivity { get; }

        /// <summary>
        /// Gets the voicenext instance
        /// </summary>
        public VoiceNextExtension Voice { get; }

        /// <summary>
        /// Gets the lavalink instance
        /// </summary>
        public LavalinkExtension Lavalink { get; }

        /// <summary>
        /// Gets the audiocategories instance for audio meme playback
        /// </summary>
        public Dictionary<string, Cooldown> AudioCategories { get; set; }

        /// <summary>
        /// Gets the MongoDB client instance for moneytracking and others (Maybe we can put this in its own class)
        /// </summary>
       // public MongoClient Client { get; set; } = null;

        /// <summary>
        /// Gets the location of the current directory
        /// </summary>
        public string CurrentDirectory { get; } = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        private IServiceProvider Services { get; }

        private MusicData music;

        /// <summary>
        /// Create new instance of Roblot bot
        /// </summary>
        /// <param name="cfg"> The configuration json file </param>
        public Roblot(ConfigJson cfg)
        {
            this.Discord = new DiscordClient(new DiscordConfiguration
            {
                Token = cfg.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });
            this.Discord.Ready += async x =>
            {
                AudioCategories = new Dictionary<string, Cooldown>()
                {
                    {"frank", new Cooldown()},
                    {"stop", new Cooldown()},
                    {"yeahboi", new Cooldown{cooldownTime = 60 }},
                    {"pranked", new Cooldown()},
                    {"wow", new Cooldown{cooldownTime = 1}}
                };


                DiscordActivity botstatus = new DiscordActivity("you sleep | !help", ActivityType.Watching);
                music = Services.GetService<MusicData>();
                await music.SetVolume(50);
                await this.Discord.UpdateStatusAsync(botstatus);
                return;
            };

            this.Discord.VoiceStateUpdated += VoiceState_Updated;

            this.Discord.GuildMemberAdded += GuildMember_Added;

            this.Services = new ServiceCollection()
                .AddSingleton(new Lists(CurrentDirectory))
                .AddSingleton(new LavalinkService(this.Discord, cfg.ipAddress, cfg.port))
                .AddSingleton(this.Discord)
                .AddSingleton<dbConnectionService>()
                .AddSingleton(new YoutubeSearchEngine())
                .AddSingleton<PasteBinService>()
                .AddSingleton<MusicData>()
                .AddSingleton(this)
                .BuildServiceProvider(true);

            this.Commands = Discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new List<string> { cfg.CommandPrefix },
                EnableDms = false,
                Services = this.Services
            });
            this.Commands.SetHelpFormatter<HelpFormatter>();
            

            this.Commands.RegisterCommands(Assembly.GetExecutingAssembly()); //registers all commands inheriting basecommandmodule - CLASS MUST BE PUBLIC!

            this.Interactivity = Discord.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Ignore,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.KeepEmojis,
                Timeout = TimeSpan.FromMinutes(5)
            });
            this.Lavalink = Discord.UseLavalink();

            this.Voice = Discord.UseVoiceNext();
        }

        private async Task GuildMember_Added(GuildMemberAddEventArgs e)
        {
            //Console.WriteLine($"{e.Member.Username} joined");
            DiscordRole citizenRole = e.Guild.GetRole(e.Guild.Roles.FirstOrDefault(r => r.Value.Name == "Citizens").Key);
            
            await e.Member.GrantRoleAsync(citizenRole);
        }

        private async Task VoiceState_Updated(VoiceStateUpdateEventArgs e)
        {
            // first check if the bot itself enters or leaves the channel
            if(e.User == Discord.CurrentUser)
            {
                return;
            }
            // then get the music data service
            music = Services.GetService<MusicData>();

            // get the voice channel the bot is in
            var vchannel = music.VoiceChannel;

            // Do nothing if bot is not connected or a user disconnects/connects from a voice channel different from Roblot
            if(vchannel == null || vchannel != e.Before.Channel)
            {
                return;
            }
            var users = vchannel.Users;
            // if it is playing and it is the only one left in the channel
            if(music.IsPlaying && (users.Count() == 1 && users.First().Id == this.Discord.CurrentUser.Id))
            {
                await music.Pause();
                if(music.TextChannel != null)
                {
                    await music.TextChannel.SendMessageAsync($"{DiscordEmoji.FromName(this.Discord, ":warning:")} All members have left the voice channel - Playback paused. Resume playback by joining the channel and using `!resume`");
                }
            }
            // pause the player
        }
        public Task StartAsync()
        {
            return this.Discord.ConnectAsync();
        }
    }
    // this structure will hold data from config.json
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }

        [JsonProperty("ip")]
        public string ipAddress { get; private set; }

        [JsonProperty("port")]
        public string port { get; private set; }
    }
}
