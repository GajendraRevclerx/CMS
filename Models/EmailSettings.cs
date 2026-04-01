namespace CMS.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = "Chandigarh";
    }
}
