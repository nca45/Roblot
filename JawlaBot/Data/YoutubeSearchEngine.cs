using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Linq;
using Roblot.Data;
using Newtonsoft.Json.Linq;

namespace Roblot.Data
{
    public sealed class YoutubeSearchEngine
    {
        private string API = "AIzaSyCyszPJJeF1ju1wm5SnwPj4KDioBhmKJ1U";
        private HttpClient http;
        public YoutubeSearchEngine()
        {
            http = new HttpClient
            {
                BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/")
            };
        }

        public async Task<IEnumerable<YoutubeSearchResults>> SearchAsync(string terms)
        {
            Console.WriteLine(terms);
            var searchUrl = new Uri($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=5&type=video&fields=items(id(videoId),snippet(title,channelTitle))&key={this.API}&q={WebUtility.UrlEncode(terms)}");
            //deserialize the results
            var searchJson = "{}";
            using (var searchReq = await this.http.GetAsync(searchUrl).ConfigureAwait(false))
            using (var searchRes = await searchReq.Content.ReadAsStreamAsync())
            using (var searchSr = new StreamReader(searchRes, false))
                searchJson = await searchSr.ReadToEndAsync();

            var searchJsonData = JObject.Parse(searchJson);
            var searchData = searchJsonData["items"].ToObject<IEnumerable<YoutubeApiResponseItem>>();

            //grab the duration for each video using the videoAPI
            var videoIds = string.Join(",", searchData.Select(element => element.Id.videoId));

            //use the api again to grab all the durations for the videos
            var durationUrl = new Uri($"https://www.googleapis.com/youtube/v3/videos?part=contentDetails&id={WebUtility.UrlEncode(videoIds)}&fields=items(contentDetails(duration))&key={this.API}");

            var durationJson = "{}";
            using (var durationReq = await this.http.GetAsync(durationUrl).ConfigureAwait(false))
            using (var durationRes = await durationReq.Content.ReadAsStreamAsync())
            using (var durationSr = new StreamReader(durationRes, false))
                durationJson = await durationSr.ReadToEndAsync();

            var durationJsonData = JObject.Parse(durationJson);
            //transform into array for indexing
            var durationData = durationJsonData["items"].ToObject<IEnumerable<YoutubeApiVideoItem>>().ToArray<YoutubeApiVideoItem>();


            //like a SQL query - select the title, creator and videoID from each search result
            //for each element in the newly parsed json array we return a YoutubeSearchResults instance
            return searchData.Select((x,i) => new YoutubeSearchResults(WebUtility.HtmlDecode(x.Snippet.Title), x.Snippet.Creator, x.Id.videoId, durationData[i].contentDetails.duration));

        }
    }
}
