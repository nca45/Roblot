using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Roblot;
using Newtonsoft.Json;

namespace Roblot.Data
{
    public sealed class YoutubeSearchResults
    {
        public string Title { get; }
        public string Author { get; }
        public string Id { get; }
        public string Duration { get; }

        //private Time_Convert TimeConvert { get; }

        //create a new result with specified params
        public YoutubeSearchResults(string title, string author, string id, string duration)
        {
            this.Title = title;
            this.Author = author;
            this.Id = id;
            
            this.Duration = Time_Convert.ConvertYoutubeTime(duration);

        }

        //private string convertTime(string duration)
        //{
        //    TimeSpan convertedTime = XmlConvert.ToTimeSpan(duration);
        //    int hours = convertedTime.Hours;
        //    int minutes = convertedTime.Minutes;
        //    int seconds = convertedTime.Seconds;

        //    return ((hours != 0) ? $"{hours.ToString("D2")}:" : String.Empty) + minutes.ToString("D2") + ":" + seconds.ToString("D2");
        //}

    }
    internal struct YoutubeApiResponseItem //the JsonProperties follow the JSON structure that is returned by the api
    {
        [JsonProperty("id")]
        public ResponseId Id { get; private set; }

        [JsonProperty("snippet")]
        public ResponseSnippet Snippet { get; private set; }

        public struct ResponseId
        { 
            [JsonProperty("videoId")]
            public string videoId { get; private set; }
        }

        public struct ResponseSnippet
        {
            [JsonProperty("title")]
            public string Title { get; private set; }

            [JsonProperty("channelTitle")]
            public string Creator { get; private set; }
        }

    }

    internal struct YoutubeApiVideoItem //Json properties for getting the duration
    {
        [JsonProperty("contentDetails")]
        public responseDetails contentDetails { get; private set; }

        public struct responseDetails
        {
            [JsonProperty("duration")]
            public string duration { get; private set; }
        }
    }

}
