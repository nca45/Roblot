using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using HtmlAgilityPack;

namespace Roblot.Services
{
    public sealed class WebPageService
    {
        private Regex linkParser { get; set; }
        private HtmlWeb htmlWeb { get; }
        private string[] monitorConditions = new string[] { "IPS", "TN", "VA", "Monitor", "Gaming Monitor" };
        private Lists listClass { get; }
        //private Task waitForMessageThread { get; set; }
        private DiscordClient dClient { get; set; }
        private Timer scrapeWebsiteTimer { get; set; }

        public WebPageService(DiscordClient client)
        {
            // Looks for messages that start with https:// or www.
            linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            htmlWeb = new HtmlWeb
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36",
                AutoDetectEncoding = true

            };
            this.dClient = client;
            dClient.MessageCreated += Message_Created;
            Console.WriteLine("WebPageService Ready!");

            // Have it work right away for testing
            Console.WriteLine("Starting web scraping timer...");
            scrapeWebsiteTimer = new Timer(CheckWebsitesCallback, client, 0, (int)TimeSpan.FromMinutes(30).TotalMilliseconds);
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

                    Console.WriteLine("now sending to task.run...");
                    var waitForMessageThread = Task.Run(() => WaitForMessage(arg));
                    if(Task.CompletedTask.IsCompletedSuccessfully)
                    {
                        Console.WriteLine("Task.Run is completed!");
                        return;
                    }
                }
            }
        }

        private async Task WaitForMessage(MessageCreateEventArgs arg)
        {
            await Task.CompletedTask;
            var interactivity = dClient.GetInteractivity();
            var result = await interactivity.WaitForMessageAsync(xm => xm.Content.ToLower().Contains("roblot"), TimeSpan.FromSeconds(30));
            if(result.Result != null)
            {
                await dClient.SendMessageAsync(arg.Channel, Lists.ChooseFromLines(TextFileCategory.retorts));
            }
            return;
        }
        private async Task<bool> CheckSiteMonitorValid(string url)
        {
            try
            {
                var document = await htmlWeb.LoadFromWebAsync(url);
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

        private async void CheckWebsitesCallback(object obj)
        {
            var client = obj as DiscordClient;
            try
            {
                // Get 5800x stock
                String canadaComputers5800x = @"https://www.canadacomputers.com/product_info.php?cPath=4_64&item_id=183431";
                String mikes5800x = @"https://www.mikescomputershop.com/product/10912598";
                String memEx5800x = @"https://www.memoryexpress.com/Products/MX00114452";
                String imgUrl5800x = @"https://c1.neweggimages.com/ProductImageCompressAll1280/19-113-665-V01.jpg";

                String canadaComputers5600x = @"https://www.canadacomputers.com/product_info.php?cPath=4_64&item_id=183432";
                String mikes5600x = @"https://www.mikescomputershop.com/product/10912599";
                String memEx5600x = @"https://www.memoryexpress.com/Products/MX00114455";
                String imgUrl5600x = @"https://c1.neweggimages.com/ProductImageCompressAll1280/19-113-666-V01.jpg";

                await GetProductStock(client, "5800x", canadaComputers5800x, mikes5800x, memEx5800x, imgUrl5800x);
                await GetProductStock(client, "5600x", canadaComputers5600x, mikes5600x, memEx5600x, imgUrl5600x);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ERROR: Something went wrong with the scraping timer ({ex.GetType()}: {ex.Message})");
            }
        }

        private async Task GetProductStock(DiscordClient client, String productName, String canadaComputerUrl, String mikesUrl, String memExUrl, String productImg)
        {
            Dictionary<String, String> canadaComputersStock = await getCCStock(htmlWeb, canadaComputerUrl);
            Dictionary<String, String> memoryExpressStock = await getMEStock(htmlWeb, memExUrl);
            Dictionary<String, String> mikesComputerStock = await getMikeStock(htmlWeb, mikesUrl);

            if(canadaComputersStock.Count > 0 || memoryExpressStock.Count > 0 || mikesComputerStock.Count > 0)
            {
                var builder = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = $"The {productName} is back in stock!",
                    ThumbnailUrl = productImg
                };

                String canadaComputersString = String.Empty;
                String memoryExpressString = String.Empty;
                String mikesComputerString = String.Empty;

                foreach(var location in canadaComputersStock)
                {
                    canadaComputersString += $"{location.Key}: {location.Value} in stock\n";
                }
                foreach (var location in memoryExpressStock)
                {
                    memoryExpressString += $"{location.Key}: {location.Value} in stock\n";
                }
                foreach (var location in mikesComputerStock)
                {
                    mikesComputerString += $"{location.Key}: {location.Value} in stock\n";
                }

                if(canadaComputersString != String.Empty)
                {
                    var link = Formatter.MaskedUrl("Link to Product Page", new Uri(canadaComputerUrl));
                    builder.AddField("Canada Computers", canadaComputersString + link, true);
                }
                if(memoryExpressString != String.Empty)
                {
                    var link = Formatter.MaskedUrl("Link to Product Page", new Uri(memExUrl));
                    builder.AddField("Memory Express", memoryExpressString + link);
                }
                if(mikesComputerString != String.Empty)
                {
                    var link = Formatter.MaskedUrl("Link to Product Page", new Uri(mikesUrl));
                    builder.AddField("Mikes Computer Shop", mikesComputerString + link);
                }
                var user = await client.GetUserAsync(184864838651084801);
                await client.SendMessageAsync(await client.GetChannelAsync(576602140861136926), $"{user.Mention}", embed: builder);
            }
            canadaComputersStock.Clear();
            memoryExpressStock.Clear();
            mikesComputerStock.Clear();
        }

        private async Task<Dictionary<String, String>> getCCStock(HtmlWeb htmlWeb, string html)
        {
            int[] storesOfInterest = new int[] { 0, 4 }; // We are only interested in the online store and the vancouver stores

            HtmlDocument htmlDoc = await htmlWeb.LoadFromWebAsync(html);
            HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='row col-border-bottom pt-1']");

            Dictionary<String, String> canadaComputersStock = new Dictionary<string, string>();
            foreach (int index in storesOfInterest)
            {
                HtmlNode region = nodes[index];

                HtmlNodeCollection storeNames = region.SelectNodes(".//div[@class='col-9']/p");
                HtmlNodeCollection storeStock = region.SelectNodes(".//span[@class='stocknumber']");
                for (int i = 0; i < storeStock.Count; i++)
                {
                    // Remove the HTML tags left over
                    string currentStore = storeNames[i].ParentNode.RemoveChild(storeNames[i], true).InnerText;
                    string currentStock = storeStock[i].ParentNode.RemoveChild(storeStock[i], true).InnerText;

                    if (currentStock != "-")
                    {
                        canadaComputersStock.Add(currentStore, currentStock);
                    }
                }
            }
            return canadaComputersStock;
        }

        private async Task<Dictionary<String, String>> getMEStock(HtmlWeb htmlWeb, string html)
        {
            Dictionary<String, String> memoryExpressStock = new Dictionary<string, string>();

            HtmlDocument htmlDoc = await htmlWeb.LoadFromWebAsync(html);
            String[] storesOfInterest = new string[] { "Online Store", "Burnaby", "Langley", "Vancouver" };

            foreach (var store in storesOfInterest)
            {
                HtmlNode currStore = htmlDoc.DocumentNode.SelectSingleNode($"//*[text()[contains(., '{store}')]]");
                string currStock = currStore.ParentNode.SelectSingleNode(".//span[contains(@class, 'c-capr-inventory-store__availability')]").InnerText.Trim();

                if (currStock != "Not Available" && currStock != "Out of Stock" && currStock != "Backorder")
                {
                    string currStoreName = currStore.InnerText.Trim();
                    memoryExpressStock.Add(currStoreName.Substring(0, currStoreName.Length - 1), currStock);
                }
            }
            return memoryExpressStock;
        }

        private async Task<Dictionary<String, String>> getMikeStock(HtmlWeb htmlWeb, string html)
        {
            Dictionary<String, String> mikesComputerStock = new Dictionary<string, string>();
            HtmlDocument htmlDoc = await htmlWeb.LoadFromWebAsync(html);
            HtmlNode storeStock = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='gp-60 store-stock']");
            HtmlNode warehouseStock = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='gp-40 wh-stock']");

            // There's stock!
            if (storeStock != null || warehouseStock != null)
            {
                // Check if there's stock in the store
                if (storeStock.SelectSingleNode(".//td[@class='nostock']") == null)
                {
                    List<HtmlNode> listOfStores = storeStock.SelectNodes(".//th").ToList();
                    List<HtmlNode> numberOfItems = storeStock.SelectNodes(".//td[@class='stock']").ToList();

                    for (int i = 0; i < listOfStores.Count; i++)
                    {
                        if (numberOfItems[i].InnerText != "0")
                        {
                            mikesComputerStock.Add(listOfStores[i].InnerText, numberOfItems[i].InnerText);
                        }
                    }

                }
                // Check if there's stock in the warehouse
                if (warehouseStock.SelectSingleNode(".//td[@class='nostock']") == null)
                {
                    List<HtmlNode> listOfWarehouses = warehouseStock.SelectNodes(".//th").ToList();
                    List<HtmlNode> numberOfItems = warehouseStock.SelectNodes(".//td[@class='stock']").ToList();
                    for (int i = 0; i < listOfWarehouses.Count; i++)
                    {
                        if (numberOfItems[i].InnerText != "0")
                        {
                            mikesComputerStock.Add($"{listOfWarehouses[i].InnerText} Warehouse", numberOfItems[i].InnerText);
                        }
                    }
                }
            }
            return mikesComputerStock;
        }
    }
}
   
