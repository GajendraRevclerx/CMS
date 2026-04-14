using CMS.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;

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
            MigrateDepartments();
            SeedData();
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Department> Departments => _database.GetCollection<Department>("Departments");
        public IMongoCollection<Complaint> Complaints => _database.GetCollection<Complaint>("Complaints");
        public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("Counters");
        public IMongoCollection<Master> Masters => _database.GetCollection<Master>("Masters");
        public IMongoCollection<StateMaster> States => _database.GetCollection<StateMaster>("States");
        public IMongoCollection<CityMaster> Cities => _database.GetCollection<CityMaster>("Cities");

        private void ConfigureIndices()
        {
            var usersCollection = Users;
            // DROP EXISTING UNIQUE INDEX IF IT EXISTS
            try { usersCollection.Indexes.DropOne("MobileNo_1"); } catch { }

            var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.MobileNo);
            var indexModel = new CreateIndexModel<User>(indexKeysDefinition);
            usersCollection.Indexes.CreateOne(indexModel);

            // States / Cities indices
            States.Indexes.CreateOne(new CreateIndexModel<StateMaster>(Builders<StateMaster>.IndexKeys.Ascending(s => s.ShortCode), new CreateIndexOptions { Unique = true }));
            Cities.Indexes.CreateOne(new CreateIndexModel<CityMaster>(Builders<CityMaster>.IndexKeys.Ascending(c => c.StateCode)));
        }

        private void SeedData()
        {
            // Seed Master Data
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
                        new DepartmentMaster { Name = "Construction-1", Code = "CON1" },
                        new DepartmentMaster { Name = "Construction-2", Code = "CON2" },
                        new DepartmentMaster { Name = "Public Health", Code = "PHD" },
                        new DepartmentMaster { Name = "Electrical", Code = "ELE" }
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
            if (usersCollection.CountDocuments(u => u.MobileNo == "admin") == 0)
            {
                usersCollection.InsertOne(new User
                {
                    MobileNo = "admin",
                    Password = "admin",
                    FullName = "Super Administrator",
                    Role = "Admin"
                });
            }
        }

        private void MigrateDepartments()
        {
            var collection = _database.GetCollection<BsonDocument>("Users");
            // Find documents where Department is an array
            var filter = Builders<BsonDocument>.Filter.Type("Department", BsonType.Array);
            
            using var cursor = collection.Find(filter).ToCursor();
            foreach (var doc in cursor.ToEnumerable())
            {
                var deptArray = doc["Department"].AsBsonArray;
                string firstDept = deptArray.Count > 0 ? deptArray[0].AsString : "";
                
                var update = Builders<BsonDocument>.Update.Set("Department", firstDept);
                collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]), update);
            }

            // Seed States
            if (States.CountDocuments(_ => true) == 0)
            {
                var states = new List<StateMaster>
                {
                    new StateMaster { ShortCode = "PB", Name = "Punjab" },
                    new StateMaster { ShortCode = "HR", Name = "Haryana" },
                    new StateMaster { ShortCode = "UP", Name = "Uttar Pradesh" },
                    new StateMaster { ShortCode = "DL", Name = "Delhi" },
                    new StateMaster { ShortCode = "RJ", Name = "Rajasthan" },
                    new StateMaster { ShortCode = "MH", Name = "Maharashtra" },
                    new StateMaster { ShortCode = "KA", Name = "Karnataka" },
                    new StateMaster { ShortCode = "TN", Name = "Tamil Nadu" }
                };
                States.InsertMany(states);
            }

            // Seed Cities
            if (Cities.CountDocuments(_ => true) == 0)
            {
                var cities = new List<CityMaster>
                {
                    // Punjab
                    new CityMaster { Name = "Chandigarh", StateCode = "PB" },
                    new CityMaster { Name = "Amritsar", StateCode = "PB" },
                    new CityMaster { Name = "Ludhiana", StateCode = "PB" },
                    new CityMaster { Name = "Patiala", StateCode = "PB" },
                    new CityMaster { Name = "Mohali", StateCode = "PB" },
                    // Haryana
                    new CityMaster { Name = "Panchkula", StateCode = "HR" },
                    new CityMaster { Name = "Gurugram", StateCode = "HR" },
                    new CityMaster { Name = "Faridabad", StateCode = "HR" },
                    new CityMaster { Name = "Rohtak", StateCode = "HR" },
                    // Uttar Pradesh
                    new CityMaster { Name = "Lucknow", StateCode = "UP" },
                    new CityMaster { Name = "Noida", StateCode = "UP" },
                    new CityMaster { Name = "Ghaziabad", StateCode = "UP" },
                    new CityMaster { Name = "Kanpur", StateCode = "UP" },
                    new CityMaster { Name = "Varanasi", StateCode = "UP" },
                    // Delhi
                    new CityMaster { Name = "New Delhi", StateCode = "DL" },
                    new CityMaster { Name = "North Delhi", StateCode = "DL" },
                    new CityMaster { Name = "South Delhi", StateCode = "DL" }
                };
                Cities.InsertMany(cities);
            }

            // Seed Departments
            if (Departments.CountDocuments(_ => true) == 0)
            {
                var depts = new List<Department>
                {
                    new Department { Name = "Construction-1", Code = "CON1", HeadName = "Sh. Sanjay Arrora", Icon = "🛣️" },
                    new Department { Name = "Construction-2", Code = "CON2", HeadName = "Sh. Sanjay Arrora", Icon = "🏢" },
                    new Department { Name = "Public Health", Code = "PHD", HeadName = "Sh. Sanjay Arrora", Icon = "🚰" },
                    new Department { Name = "Electrical", Code = "ELE", HeadName = "Amit Kumar", Icon = "💡" }
                };
                Departments.InsertMany(depts);
            }

            // Seed Admin and Officers
            var users = new List<User>
            {
                // Admin / Main Officer
                new User { FullName = "Sh. Sanjay Arrora", MobileNo = "7508185407", Password = "password123", Role = "Admin", Designation = "Superintending Engineer", Department = "Construction-2", Email = "seconst2@yahoo.com", Landline = "01722740019" },
                    
                // Officers from provided list
                new User { FullName = "Er. Surrinder Singh Grewal", MobileNo = "7508185591", Password = "password123", Role = "Officer", Designation = "XEN", Department = "Construction-2", Email = "cp2division@gmail.com", Landline = "01722740344", AreaOfJurisdiction = "XEN C.P. Division No. 2 (R)" },
                new User { FullName = "Er. Sumit Dixit", MobileNo = "6280161644", Password = "password123", Role = "Officer", Designation = "SDE", Department = "Construction-2", Landline = "7508185593", AreaOfJurisdiction = "R-3 Sub Division" },
                new User { FullName = "Er. Abhishek Pokhariyal", MobileNo = "7888391436", Password = "password123", Role = "Officer", Designation = "JE", Department = "Construction-2", AreaOfJurisdiction = "Jan Marg, Sector-17, ISBT-17" },
                new User { FullName = "Er. Ramil Bhritia", MobileNo = "7888391437", Password = "password123", Role = "Officer", Designation = "JE", Department = "Construction-2", AreaOfJurisdiction = "Jan Marg, Dakshin Marg, Sector-35-37" }
            };

            foreach (var user in users)
            {
                var filter = Builders<User>.Filter.Eq(u => u.MobileNo, user.MobileNo);
                var existing = Users.Find(filter).FirstOrDefault();
                if (existing == null)
                {
                    Users.InsertOne(user);
                }
                else if (user.Role == "Officer" && existing.Role == "Citizen")
                {
                    // Upgrade citizen to officer if seeded
                    var update = Builders<User>.Update
                        .Set(u => u.Role, user.Role)
                        .Set(u => u.Designation, user.Designation)
                        .Set(u => u.Department, user.Department)
                        .Set(u => u.AreaOfJurisdiction, user.AreaOfJurisdiction);
                    Users.UpdateOne(filter, update);
                }
            }
        }
    }
}
