﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;

namespace JawlaBot
{
    class Program
    {
        static DiscordClient discord;

        static CommandsNextModule commands;

        static InteractivityModule interactivity;

        static VoiceNextClient voice;

        public static Cooldown cooldown = new Cooldown();


        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] arg)
        {
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            var cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            });

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = cfgjson.CommandPrefix,
                EnableDms = false
            });

            commands.RegisterCommands<JawlaCommands>();

            //TODO: Add interactivity module and configuration

            interactivity = discord.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = TimeoutBehaviour.Delete,
                PaginationTimeout = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromMinutes(5)
            });

            voice = discord.UseVoiceNext();
            discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().Replace(" ", String.Empty).Equals("canigetahooyah")) 
                {
                    var rnd = new Random();
                    var nxt = rnd.Next(0, 10);
                    switch (nxt)
                    {
                        case 0:
                            await e.Message.RespondAsync("Not happening bud.");
                            return;
                        case 1:
                        case 2:
                            await e.Message.RespondAsync("HOOYAH");
                            return;
                        case 3:
                        case 4:
                        case 5:
                            await e.Message.RespondAsync("***HOOYAH***");
                            return;
                        case 6:
                        case 7:
                            await e.Message.RespondAsync("```HOOYAH```");
                            return;
                        case 8:
                        case 9:
                            await e.Message.RespondAsync(":regional_indicator_h: :regional_indicator_o:  :regional_indicator_o:  :regional_indicator_y:  :regional_indicator_a:  :regional_indicator_h:");
                            return;
                    }

                }
            };

            discord.MessageCreated += async e => //check the cooldown
            {
                if(e.Author.IsBot == false)
                {
                    var test = cooldown.CheckCoolDown(e);
                    await e.Message.RespondAsync($"update cooldown here {test}");
                     
                }
            };

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
    // this structure will hold data from config.json
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}
