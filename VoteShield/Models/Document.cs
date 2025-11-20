using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoteShield.Models
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(100)]
        public string OriginalFileName { get; set; }

        [Required]
        [StringLength(50)]
        public string ContentType { get; set; }

        public long FileSize { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [StringLength(50)]
        public string DocumentType { get; set; } // ID_Card, Evidence, Supporting_Doc

        [StringLength(20)]
        public string VerificationStatus { get; set; } = DocumentStatus.Pending;

        public double? AI_ConfidenceScore { get; set; }

        [StringLength(1000)]
        public string AI_AnalysisResult { get; set; }

        public string AI_DetectedAnomalies { get; set; }

        // OCR Extracted Data
        public string ExtractedText { get; set; }
        public string ExtractedName { get; set; }
        public string ExtractedAddress { get; set; }
        public string ExtractedDOB { get; set; }
        public string ExtractedCitizenshipNumber { get; set; }

        // Relationships
        public Guid? ReportId { get; set; }
        public Report Report { get; set; }

        public Guid? CandidateId { get; set; }
        public Candidate Candidate { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string UploadedBy { get; set; } // UserId or Anonymous
    }

    public static class DocumentTypes
    {
        public const string ID_Card = "ID_Card";
        public const string Evidence_Photo = "Evidence_Photo";
        public const string Evidence_Video = "Evidence_Video";
        public const string Supporting_Document = "Supporting_Document";
        public const string Candidate_Asset = "Candidate_Asset";
        public const string Legal_Document = "Legal_Document";
    }

    public static class DocumentStatus
    {
        public const string Pending = "Pending";
        public const string Verified = "Verified";
        public const string Rejected = "Rejected";
        public const string Suspicious = "Suspicious";
        public const string Under_Review = "Under_Review";
    }

    public class DocumentAnalysisResult
    {
        public bool IsVerified { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Anomalies { get; set; } = new List<string>();
        public string AnalysisSummary { get; set; }
        public Dictionary<string, string> ExtractedData { get; set; } = new Dictionary<string, string>();
    }
}