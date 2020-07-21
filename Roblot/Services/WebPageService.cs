using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using DSharpPlus;
using DSharpPlus.EventArgs;
using HtmlAgilityPack;

namespace Roblot.Services
{
    public sealed class WebPageService
    {
        private Regex linkParser { get; set; }
        private HtmlWeb htmlGetTitle { get; }
        private string[] monitorConditions = new string[] { "IPS", "TN", "VA", "Monitor", "Gaming Monitor" };
        private Lists listClass { get; }

        public WebPageService(DiscordClient client)
        {
            // Looks for messages that start with https:// or www.
            linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            htmlGetTitle = new HtmlWeb
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36",
                AutoDetectEncoding = true

            };
            client.MessageCreated += Message_Created;
            Console.WriteLine("WebPageService Ready!");
        }



        private async Task Message_Created(MessageCreateEventArgs arg)
        {

            if (arg.Message.Author.IsBot)
            {
                return;
            }

            // Gets the number of webpages that we found in the message
            var matches = linkParser.Matches(arg.Message.Content);

            foreach (Match match in matches)
            {
                Console.WriteLine($"Found webpage: {match.Value}");
                // If the site is valid and selling a monitor
                if (await CheckSiteMonitorValid(match.Value))
                {
                    //Send a quote
                    Console.WriteLine("Monitor match is valid");
                    await arg.Channel.SendMessageAsync(Lists.ChooseFromLines(TextFileCategory.quotes));
                }
            }
        }

        private async Task<bool> CheckSiteMonitorValid(string url)
        {
            try
            {
                var document = await htmlGetTitle.LoadFromWebAsync(url);
                var title = document.DocumentNode.SelectSingleNode("//head/title");

                if (monitorConditions.Any(title.InnerText.Contains))
                {
                    return true;
                }
                Console.WriteLine("Monitor match not valid");
                return false;
            }

            catch(Exception e)
            {
                Console.WriteLine($"Caught an error {e.InnerException}");
                return false;
            }
        }
    }
}
   
