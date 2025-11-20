using System.Xml.Linq;
using VoteShield.Models;

namespace VoteShield.Services
{
    public interface IAIVerificationService
    {
        Task<DocumentAnalysisResult> AnalyzeDocumentAsync(Stream fileStream, string fileName, string documentType);
        Task<bool> VerifyIDCardAsync(Stream fileStream);
        Task<DocumentAnalysisResult> AnalyzeEvidencePhotoAsync(Stream fileStream);
        Task<bool> CheckForTamperingAsync(Stream fileStream);
        Task<string> ExtractTextFromImageAsync(Stream fileStream);
        Task<Dictionary<string, object>> AnalyzeDocumentMetadataAsync(string filePath);
    }

    public class AIVerificationService : IAIVerificationService
    {
        private readonly ILogger<AIVerificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AIVerificationService(
            ILogger<AIVerificationService> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // Interface Implementation - Must be Public
        public async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(Stream fileStream, string fileName, string documentType)
        {
            var result = new DocumentAnalysisResult();

            try
            {
                // 1. Basic file validation
                var metadata = await AnalyzeDocumentMetadataAsync(fileName);
                result.Anomalies.AddRange(await CheckForBasicAnomalies(metadata));

                // 2. OCR Text Extraction
                result.ExtractedData["text"] = await ExtractTextFromImageAsync(fileStream);

                // 3. Document-specific analysis
                switch (documentType)
                {
                    case DocumentTypes.ID_Card:
                        result = await AnalyzeIDCardAsync(fileStream, result);
                        break;
                    case DocumentTypes.Evidence_Photo:
                        result = await AnalyzeEvidencePhotoAsync(fileStream);
                        break;
                    case DocumentTypes.Evidence_Video:
                        result = await AnalyzeEvidenceVideoAsync(fileStream);
                        break;
                }

                // 4. Tampering detection
                var isTampered = await CheckForTamperingAsync(fileStream);
                if (isTampered)
                {
                    result.Anomalies.Add("Possible image tampering detected");
                    result.ConfidenceScore *= 0.5; // Reduce confidence
                }

                // 5. Final verification decision
                result.IsVerified = result.ConfidenceScore >= 0.7 && result.Anomalies.Count == 0;
                result.AnalysisSummary = GenerateAnalysisSummary(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document: {FileName}", fileName);
                result.Anomalies.Add("Analysis failed: " + ex.Message);
                result.IsVerified = false;
                result.ConfidenceScore = 0;
            }

            return result;
        }

        public async Task<bool> VerifyIDCardAsync(Stream fileStream)
        {
            var result = await AnalyzeDocumentAsync(fileStream, "id_card", DocumentTypes.ID_Card);
            return result.IsVerified;
        }

        public async Task<DocumentAnalysisResult> AnalyzeEvidencePhotoAsync(Stream fileStream)
        {
            var result = new DocumentAnalysisResult();

            try
            {
                // Simulate AI analysis for evidence photos
                result.ConfidenceScore = 0.85; // Simulated confidence score
                result.ExtractedData["face_count"] = "2"; // Simulated face detection
                result.Anomalies.AddRange(await DetectBriberyPatternsAsync(fileStream));
                result.IsVerified = result.ConfidenceScore >= 0.7;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing evidence photo");
                result.Anomalies.Add("Evidence analysis failed");
            }

            return result;
        }

        public async Task<bool> CheckForTamperingAsync(Stream fileStream)
        {
            var random = new Random();
            return random.NextDouble() < 0.1; // 10% chance of detecting tampering
        }

        public async Task<string> ExtractTextFromImageAsync(Stream fileStream)
        {
            // Simulate OCR text extraction
            return "Simulated extracted text from document. नागरिकता प्रमाणपत्र Citizenship Certificate Republic of Nepal";
        }

        public async Task<Dictionary<string, object>> AnalyzeDocumentMetadataAsync(string filePath)
        {
            // Analyze file metadata for anomalies
            var metadata = new Dictionary<string, object>
            {
                { "file_size", 1024 * 1024 }, // 1MB
                { "created_date", DateTime.UtcNow.AddDays(-1) },
                { "modified_date", DateTime.UtcNow },
                { "file_type", "image/jpeg" },
                { "is_metadata_consistent", true }
            };

            return metadata;
        }

        // ---------------- Private Helpers ----------------
        private async Task<DocumentAnalysisResult> AnalyzeIDCardAsync(Stream fileStream, DocumentAnalysisResult baseResult)
        {
            var extractedText = baseResult.ExtractedData["text"];

            var patterns = new Dictionary<string, string>
            {
                { @"नागरिकता", "Citizenship card detected" },
                { @"Citizenship", "Citizenship card detected" },
                { @"\d{1,2}-\d{2}-\d{4}", "Possible date pattern" },
                { @"\d{4,10}", "Possible citizenship number" }
            };

            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(extractedText, pattern.Key))
                {
                    baseResult.ExtractedData[pattern.Value] = "Found";
                }
            }

            baseResult.ConfidenceScore = CalculateIDConfidence(extractedText);
            baseResult.IsVerified = baseResult.ConfidenceScore > 0.8;

            return baseResult;
        }

        private async Task<DocumentAnalysisResult> AnalyzeEvidenceVideoAsync(Stream fileStream)
        {
            var result = new DocumentAnalysisResult();
            result.ConfidenceScore = 0.75;
            result.ExtractedData["duration"] = "00:02:30";
            result.ExtractedData["frame_count"] = "4500";
            result.Anomalies.Add("Video analysis requires manual review");
            return result;
        }

        private async Task<List<string>> DetectBriberyPatternsAsync(Stream fileStream)
        {
            var anomalies = new List<string>();
            var random = new Random();
            if (random.NextDouble() > 0.8)
            {
                anomalies.Add("Possible cash exchange pattern detected");
            }
            return anomalies;
        }

        private double CalculateIDConfidence(string extractedText)
        {
            double confidence = 0.0;
            var keywords = new[] { "नागरिकता", "Citizenship", "Republic", "Nepal", "Date", "जन्म मिति" };

            foreach (var keyword in keywords)
            {
                if (extractedText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    confidence += 0.15;
            }

            return Math.Min(confidence, 1.0);
        }

        private async Task<List<string>> CheckForBasicAnomalies(Dictionary<string, object> metadata)
        {
            var anomalies = new List<string>();

            if ((long)metadata["file_size"] > 10 * 1024 * 1024)
                anomalies.Add("File size too large");

            var created = (DateTime)metadata["created_date"];
            var modified = (DateTime)metadata["modified_date"];
            if (modified < created)
                anomalies.Add("Suspicious file modification dates");

            return anomalies;
        }

        private string GenerateAnalysisSummary(DocumentAnalysisResult result)
        {
            var summary = $"Document Analysis Complete - Confidence: {result.ConfidenceScore:P0}\n";
            summary += $"Status: {(result.IsVerified ? "VERIFIED" : "REQUIRES REVIEW")}\n";

            if (result.Anomalies.Any())
                summary += $"Anomalies Detected: {string.Join(", ", result.Anomalies)}";
            else
                summary += "No anomalies detected";

            return summary;
        }
    }
}
