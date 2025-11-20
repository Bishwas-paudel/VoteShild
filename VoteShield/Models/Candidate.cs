using System.ComponentModel.DataAnnotations;

namespace VoteShield.Models
{
    public class Candidate
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Party { get; set; }

        [Required]
        [StringLength(50)]
        public string District { get; set; }

        [StringLength(50)]
        public string Municipality { get; set; }

        [Display(Name = "Declared Assets")]
        public decimal DeclaredAssets { get; set; }

        [Display(Name = "Criminal Records")]
        public string CriminalRecords { get; set; }

        public string PhotoUrl { get; set; }

        [Display(Name = "Election Event")]
        public Guid ElectionEventId { get; set; }
        public ElectionEvent ElectionEvent { get; set; }
    }

    public class ElectionEvent
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Display(Name = "Election Date")]
        public DateTime ElectionDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public List<Candidate> Candidates { get; set; } = new List<Candidate>();
    }
}