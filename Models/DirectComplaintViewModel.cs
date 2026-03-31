using System.ComponentModel.DataAnnotations;
using System;
using Microsoft.AspNetCore.Http;

namespace CMS.Models
{
    public class DirectComplaintViewModel
    {
        // User Details
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Please enter a valid 10-digit mobile number.")]
        public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;


        // Complaint Details
        [Required(ErrorMessage = "Complaint Title is required.")]
        public string ComplaintTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required.")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MinLength(200, ErrorMessage = "Description must be at least 200 characters long.")]
        public string Description { get; set; } = string.Empty;

        public string? ComplaintNo { get; set; }
        
        // Location specifics
        public string? Street { get; set; }
        public string? Locality { get; set; }
        public string? PinCode { get; set; }
        public DateTime? IncidentDate { get; set; }
        public string? Site { get; set; }
        public string? Source { get; set; }

        // Attachment
        public IFormFile? Attachment { get; set; }

        public string? Area { get; set; }
        public string? AreaOfJurisdiction { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
    }
}
