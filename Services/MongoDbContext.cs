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
        public IMongoCollection<Department> Departments => _database.GetCollection<Department>("Departments");
        public IMongoCollection<Complaint> Complaints => _database.GetCollection<Complaint>("Complaints");
        public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("Counters");
        public IMongoCollection<Master> Masters => _database.GetCollection<Master>("Masters");
        public IMongoCollection<StateMaster> States => _database.GetCollection<StateMaster>("States");
        public IMongoCollection<CityMaster> Cities => _database.GetCollection<CityMaster>("Cities");

        private void ConfigureIndices()
        {
            var usersCollection = Users;
            var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.MobileNo);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);
            usersCollection.Indexes.CreateOne(indexModel);

            // States / Cities indices
            States.Indexes.CreateOne(new CreateIndexModel<StateMaster>(Builders<StateMaster>.IndexKeys.Ascending(s => s.ShortCode), new CreateIndexOptions { Unique = true }));
            Cities.Indexes.CreateOne(new CreateIndexModel<CityMaster>(Builders<CityMaster>.IndexKeys.Ascending(c => c.StateCode)));
        }

        private void SeedData()
        {
            // Seed Master Data
            var mastersCollection = Masters;
            if (mastersCollection.CountDocuments(FilterDefinition<Master>.Empty) == 0)
            {
                var masterData = new Master
                {
                    States = new List<StateData>
                    {
                        new StateData { ShortCode = "PB", FullName = "Punjab", Cities = new List<string> { "Chandigarh", "Amritsar", "Ludhiana", "Jalandhar", "Patiala", "Mohali" } },
                        new StateData { ShortCode = "HR", FullName = "Haryana", Cities = new List<string> { "Panchkula", "Gurugram", "Faridabad", "Rohtak", "Ambala", "Karnal" } },
                        new StateData { ShortCode = "UP", FullName = "Uttar Pradesh", Cities = new List<string> { "Lucknow", "Kanpur", "Varanasi", "Agra", "Noida", "Ghaziabad", "Prayagraj" } },
                        new StateData { ShortCode = "DL", FullName = "Delhi", Cities = new List<string> { "New Delhi", "North Delhi", "South Delhi", "Central Delhi" } },
                        new StateData { ShortCode = "RJ", FullName = "Rajasthan", Cities = new List<string> { "Jaipur", "Jodhpur", "Udaipur", "Kota", "Ajmer", "Bikaner" } },
                        new StateData { ShortCode = "MH", FullName = "Maharashtra", Cities = new List<string> { "Mumbai", "Pune", "Nagpur", "Thane", "Nashik" } },
                        new StateData { ShortCode = "KA", FullName = "Karnataka", Cities = new List<string> { "Bengaluru", "Mysuru", "Hubballi", "Mangaluru" } },
                        new StateData { ShortCode = "TN", FullName = "Tamil Nadu", Cities = new List<string> { "Chennai", "Coimbatore", "Madurai", "Salem" } }
                    },
                    Departments = new List<DepartmentMaster>
                    {
                        new DepartmentMaster { Name = "Construction-1", Code = "CON1" },
                        new DepartmentMaster { Name = "Construction-2", Code = "CON2" },
                        new DepartmentMaster { Name = "Public Health", Code = "PHD" },
                        new DepartmentMaster { Name = "Electrical", Code = "ELE" }
                    }
                };
                mastersCollection.InsertOne(masterData);
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
