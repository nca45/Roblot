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
            htmlGetTitle = new HtmlWeb();
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
                // If the site is valid and selling a monitor
                if (await CheckSiteMonitorValid(match.Value))
                {
                    //Send a quote
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
                return false;
            }

            catch
            {
                return false;
            }
        }
    }
}
   
