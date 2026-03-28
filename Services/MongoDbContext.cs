using CMS.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Generic;

namespace CMS.Services
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            _database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);

            ConfigureIndices();
            SeedData();
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Complaint> Complaints => _database.GetCollection<Complaint>("Complaints");
        public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("Counters");
        public IMongoCollection<Master> Masters => _database.GetCollection<Master>("Masters");

        private void ConfigureIndices()
        {
            var usersCollection = Users;
            var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.MobileNo);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);
            usersCollection.Indexes.CreateOne(indexModel);
        }

        private void SeedData()
        {
            var mastersCollection = Masters;
            if (mastersCollection.CountDocuments(FilterDefinition<Master>.Empty) == 0)
            {
                var masterData = new Master
                {
                    States = new List<string> { "CHD", "PUN", "HAR", "DEL" },
                    Cities = new List<string> { "Chandigarh", "Mohali", "Panchkula", "New Delhi" },
                    Departments = new List<DepartmentMaster>
                    {
                        new DepartmentMaster { Name = "Electrical", Code = "ELE" },
                        new DepartmentMaster { Name = "Water Supply", Code = "WAT" },
                        new DepartmentMaster { Name = "Sanitation", Code = "SAN" },
                        new DepartmentMaster { Name = "Roads", Code = "ROA" }
                    }
                };
                mastersCollection.InsertOne(masterData);
            }
        }
    }
}
