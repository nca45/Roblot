using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoDB;
using MongoDB.Bson;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roblot.JSON_Classes;
using System.Threading.Tasks;

namespace Roblot
{
    public sealed class dbConnectionService
    {
        private String Username { get; }
        private String Password { get; }
        private String DatabaseName { get; }

        private MongoClient Client { get; set; } = null;
        private IMongoDatabase Database { get; set; } //= Roblot.client.GetDatabase("jawlamoney");
        private IMongoCollection<BsonDocument> Collection { get; set; } //= database.GetCollection<BsonDocument>("users");

        public dbConnectionService(DiscordClient client)
        {
            using (StreamReader file = File.OpenText("mongoDBKey.json"))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject loginInfo = (JObject)JToken.ReadFrom(reader);
                Username = (string)loginInfo["User"];
                Password = (string)loginInfo["Pass"];
                DatabaseName = (string)loginInfo["Database"];
            }

            this.Client = new MongoClient($"mongodb://{Username}:{Password}@ds235860.mlab.com:35860/{DatabaseName}?retryWrites=false");
            Database = Client.GetDatabase($"{DatabaseName}");
            Collection = Database.GetCollection<BsonDocument>("users");
            Console.WriteLine("Database Ready!");
        }

        private void CheckConnection()
        {
            if (Client == null)
            {
                Console.WriteLine("we have not connected to the database yet - now connecting");
                Client = new MongoClient("mongodb://nca45:moneyiscool1@ds235860.mlab.com:35860/jawlamoney");

            }
            else
            {
                Console.WriteLine("already connected to the database");
            }
        }

        private async Task dbInsert(string id, User serializable)
        {

            string output = JsonConvert.SerializeObject(serializable);
            BsonDocument entry = BsonDocument.Parse(output);
            try
            {
                await Collection.ReplaceOneAsync(
                    filter: new BsonDocument("id", id),
                    options: new ReplaceOptions { IsUpsert = true },
                    replacement: entry);
            }
            catch(MongoCommandException ex)
            {
                Console.WriteLine("Thrown exception error message: {0}", ex.ErrorMessage);
                Console.WriteLine("Thrown exception message: {0}", ex.Message);
            }
        }

        public async Task UserExists(string id)
        {
            CheckConnection();

            var query = Builders<BsonDocument>.Filter.Eq("id", id);
            bool result = Collection.Find(query).Limit(1).Any(); //this checks for existance
            if (!result)
            {
                Console.WriteLine("creating a document for this user");

                User user = new User
                {
                    id = id
                };

                await dbInsert(id, user);
            }
            else
            {
                Console.WriteLine("This user exists as a document");
            }
        }

        // Gets the user from the database and deserializes it into a User class
        private async Task<User> grabUser(string memberId)
        {
            await UserExists(memberId);
            var query = Builders<BsonDocument>.Filter.Eq("id", memberId);
            var result = await Collection.Find(query).Project<BsonDocument>("{_id: 0}").Limit(1).SingleAsync(); //exclude the '_id' that is given with each document
            return JsonConvert.DeserializeObject<User>(result.ToString());
        }

        // TODO: What if they try to save a playlist with the same name?
        // Deprecated (we don't need personalized playlists right now) 

        //public async Task savePlaylist(string memberId, string playlistUrl, string playlistName)
        //{
        //    Console.WriteLine("Getting user from database...");
        //    User member = await grabUser(memberId);
        //    Console.WriteLine("Got user from database");
        //    Playlists playlist = new Playlists
        //    {
        //        PlaylistName = playlistName,
        //        PlaylistUrl = playlistUrl
        //    };
        //    member.playlists.Add(playlist);
        //    Console.WriteLine("Inserting user into database...");
        //    await dbInsert(memberId, member);
        //    Console.WriteLine("Done");
        //}

        //public async Task<List<String>> loadPlaylist()
        //{

        //}

        public async Task UserOwes(string payerId, string payeeId, double amount)
        {
            //grab the payer's document
            var query = Builders<BsonDocument>.Filter.Eq("id", payerId);
            var result = await Collection.Find(query).Project<BsonDocument>("{_id: 0}").Limit(1).SingleAsync(); //exclude the '_id' that is given with each document
            User payer = JsonConvert.DeserializeObject<User>(result.ToString());
            bool payeeExists = false;

            for (int i = 0; i < payer.IOwe.Count; i++) //iterate over the list of people to check if we need to add more money
            {
                if (payer.IOwe[i].id == payeeId)
                {
                    payer.IOwe[i].amount += Math.Round(amount, 2);
                    payeeExists = true;
                }
            }
            //if payee doesn't exist then create a document for them
            if (!payeeExists)
            {
                IOwe payee = new IOwe
                {
                    id = payeeId,
                    amount = Math.Round(amount, 2)
                };
                payer.IOwe.Add(payee);
            }
            //update the user document and insert

            await dbInsert(payerId, payer);
        }

        public async Task UserIsOwed(string userOwedId, string userPayingId, double amount)
        {
            var query = Builders<BsonDocument>.Filter.Eq("id", userOwedId);
            var result = await Collection.Find(query).Project<BsonDocument>("{_id: 0}").Limit(1).SingleAsync(); //exclude the '_id' that is given with each document
            User userBeingPaid = JsonConvert.DeserializeObject<User>(result.ToString());
            bool payerExists = false;

            for (int i = 0; i < userBeingPaid.owesMe.Count; i++)
            {
                if (userBeingPaid.owesMe[i].id == userPayingId)
                {
                    userBeingPaid.owesMe[i].amount += Math.Round(amount, 2);
                    payerExists = true;
                }
            }
            if (!payerExists)
            {
                OwesMe payer = new OwesMe
                {
                    id = userPayingId,
                    amount = Math.Round(amount, 2)
                };
                userBeingPaid.owesMe.Add(payer);
            }

            await dbInsert(userOwedId, userBeingPaid);
        }

        public async Task<List<IOwe>> WhoIowe(string userId)
        {
            await UserExists(userId);
            var query = Builders<BsonDocument>.Filter.Eq("id", userId);
            var result = Collection.Find(query).Project<BsonDocument>("{_id: 0, IOwe: 1}").Limit(1).Single(); //exclude the '_id' that is given with each document
            User usersIOwe = JsonConvert.DeserializeObject<User>(result.ToString());
            return usersIOwe.IOwe;
        }

        public async Task<List<OwesMe>> WhoOwesMe(string userId)
        {
            await UserExists(userId);
            var query = Builders<BsonDocument>.Filter.Eq("id", userId);
            var result = Collection.Find(query).Project<BsonDocument>("{_id: 0, owesMe: 1}").Limit(1).Single(); //exclude the '_id' that is given with each document
            User usersThatOwe = JsonConvert.DeserializeObject<User>(result.ToString());
            return usersThatOwe.owesMe;
        }
    }
}
