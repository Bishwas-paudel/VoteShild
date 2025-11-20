using System.ComponentModel.DataAnnotations;

namespace VoteShield.Models
{
    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Required]
        [StringLength(50)]
        public string District { get; set; }

        [StringLength(50)]
        public string Municipality { get; set; }

        [Display(Name = "Bribe Amount")]
        public decimal? BribeAmount { get; set; }

        public string Currency { get; set; } = "NPR";

        [Display(Name = "Involved Party")]
        public string InvolvedParty { get; set; }

        [Display(Name = "Report Type")]
        public ReportType Type { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public string EvidenceUrl { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Display(Name = "Anonymous Code")]
        public string AnonymousCode { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSynced { get; set; } = true;

        // For SMS reports
        public string PhoneNumberHash { get; set; }
        public string SMSContent { get; set; }
    }

    public enum ReportType
    {
        [Display(Name = "Cash Bribe")]
        CashBribe,
        [Display(Name = "Gift Bribe")]
        GiftBribe,
        Threat,
        [Display(Name = "Vote Buying")]
        VoteBuying,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        [Display(Name = "Under Review")]
        UnderReview,
        Resolved,
        Rejected
    }
}