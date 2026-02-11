namespace FileConverter.Models
{
    public class DetectedFileType
    {
        public FileCategory Category { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Icon { get; set; } = "📄";
        public string Description { get; set; } = string.Empty;
    }
}
