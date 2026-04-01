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
            var master = mastersCollection.Find(FilterDefinition<Master>.Empty).FirstOrDefault();
            
            if (master == null)
            {
                master = new Master
                {
                    States = new List<string> { "CHD", "PUN", "HAR", "DEL" },
                    Cities = new List<string> { "Chandigarh", "Mohali", "Panchkula", "New Delhi" },
                    Areas = new List<string> { "North Zone", "South Zone", "East Zone", "West Zone", "Central Mall" },
                    Sectors = new List<SectorMapping> 
                    {
                        new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 1" },
                        new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 2" },
                        new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 17" },
                        new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 22" },
                        new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 34" },
                        new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 35" }
                    },
                    Departments = new List<DepartmentMaster>
                    {
                        new DepartmentMaster { Name = "Electrical", Code = "ELE" },
                        new DepartmentMaster { Name = "Water Supply", Code = "WAT" },
                        new DepartmentMaster { Name = "Sanitation", Code = "SAN" },
                        new DepartmentMaster { Name = "Roads", Code = "ROA" }
                    }
                };
                mastersCollection.InsertOne(master);
            }
            else if (master.Sectors == null || master.Sectors.Count == 0)
            {
                // Force seed sectors if missing in existing master record
                var update = Builders<Master>.Update.Set(m => m.Sectors, new List<SectorMapping> 
                {
                    new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 1" },
                    new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 2" },
                    new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 17" },
                    new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 22" },
                    new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 34" },
                    new SectorMapping { State = "CHD", City = "Chandigarh", SectorName = "Sector 35" }
                });
                mastersCollection.UpdateOne(m => m.Id == master.Id, update);
            }

            // Seed Users (Admin & Dept Heads)
            var usersCollection = Users;
            
            // Check by MobileNo to ensure exact accounts exist
           
        }
    }
}
