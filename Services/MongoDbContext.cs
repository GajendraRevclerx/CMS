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
            // Seed Master Data
            var mastersCollection = Masters;
            if (mastersCollection.CountDocuments(FilterDefinition<Master>.Empty) == 0)
            {
                var masterData = new Master
                {
                    States = new List<string> { "CHD", "PUN", "HAR", "DEL" },
                    Cities = new List<string> { "Chandigarh", "Mohali", "Panchkula", "New Delhi" },
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
