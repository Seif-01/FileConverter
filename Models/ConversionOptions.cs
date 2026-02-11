using System.Collections.Generic;

namespace FileConverter.Models
{
    public class ConversionOptions
    {
        public List<string> Recommended { get; set; } = new List<string>();
        public List<string> AllFormats { get; set; } = new List<string>();
    }
}
