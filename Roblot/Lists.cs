using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Roblot
{
    public sealed class Lists
    {
        private static string CurrentDirectory { get; set; }

        public Lists(string currentDirectory)
        {
            Lists.CurrentDirectory = currentDirectory;
        }
        public static string ChooseFromLines(TextFileCategory category)
        {
            var list = File.ReadAllLines(CurrentDirectory + $"/textfiles/{category.ToString()}.txt");
            Random rnd = new Random();
            return list[rnd.Next(list.Length)];
        }

        public string[] ListRestaurants()
        {
            return File.ReadAllLines(CurrentDirectory + "/textfiles/Restaurants.txt");
        }

    }
}
