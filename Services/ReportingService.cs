using CMS.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Services
{
    public interface IReportingService
    {
        Task<(string body, byte[] csvData, string fileName)> GenerateDailyReportAsync();
    }

    public class ReportingService : IReportingService
    {
        private readonly MongoDbContext _context;

        public ReportingService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<(string body, byte[] csvData, string fileName)> GenerateDailyReportAsync()
        {
            // 1. Fetch all complaints
            var allComplaints = await _context.Complaints.Find(_ => true).ToListAsync();

            // 2. Group by Department for summary
            var deptSummary = allComplaints
                .GroupBy(c => c.Department ?? "System Uploads")
                .Select(g => new {
                    Dept = g.Key,
                    Total = g.Count(),
                    Resolved = g.Count(c => c.Status == "Resolved"),
                    Pending = g.Count(c => c.Status != "Resolved")
                })
                .OrderBy(x => x.Dept)
                .ToList();

            var totalCount = allComplaints.Count;
            var resolvedCount = allComplaints.Count(c => c.Status == "Resolved");
            var pendingCount = totalCount - resolvedCount;

            // 3. Generate HTML table for email body
            var summaryTableHtml = @"<table border='1' cellpadding='5' style='border-collapse:collapse;width:100%;font-family:sans-serif;'>
                <thead><tr style='background:#2c3e50;color:white;'><th>Department</th><th>Total</th><th>Resolved</th><th>Pending</th></tr></thead>
                <tbody>";
            foreach (var s in deptSummary)
            {
                summaryTableHtml += $"<tr><td>{s.Dept}</td><td align='center'>{s.Total}</td><td align='center' style='color:green;'>{s.Resolved}</td><td align='center' style='color:red;'>{s.Pending}</td></tr>";
            }
            summaryTableHtml += "</tbody></table>";

            var body = $@"
<div style='font-family:sans-serif;max-width:800px;'>
    <h2 style='color:#2c3e50;'>Daily Complaint Status Report</h2>
    <p>Dear Administrator,</p>
    <p>Please find the daily summary of complaints registered in the system as of <strong>{DateTime.Now:dd MMM yyyy, hh:mm tt}</strong>.</p>
    
    <div style='background:#f9f9f9;padding:15px;border-radius:8px;margin-bottom:20px;'>
        <h3 style='margin-top:0;'>Departmental Summary</h3>
        {summaryTableHtml}
    </div>

    <div style='display:flex;gap:20px;'>
        <div style='flex:1;background:#eef7ee;padding:10px;border-radius:5px;'><strong>Total:</strong> {totalCount}</div>
        <div style='flex:1;background:#eef7ee;padding:10px;border-radius:5px;'><strong>Resolved:</strong> {resolvedCount}</div>
        <div style='flex:1;background:#fff5f5;padding:10px;border-radius:5px;'><strong>Pending:</strong> {pendingCount}</div>
    </div>

    <p style='margin-top:20px;color:#666;'><em>A detailed CSV report with complete citizen details (Mobile & Name) is attached to this email.</em></p>
    <hr/>
    <p style='font-size:12px;color:#888;'>This is an automated report from the CCMS Government Portal. Please do not reply to this email.</p>
</div>";

            // 4. Generate CSV
            var csv = new StringBuilder();
            csv.Append('\uFEFF'); // UTF-8 BOM

            csv.AppendLine("DEPARTMENT SUMMARY REPORT");
            csv.AppendLine($"Generated On,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine("");
            csv.AppendLine("Department,Total Complaints,Resolved,Pending");
            foreach (var s in deptSummary)
            {
                csv.AppendLine($"{Escape(s.Dept)},{s.Total},{s.Resolved},{s.Pending}");
            }
            csv.AppendLine("");
            csv.AppendLine("");

            csv.AppendLine("DETAILED COMPLAINT LOG");
            csv.AppendLine("Complaint No,Date,Citizen Name,Mobile No,Department,Issue,Status,Priority");

            var sortedDetails = allComplaints
                .OrderBy(c => c.Department)
                .ThenByDescending(c => c.CreatedDate)
                .ToList();

            foreach (var c in sortedDetails)
            {
                csv.AppendLine($"{Escape(c.ComplaintNo)},{c.CreatedDate:yyyy-MM-dd HH:mm},\"{Escape(c.FullName)}\",\"{Escape(c.UserId)}\",\"{Escape(c.Department)}\",\"{Escape(c.ComplaintTitle)}\",\"{Escape(c.Status)}\",\"{Escape(c.Priority)}\"");
            }

            var csvData = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"CCMS_Daily_Report_{DateTime.Now:yyyy_MM_dd}.csv";

            return (body, csvData, fileName);
        }

        private string Escape(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Replace("\"", "\"\"").Replace("\r", "").Replace("\n", " ");
        }
    }
}
