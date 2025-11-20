using Microsoft.EntityFrameworkCore;
using VoteShield.Data;
using VoteShield.Models;

namespace VoteShield.Services
{
    public interface IDocumentService
    {
        Task<Document> UploadDocumentAsync(IFormFile file, string documentType, Guid? reportId = null, Guid? candidateId = null);
        Task<bool> DeleteDocumentAsync(Guid documentId);
        Task<Document> GetDocumentAsync(Guid documentId);
        Task<List<Document>> GetDocumentsByReportAsync(Guid reportId);
        Task<List<Document>> GetPendingVerificationDocumentsAsync();
        Task<Document> UpdateVerificationStatusAsync(Guid documentId, string status, string aiAnalysis = null);
    }

    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIVerificationService _aiService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(ApplicationDbContext context,
                             IAIVerificationService aiService,
                             IWebHostEnvironment environment,
                             ILogger<DocumentService> logger)
        {
            _context = context;
            _aiService = aiService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<Document> UploadDocumentAsync(IFormFile file, string documentType, Guid? reportId = null, Guid? candidateId = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Validate file type
            if (!IsAllowedFileType(file.FileName))
                throw new InvalidOperationException("File type not allowed");

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("File size exceeds 10MB limit");

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", documentType);
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new Document
            {
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FilePath = filePath,
                DocumentType = documentType,
                ReportId = reportId,
                CandidateId = candidateId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Start AI verification in background
            _ = Task.Run(async () => await ProcessAIVerificationAsync(document));

            return document;
        }

        private async Task ProcessAIVerificationAsync(Document document)
        {
            try
            {
                using var fileStream = new FileStream(document.FilePath, FileMode.Open);
                var analysisResult = await _aiService.AnalyzeDocumentAsync(
                    fileStream, document.OriginalFileName, document.DocumentType);

                // Update document with AI analysis results
                document.AI_ConfidenceScore = analysisResult.ConfidenceScore;
                document.AI_AnalysisResult = analysisResult.AnalysisSummary;
                document.AI_DetectedAnomalies = string.Join("; ", analysisResult.Anomalies);
                document.VerificationStatus = analysisResult.IsVerified ? DocumentStatus.Verified : DocumentStatus.Under_Review;
                document.ExtractedText = analysisResult.ExtractedData.ContainsKey("text") ?
                    analysisResult.ExtractedData["text"] : null;

                _context.Documents.Update(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("AI verification completed for document: {DocumentId}", document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI verification failed for document: {DocumentId}", document.Id);
                document.VerificationStatus = DocumentStatus.Under_Review;
                document.AI_AnalysisResult = "Analysis failed: " + ex.Message;
                _context.Documents.Update(document);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteDocumentAsync(Guid documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null) return false;

            // Delete physical file
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Document> GetDocumentAsync(Guid documentId)
        {
            return await _context.Documents
                .Include(d => d.Report)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<List<Document>> GetDocumentsByReportAsync(Guid reportId)
        {
            return await _context.Documents
                .Where(d => d.ReportId == reportId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<Document>> GetPendingVerificationDocumentsAsync()
        {
            return await _context.Documents
                .Where(d => d.VerificationStatus == DocumentStatus.Pending ||
                           d.VerificationStatus == DocumentStatus.Under_Review)
                .OrderBy(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<Document> UpdateVerificationStatusAsync(Guid documentId, string status, string aiAnalysis = null)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null) return null;

            document.VerificationStatus = status;
            if (!string.IsNullOrEmpty(aiAnalysis))
            {
                document.AI_AnalysisResult = aiAnalysis;
            }

            _context.Documents.Update(document);
            await _context.SaveChangesAsync();

            return document;
        }

        private bool IsAllowedFileType(string fileName)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".mp4", ".avi", ".mov" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
}