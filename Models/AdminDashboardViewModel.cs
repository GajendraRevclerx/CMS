using System.Collections.Generic;
using CMS.Models;

namespace CMS.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int EscalatedComplaints { get; set; }
        
        public double AvgResolutionTimeDays { get; set; }
        public double AvgCitizenRating { get; set; }
        public int OfficersOnDuty { get; set; }

        public List<Complaint> RecentComplaints { get; set; } = new List<Complaint>();
        public List<Complaint> AllComplaints { get; set; } = new List<Complaint>();
        
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<User> Officers { get; set; } = new List<User>();
    }
}
