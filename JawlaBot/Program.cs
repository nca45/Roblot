using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
namespace Roblot
{
    class Program
    {
        /// <summary>
        /// Entry for async entry point
        /// </summary>
        static void Main(string[] args)
        {

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Async entry point for the bot
        /// </summary>
        /// <param name="args"> args from Main </param>
        /// <returns></returns>
        private static async Task MainAsync(string[] args)
        {
            // Load the config file
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            var cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);

            // Instantiate a new roblot class
            var roblotInstance = new Roblot(cfgjson);
            // Run Roblot
            await roblotInstance.StartAsync().ConfigureAwait(false);
            // Wait forever
            await Task.Delay(-1);
        }
    }
}
