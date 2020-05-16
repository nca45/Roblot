using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Roblot
{
    public sealed class Lists
    {
        private string CurrentDirectory { get; }

        public Lists(string currentDirectory)
        {
            this.CurrentDirectory = currentDirectory;
        }
        public string Restaurants()
        {
            var list = File.ReadAllLines(CurrentDirectory + "/textfiles/Restaurants.txt");
            Random rnd = new Random();
            return list[rnd.Next(list.Length)];
        }

        public string[] ListRestaurants()
        {
            return File.ReadAllLines(CurrentDirectory + "/textfiles/Restaurants.txt");
        }
    }
}
