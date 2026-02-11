using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileConverter.Models;

namespace FileConverter.Services
{
    public class FileTypeDetector
    {
        private readonly Dictionary<FileCategory, CategoryInfo> categoryMap;

        public FileTypeDetector()
        {
            categoryMap = new Dictionary<FileCategory, CategoryInfo>
            {
                {
                    FileCategory.Document,
                    new CategoryInfo
                    {
                        Icon = "📄",
                        Extensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt", ".xls", ".xlsx", ".ppt", ".pptx", ".csv" }
                    }
                },
                {
                    FileCategory.Image,
                    new CategoryInfo
                    {
                        Icon = "🖼️",
                        Extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico", ".tiff", ".tif", ".heic" }
                    }
                },
                {
                    FileCategory.Video,
                    new CategoryInfo
                    {
                        Icon = "🎬",
                        Extensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" }
                    }
                },
                {
                    FileCategory.Audio,
                    new CategoryInfo
                    {
                        Icon = "🎵",
                        Extensions = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus", ".alac" }
                    }
                },
                {
                    FileCategory.Archive,
                    new CategoryInfo
                    {
                        Icon = "📦",
                        Extensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso" }
                    }
                }
            };
        }

        public DetectedFileType DetectFileType(FileInfo fileInfo)
        {
            if (fileInfo == null || !fileInfo.Exists)
            {
                return CreateUnknownFileType();
            }

            string extension = fileInfo.Extension.ToLowerInvariant();

            foreach (var category in categoryMap)
            {
                if (category.Value.Extensions.Contains(extension))
                {
                    return new DetectedFileType
                    {
                        Category = category.Key,
                        Format = extension.TrimStart('.'),
                        Icon = category.Value.Icon,
                        Description = $"{category.Key} file"
                    };
                }
            }

            return CreateUnknownFileType();
        }

        private DetectedFileType CreateUnknownFileType()
        {
            return new DetectedFileType
            {
                Category = FileCategory.Unknown,
                Format = "unknown",
                Icon = "❓",
                Description = "Unknown file type"
            };
        }

        private class CategoryInfo
        {
            public string Icon { get; set; } = string.Empty;
            public string[] Extensions { get; set; } = Array.Empty<string>();
        }
    }
}
