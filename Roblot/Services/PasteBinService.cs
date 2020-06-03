using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using DSharpPlus;
using DSharpPlus.EventArgs;
using PastebinAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roblot.Services
{
    public sealed class PasteBinService
    {
        private const int listPasteNum = 100;
        private String DevKey { get; }
        private String Username { get; }
        private String Pass { get; }
        private dbConnectionService database { get; }

        public User User { get; private set; }


        public PasteBinService(DiscordClient client, dbConnectionService dbConnection)
        {
            database = dbConnection;
            using (StreamReader file = File.OpenText("pasteBinAPIKey.json"))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject loginInfo = (JObject)JToken.ReadFrom(reader);
                DevKey = (string)loginInfo["Key"];
                Username = (string)loginInfo["User"];
                Pass = (string)loginInfo["Pass"];
            }

            client.Ready += Client_Ready;
        }

        public async Task<PlaylistResult> deletePlaylistAsync(string playlistName)
        {
            foreach(Paste paste in await User.ListPastesAsync(listPasteNum))
            {
                if(paste.Title.ToLower() == playlistName.ToLower())
                {
                    try
                    {
                        await User.DeletePasteAsync(paste);
                        return PlaylistResult.Successful;
                    }
                    catch (PastebinException ex)                  
                    {
                        Console.Error.WriteLine(ex.Parameter.ToString());
                        return PlaylistResult.Failed;
                    }
                }
            }
            return PlaylistResult.Failed;
  
        }

        public async Task<bool> playlistExists(string playlistName)
        {
            foreach (Paste paste in await User.ListPastesAsync(listPasteNum))
            {
                if (paste.Title.ToLower() == playlistName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        // Returns an enum stating the result of saving
        public async Task<PlaylistResult> saveTracksAsync(List<string> listOfTracks, string playlistName)
        {
            string playlist = String.Join('\n', listOfTracks);
            try
            {
                Paste playlistPaste = await User.CreatePasteAsync(playlist, playlistName, null, Visibility.Unlisted, Expiration.Never);

                // Check if the name already exists without checking the database (costly!)
                return PlaylistResult.Successful;
            }
            catch (PastebinException ex)
            {
                Console.Error.WriteLine(ex.Parameter.ToString());
                return PlaylistResult.Failed;
            }
        }

        public async Task<IEnumerable<String>> loadTracksAsync(string playlistName)
        {
            using (HttpClient client = new HttpClient())
            {
                foreach (Paste paste in await User.ListPastesAsync(listPasteNum))
                {
                    // For some reason it won't actually do 'toLower()' when i put this in the for loop so i put it out here and it works wut
                    var checkPaste = paste.Title.ToLower();
                    if (checkPaste == playlistName.ToLower())
                    {
                        Console.WriteLine("Getting raw data from pastebin ...");
                        try
                        {
                            String rawData = await client.GetStringAsync($"https://pastebin.com/raw/{paste.Key}");
                            Console.WriteLine("Got it!");
                            return rawData.Split('\n').ToList<String>();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
            return null;
        }

        public async Task<Dictionary<String, int>> listPlaylistsAsync()
        {
            Dictionary<String, int> listOfPlaylists = new Dictionary<string, int>();
            using (HttpClient client = new HttpClient())
            {
                foreach (Paste paste in await User.ListPastesAsync(listPasteNum))
                {
                    // get number of lines in the text
                    Console.WriteLine("Getting raw data from pastebin ...");
                    try
                    {
                        String rawData = await client.GetStringAsync($"https://pastebin.com/raw/{paste.Key}");
                        Console.WriteLine("Got it!");
                        listOfPlaylists.Add(paste.Title, rawData.Split('\n').Length);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

            }
            return listOfPlaylists;
        }

        private async Task Client_Ready(ReadyEventArgs e)
        {
            Pastebin.DevKey = this.DevKey;
            try
            {
                User = await Pastebin.LoginAsync(Username, Pass);
                Console.WriteLine("Pastebin service ready!");
            }
            catch (PastebinException ex)
            {
                if (ex.Parameter == PastebinException.ParameterType.Login)
                {
                    Console.Error.WriteLine("Invalid username/password");
                }
            }
        }
    }
}
