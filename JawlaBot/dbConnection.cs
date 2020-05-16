using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using MongoDB;
using MongoDB.Bson;
using Newtonsoft.Json;
using Roblot.JSON_Classes;
using System.Threading.Tasks;

namespace Roblot
{
    public sealed class dbConnection
    {
        private MongoClient Client { get; set; } = null;
        private IMongoDatabase Database { get; set; } //= Roblot.client.GetDatabase("jawlamoney");
        private IMongoCollection<BsonDocument> Collection { get; set; } //= database.GetCollection<BsonDocument>("users");

        public dbConnection()
        {
            this.Client = new MongoClient("mongodb://nca45:moneyiscool1@ds235860.mlab.com:35860/jawlamoney");
            Database = Client.GetDatabase("jawlamoney");
            Collection = Database.GetCollection<BsonDocument>("users");

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

        private void dbInsert(string id, BsonDocument doc)
        {
            Collection.ReplaceOne(
                filter: new BsonDocument("id", id),
                options: new UpdateOptions { IsUpsert = true },
                replacement: doc);
        }

        public void UserExists(string id)
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
                string output = JsonConvert.SerializeObject(user);
                BsonDocument entry = BsonDocument.Parse(output);
                dbInsert(id, entry);
            }
            else
            {
                Console.WriteLine("This user exists as a document");
            }
        }

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
            string output = JsonConvert.SerializeObject(payer);
            BsonDocument entry = BsonDocument.Parse(output);
            dbInsert(payerId, entry);
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

            string output = JsonConvert.SerializeObject(userBeingPaid);
            BsonDocument entry = BsonDocument.Parse(output);
            dbInsert(userOwedId, entry);
        }

        public List<IOwe> WhoIowe(string userId)
        {
            UserExists(userId);
            var query = Builders<BsonDocument>.Filter.Eq("id", userId);
            var result = Collection.Find(query).Project<BsonDocument>("{_id: 0, IOwe: 1}").Limit(1).Single(); //exclude the '_id' that is given with each document
            User usersIOwe = JsonConvert.DeserializeObject<User>(result.ToString());
            return usersIOwe.IOwe;
        }

        public List<OwesMe> WhoOwesMe(string userId)
        {
            UserExists(userId);
            var query = Builders<BsonDocument>.Filter.Eq("id", userId);
            var result = Collection.Find(query).Project<BsonDocument>("{_id: 0, owesMe: 1}").Limit(1).Single(); //exclude the '_id' that is given with each document
            User usersThatOwe = JsonConvert.DeserializeObject<User>(result.ToString());
            return usersThatOwe.owesMe;
        }
    }
}
