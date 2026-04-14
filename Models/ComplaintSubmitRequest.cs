using Microsoft.AspNetCore.Http;

namespace CMS.Models
{
    /// <summary>
    /// Combined payload for registering a citizen AND submitting a complaint in one step.
    /// When the citizen is already logged in, the registration fields (FullName, MobileNo, Password) are ignored.
    /// </summary>
    public class ComplaintSubmitRequest
    {
        // ── Citizen registration fields (required for anonymous users) ──
        public string? FullName    { get; set; }
        public string? MobileNo    { get; set; }
        public string? Password    { get; set; }
        public string? Email       { get; set; }

        // ── Complaint fields ──
        public string? ComplaintTitle { get; set; }
        public string? Description    { get; set; }
        public string? Department     { get; set; }
        public string? State          { get; set; }
        public string? City           { get; set; }
        public string? Street         { get; set; }
        public string? Locality       { get; set; }
        public string? PinCode        { get; set; }
        public string? Source         { get; set; }
        public string? Site           { get; set; }
        public string? IncidentDate   { get; set; }
        public IFormFile? EvidenceFile { get; set; }
    }
}
