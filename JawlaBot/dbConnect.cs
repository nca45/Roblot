using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using MongoDB;
using MongoDB.Bson;
using Newtonsoft.Json;
using JawlaBot.JSON_Classes;

namespace JawlaBot
{
    class dbConnect
    {
        public static async void dbInsert(string id, BsonDocument doc)
        {
            //check if we are already connected or not
            if (Program.client == null)
            {
                Console.WriteLine("we have not connected to the database yet - now connecting");
                Program.client = new MongoClient("mongodb://username:pass@ds235860.mlab.com:35860/jawlamoney");

            }
            else
            {
                Console.WriteLine("already connected to the database");
            }
            var database = Program.client.GetDatabase("jawlamoney");

            var collection = database.GetCollection<BsonDocument>("users");

            await collection.ReplaceOneAsync(
                filter: new BsonDocument("id", id),
                options: new UpdateOptions { IsUpsert = true },
                replacement: doc);
        }
    }
}
