using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FileConverter.Services;
using FileConverter.Models;

namespace FileConverter
{
    public partial class MainWindow : Window
    {
        private FileInfo? currentFile;
        private string? selectedOutputFormat;
        private string? convertedFilePath;
        private readonly FileTypeDetector fileTypeDetector;
        private readonly ConversionService conversionService;
        private readonly FFmpegConverter ffmpegConverter;

        public MainWindow()
        {
            InitializeComponent();
            fileTypeDetector = new FileTypeDetector();
            conversionService = new ConversionService();
            ffmpegConverter = new FFmpegConverter();

            QualitySlider.ValueChanged += QualitySlider_ValueChanged;
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    ProcessFile(files[0]);
                }
            }

            DropZone.BorderThickness = new Thickness(2);
            DropZone.Opacity = 1.0;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                
                DropZone.BorderThickness = new Thickness(3);
                DropZone.Opacity = 0.9;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ChangeFile_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void ConversionOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string format)
            {
                selectedOutputFormat = format;
                StartConversion(format);
            }
        }

        private void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (QualityValueText != null)
            {
                QualityValueText.Text = $"Quality: {(int)e.NewValue}%";
            }
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (currentFile == null || string.IsNullOrEmpty(selectedOutputFormat))
                return;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(currentFile.Name) + "." + selectedOutputFormat.ToLower(),
                Filter = $"{selectedOutputFormat.ToUpper()} Files|*.{selectedOutputFormat.ToLower()}|All Files|*.*",
                DefaultExt = selectedOutputFormat.ToLower()
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(convertedFilePath) && File.Exists(convertedFilePath))
                    {
                        File.Copy(convertedFilePath, saveDialog.FileName, true);
                    }
                    else
                    {
                        File.Copy(currentFile.FullName, saveDialog.FileName, true);
                    }
                    
                    MessageBox.Show($"File saved successfully to:\n{saveDialog.FileName}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ConvertAnother_Click(object sender, RoutedEventArgs e)
        {
            ResetToUploadScreen();
        }

        private void SelectFile()
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Select a file to convert",
                Filter = "All Files|*.*|" +
                        "Documents|*.pdf;*.doc;*.docx;*.txt;*.rtf;*.odt|" +
                        "Images|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp;*.svg;*.ico|" +
                        "Videos|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|" +
                        "Audio|*.mp3;*.wav;*.flac;*.aac;*.ogg;*.wma;*.m4a|" +
                        "Archives|*.zip;*.rar;*.7z;*.tar;*.gz"
            };

            if (openDialog.ShowDialog() == true)
            {
                ProcessFile(openDialog.FileName);
            }
        }

        private void ProcessFile(string filePath)
        {
            try
            {
                currentFile = new FileInfo(filePath);
                
                var fileType = fileTypeDetector.DetectFileType(currentFile);
                
                UpdateFileInformation(fileType);
                
                ShowConversionOptions(fileType);
                
                SwitchToConversionScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFileInformation(DetectedFileType fileType)
        {
            if (currentFile == null) return;

            FileNameText.Text = currentFile.Name;

            FileIconText.Text = fileType.Icon;
            FileTypeText.Text = $"{fileType.Category} • {fileType.Format.ToUpper()} Format";

            FileSizeText.Text = FormatFileSize(currentFile.Length);
        }

        private void ShowConversionOptions(DetectedFileType fileType)
        {
            RecommendedConversions.Children.Clear();
            AllConversions.Children.Clear();

            var conversions = conversionService.GetConversionOptions(fileType.Category, fileType.Format);

            foreach (var format in conversions.Recommended)
            {
                var button = CreateConversionButton(format, true);
                RecommendedConversions.Children.Add(button);
            }

            foreach (var format in conversions.AllFormats)
            {
                var button = CreateConversionButton(format, false);
                AllConversions.Children.Add(button);
            }

            if (fileType.Category == FileCategory.Image || fileType.Category == FileCategory.Video)
            {
                QualitySettingsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                QualitySettingsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private Button CreateConversionButton(string format, bool isRecommended)
        {
            var button = new Button
            {
                Content = isRecommended ? $"⭐ {format.ToUpper()}" : format.ToUpper(),
                Tag = format,
                Style = (Style)FindResource("ConversionButton")
            };
            button.Click += ConversionOption_Click;
            return button;
        }

        private async void StartConversion(string targetFormat)
        {
            if (currentFile == null) return;

            SwitchToProgressScreen();

            ProgressStatusText.Text = $"Converting to {targetFormat.ToUpper()}...";
            ProgressDetailText.Text = "Processing your file...";

            try
            {
                var fileType = fileTypeDetector.DetectFileType(currentFile);
                
                if (fileType.Category == FileCategory.Video && targetFormat.ToLower() == "gif")
                {
                    await ConvertVideoToGif(targetFormat);
                }
                else
                {
                    await SimulateConversion(targetFormat);
                }

                SwitchToDownloadScreen(targetFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion error: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SwitchToConversionScreen();
            }
        }

        private async Task ConvertVideoToGif(string targetFormat)
        {
            if (currentFile == null) return;

            if (!ffmpegConverter.IsFFmpegAvailable())
            {
                throw new Exception("FFmpeg is not installed. Please install FFmpeg to convert videos to GIF.\n\nDownload from: https://ffmpeg.org/download.html");
            }

            var tempOutputPath = Path.Combine(Path.GetTempPath(), 
                Path.GetFileNameWithoutExtension(currentFile.Name) + ".gif");

            var quality = (int)QualitySlider.Value;

            var progress = new Progress<int>(percent =>
            {
                Dispatcher.Invoke(() =>
                {
                    ConversionProgressBar.Value = percent;
                    ProgressPercentText.Text = $"{percent}%";

                    if (percent < 30)
                        ProgressDetailText.Text = "Analyzing video...";
                    else if (percent < 70)
                        ProgressDetailText.Text = "Converting to GIF...";
                    else
                        ProgressDetailText.Text = "Finalizing...";
                });
            });

            bool success = await ffmpegConverter.ConvertVideoToGif(currentFile.FullName, tempOutputPath, quality, progress);

            if (!success)
            {
                throw new Exception("Video conversion failed. Please check if the video file is valid.");
            }

            convertedFilePath = tempOutputPath;

            ConversionProgressBar.Value = 100;
            ProgressPercentText.Text = "100%";
        }

        private async Task SimulateConversion(string targetFormat)
        {
            for (int i = 0; i <= 100; i += 5)
            {
                await Task.Delay(100);
                
                ConversionProgressBar.Value = i;
                ProgressPercentText.Text = $"{i}%";

                if (i == 20)
                    ProgressDetailText.Text = "Analyzing file structure...";
                else if (i == 40)
                    ProgressDetailText.Text = "Converting format...";
                else if (i == 60)
                    ProgressDetailText.Text = "Optimizing output...";
                else if (i == 80)
                    ProgressDetailText.Text = "Finalizing conversion...";
            }
        }

        private void SwitchToConversionScreen()
        {
            UploadScreen.Visibility = Visibility.Collapsed;
            ConversionScreen.Visibility = Visibility.Visible;
            ProgressScreen.Visibility = Visibility.Collapsed;
            DownloadScreen.Visibility = Visibility.Collapsed;
        }

        private void SwitchToProgressScreen()
        {
            UploadScreen.Visibility = Visibility.Collapsed;
            ConversionScreen.Visibility = Visibility.Collapsed;
            ProgressScreen.Visibility = Visibility.Visible;
            DownloadScreen.Visibility = Visibility.Collapsed;

            ConversionProgressBar.Value = 0;
            ProgressPercentText.Text = "0%";
        }

        private void SwitchToDownloadScreen(string format)
        {
            UploadScreen.Visibility = Visibility.Collapsed;
            ConversionScreen.Visibility = Visibility.Collapsed;
            ProgressScreen.Visibility = Visibility.Collapsed;
            DownloadScreen.Visibility = Visibility.Visible;

            ConvertedFileInfo.Text = $"Your file has been converted to {format.ToUpper()}";
        }

        private void ResetToUploadScreen()
        {
            UploadScreen.Visibility = Visibility.Visible;
            ConversionScreen.Visibility = Visibility.Collapsed;
            ProgressScreen.Visibility = Visibility.Collapsed;
            DownloadScreen.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(convertedFilePath) && File.Exists(convertedFilePath))
            {
                try { File.Delete(convertedFilePath); } catch { }
            }

            currentFile = null;
            selectedOutputFormat = null;
            convertedFilePath = null;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
