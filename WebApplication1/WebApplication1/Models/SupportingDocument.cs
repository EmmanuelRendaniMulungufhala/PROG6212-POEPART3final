using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class SupportingDocument
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClaimId { get; set; }

        [ForeignKey("ClaimId")]
        public virtual ClaimModel? Claim { get; set; }

        [Required]
        [Display(Name = "Original File Name")]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Size")]
        public long FileSize { get; set; }

        [Required]
        [Display(Name = "Content Type")]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Display(Name = "Uploaded By")]
        [StringLength(100)]
        public string UploadedBy { get; set; } = string.Empty;

        // Computed properties
        [Display(Name = "File Size Formatted")]
        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        [Display(Name = "File Icon")]
        public string FileIcon => ContentType switch
        {
            string ct when ct.Contains("pdf") => "fas fa-file-pdf text-danger",
            string ct when ct.Contains("word") => "fas fa-file-word text-primary",
            string ct when ct.Contains("excel") || ct.Contains("spreadsheet") => "fas fa-file-excel text-success",
            string ct when ct.Contains("image") => "fas fa-file-image text-info",
            _ => "fas fa-file text-secondary"
        };

        public bool IsImage => ContentType.StartsWith("image/");
    }
}