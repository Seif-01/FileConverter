using System.Collections.Generic;
using FileConverter.Models;

namespace FileConverter.Services
{
    public class ConversionService
    {
        private readonly Dictionary<FileCategory, CategoryConversions> conversionMap;

        public ConversionService()
        {
            conversionMap = new Dictionary<FileCategory, CategoryConversions>
            {
                {
                    FileCategory.Document,
                    new CategoryConversions
                    {
                        Recommended = new List<string> { "pdf", "docx", "txt" },
                        AllFormats = new List<string> 
                        { 
                            "pdf", "docx", "doc", "txt", "rtf", "odt", 
                            "html", "md", "epub", "xps", "oxps" 
                        }
                    }
                },
                {
                    FileCategory.Image,
                    new CategoryConversions
                    {
                        Recommended = new List<string> { "jpg", "png", "webp" },
                        AllFormats = new List<string> 
                        { 
                            "jpg", "jpeg", "png", "gif", "bmp", "webp", 
                            "svg", "ico", "tiff", "pdf", "heic" 
                        }
                    }
                },
                {
                    FileCategory.Video,
                    new CategoryConversions
                    {
                        Recommended = new List<string> { "mp4", "webm", "avi" },
                        AllFormats = new List<string> 
                        { 
                            "mp4", "avi", "mkv", "mov", "wmv", "flv", 
                            "webm", "m4v", "mpg", "mpeg", "gif" 
                        }
                    }
                },
                {
                    FileCategory.Audio,
                    new CategoryConversions
                    {
                        Recommended = new List<string> { "mp3", "wav", "flac" },
                        AllFormats = new List<string> 
                        { 
                            "mp3", "wav", "flac", "aac", "ogg", "wma", 
                            "m4a", "opus", "alac" 
                        }
                    }
                },
                {
                    FileCategory.Archive,
                    new CategoryConversions
                    {
                        Recommended = new List<string> { "zip", "7z", "tar" },
                        AllFormats = new List<string> 
                        { 
                            "zip", "rar", "7z", "tar", "gz", "bz2", 
                            "xz", "tar.gz", "tar.bz2" 
                        }
                    }
                }
            };
        }

        public ConversionOptions GetConversionOptions(FileCategory category, string currentFormat)
        {
            if (!conversionMap.ContainsKey(category))
            {
                return new ConversionOptions
                {
                    Recommended = new List<string>(),
                    AllFormats = new List<string>()
                };
            }

            var conversions = conversionMap[category];
            var options = new ConversionOptions();

            foreach (var format in conversions.Recommended)
            {
                if (!format.Equals(currentFormat, System.StringComparison.OrdinalIgnoreCase))
                {
                    options.Recommended.Add(format);
                }
            }

            foreach (var format in conversions.AllFormats)
            {
                if (!format.Equals(currentFormat, System.StringComparison.OrdinalIgnoreCase))
                {
                    options.AllFormats.Add(format);
                }
            }

            AddContextSuggestions(category, currentFormat, options);

            return options;
        }

        private void AddContextSuggestions(FileCategory category, string currentFormat, ConversionOptions options)
        {
            switch (category)
            {
                case FileCategory.Image:
                    if (currentFormat != "webp" && !options.Recommended.Contains("webp"))
                    {
                        options.Recommended.Insert(0, "webp");
                    }
                    break;

                case FileCategory.Video:
                    if (currentFormat != "mp4" && !options.Recommended.Contains("mp4"))
                    {
                        options.Recommended.Insert(0, "mp4");
                    }
                    break;

                case FileCategory.Document:
                    if (currentFormat != "pdf" && !options.Recommended.Contains("pdf"))
                    {
                        options.Recommended.Insert(0, "pdf");
                    }
                    break;

                case FileCategory.Audio:
                    if (currentFormat != "mp3" && !options.Recommended.Contains("mp3"))
                    {
                        options.Recommended.Insert(0, "mp3");
                    }
                    break;
            }
        }

        private class CategoryConversions
        {
            public List<string> Recommended { get; set; } = new List<string>();
            public List<string> AllFormats { get; set; } = new List<string>();
        }
    }
}
