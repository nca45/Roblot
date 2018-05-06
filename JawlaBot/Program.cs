using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;

namespace JawlaBot
{
    class Program
    {
        static DiscordClient discord;

        static CommandsNextModule commands;

        static InteractivityModule interactivity;


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

            discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().EndsWith("xd")) 
                {
                    await e.Message.RespondAsync("😠 There will be no 'xd's on my channel.");
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
