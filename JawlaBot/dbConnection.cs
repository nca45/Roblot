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
    class dbConnection
    {
        static private IMongoDatabase database = JawlaBot.client.GetDatabase("jawlamoney");
        static private IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("users");

        private static void CheckConnection()
        {
            if (JawlaBot.client == null)
            {
                Console.WriteLine("we have not connected to the database yet - now connecting");
                JawlaBot.client = new MongoClient("mongodb://user:pass@ds235860.mlab.com:35860/jawlamoney");

            }
            else
            {
                Console.WriteLine("already connected to the database");
            }
        }

        private static async void dbInsert(string id, BsonDocument doc)
        {
            await collection.ReplaceOneAsync(
                filter: new BsonDocument("id", id),
                options: new UpdateOptions { IsUpsert = true },
                replacement: doc);
        }

        public static void UserExists(string id)
        {
            CheckConnection();

            var query = Builders<BsonDocument>.Filter.Eq("id", id);
            bool result = collection.Find(query).Limit(1).Any(); //this checks for existance
            if (!result)
            {
                Console.WriteLine("creating a document for this user");

                User user = new User();
                user.id = id;
                string output = JsonConvert.SerializeObject(user);
                BsonDocument entry = BsonDocument.Parse(output);
                dbInsert(id, entry);
            }
            else
            {
                Console.WriteLine("This user exists as a document");
            }
        }

        public static async void UserOwes(string payerId, string payeeId, double amount)
        {
            //grab the payer's document
            var query = Builders<BsonDocument>.Filter.Eq("id", payerId);
            var result = await collection.Find(query).Project<BsonDocument>("{_id: 0}").Limit(1).SingleAsync(); //exclude the '_id' that is given with each document
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
                IOwe payee = new IOwe();
                payee.id = payeeId;
                payee.amount = Math.Round(amount, 2);
                payer.IOwe.Add(payee);
            }
            //update the user document and insert
            string output = JsonConvert.SerializeObject(payer);
            BsonDocument entry = BsonDocument.Parse(output);
            dbInsert(payerId, entry);
        }

        public static async void UserIsOwed(string userOwedId, string userPayingId, double amount)
        {
            var query = Builders<BsonDocument>.Filter.Eq("id", userOwedId);
            var result = await collection.Find(query).Project<BsonDocument>("{_id: 0}").Limit(1).SingleAsync(); //exclude the '_id' that is given with each document
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
                OwesMe payer = new OwesMe();
                payer.id = userPayingId;
                payer.amount = Math.Round(amount, 2);
                userBeingPaid.owesMe.Add(payer);
            }

            string output = JsonConvert.SerializeObject(userBeingPaid);
            BsonDocument entry = BsonDocument.Parse(output);
            dbInsert(userOwedId, entry);
        }

        public static List<IOwe> WhoIowe(string userId)
        {
            UserExists(userId);
            var query = Builders<BsonDocument>.Filter.Eq("id", userId);
            var result = collection.Find(query).Project<BsonDocument>("{_id: 0, IOwe: 1}").Limit(1).Single(); //exclude the '_id' that is given with each document
            User usersIOwe = JsonConvert.DeserializeObject<User>(result.ToString());
            return usersIOwe.IOwe;
        }

        public static List<OwesMe> WhoOwesMe(string userId)
        {
            UserExists(userId);
            var query = Builders<BsonDocument>.Filter.Eq("id", userId);
            var result = collection.Find(query).Project<BsonDocument>("{_id: 0, owesMe: 1}").Limit(1).Single(); //exclude the '_id' that is given with each document
            User usersThatOwe = JsonConvert.DeserializeObject<User>(result.ToString());
            return usersThatOwe.owesMe;
        }
    }
}
