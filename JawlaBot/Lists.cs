using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JawlaBot
{
    class Lists
    {

        public static string PUBGDrops(string map)
        {
            var mapFile = File.ReadAllLines(Program.currentDirectory + $@"\textfiles\{map}Drops.txt");
            Random rnd = new Random();          
            return mapFile[rnd.Next(mapFile.Length)];
        }

        public static string Restaurants()
        {
            var list = File.ReadAllLines(Program.currentDirectory + @"\textfiles\Restaurants.txt");
            Random rnd = new Random();
            return list[rnd.Next(list.Length)];
        }

        public static string[] ListRestaurants()
        {
            return File.ReadAllLines(Program.currentDirectory + @"\textfiles\Restaurants.txt");
        }

        //add restaurant? Depends on how often we try out new places to eat.
    }
}
