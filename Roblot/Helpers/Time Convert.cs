using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Roblot
{
    public sealed class Time_Convert
    {
        public static string ConvertYoutubeTime(string duration)
        {
            TimeSpan convertedTime = XmlConvert.ToTimeSpan(duration);
            int hours = convertedTime.Hours;
            int minutes = convertedTime.Minutes;
            int seconds = convertedTime.Seconds;

            return ((hours != 0) ? $"{hours.ToString("D2")}:" : String.Empty) + minutes.ToString("D2") + ":" + seconds.ToString("D2");
        }

        /// <summary>
        /// Compresses the display of the track length if there are no hours
        /// </summary>
        /// <param name="duration"></param>
        /// <returns>The compressed time</returns>
        public static string CompressLavalinkTime(TimeSpan duration)
        {
            string shortForm = "";
            if(duration.Hours > 0)
            {
                shortForm += String.Format("{0}:", duration.Hours.ToString("D2"));
            }
            shortForm += String.Format("{0}:{1}", duration.Minutes.ToString("D2"), duration.Seconds.ToString("D2"));
            return shortForm;
        }
    }
}
