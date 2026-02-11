using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FileConverter.Services
{
    public class FFmpegConverter
    {
        private readonly string ffmpegPath;

        public FFmpegConverter()
        {
            ffmpegPath = FindFFmpeg();
        }

        public async Task<bool> ConvertVideoToGif(string inputPath, string outputPath, int quality, IProgress<int>? progress = null)
        {
            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("FFmpeg not found. Please install FFmpeg and add it to your system PATH.");
            }

            int fps = quality > 70 ? 15 : quality > 40 ? 10 : 5;
            int scale = quality > 70 ? 480 : quality > 40 ? 360 : 240;

            string arguments = $"-i \"{inputPath}\" -vf \"fps={fps},scale={scale}:-1:flags=lanczos\" -c:v gif \"{outputPath}\" -y";

            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            var errorOutput = string.Empty;
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorOutput += e.Data + "\n";
                    if (progress != null && e.Data.Contains("time="))
                    {
                        var percent = ExtractProgress(e.Data);
                        if (percent >= 0)
                        {
                            progress.Report(percent);
                        }
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }

        private int ExtractProgress(string ffmpegOutput)
        {
            try
            {
                var timeIndex = ffmpegOutput.IndexOf("time=");
                if (timeIndex > 0)
                {
                    return 50;
                }
            }
            catch { }
            return -1;
        }

        private string FindFFmpeg()
        {
            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(localPath))
                return localPath;

            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
            foreach (var path in paths)
            {
                var ffmpegExe = Path.Combine(path.Trim(), "ffmpeg.exe");
                if (File.Exists(ffmpegExe))
                    return ffmpegExe;
            }

            return "ffmpeg";
        }

        public bool IsFFmpegAvailable()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }
    }
}
