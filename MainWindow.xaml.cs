using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FileConverter.Services;
using FileConverter.Models;

namespace FileConverter
{
    public partial class MainWindow : Window
    {
        private FileInfo? _currentFile;
        private string? _selectedOutputFormat;
        private string? _convertedFilePath;
        private readonly FileTypeDetector _fileTypeDetector;
        private readonly ConversionService _conversionService;
        private readonly FFmpegConverter _ffmpegConverter;

        public MainWindow()
        {
            InitializeComponent();
            _fileTypeDetector = new FileTypeDetector();
            _conversionService = new ConversionService();
            _ffmpegConverter = new FFmpegConverter();

            QualitySlider.ValueChanged += QualitySlider_ValueChanged;
        }

        #region Upload Screen Events

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

            // Reset drop zone appearance
            DropZone.BorderThickness = new Thickness(2);
            DropZone.Opacity = 1.0;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                
                // Highlight drop zone
                DropZone.BorderThickness = new Thickness(3);
                DropZone.Opacity = 0.9;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        #endregion

        #region Conversion Screen Events

        private void ChangeFile_Click(object sender, RoutedEventArgs e)
        {
            SelectFile();
        }

        private void ConversionOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string format)
            {
                _selectedOutputFormat = format;
                StartConversion(format);
            }
        }

        private void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (QualityValueText != null)
            {
                QualityValueText.Text = $"{(int)e.NewValue}%";
            }
        }

        #endregion

        #region Download Screen Events

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null || string.IsNullOrEmpty(_selectedOutputFormat))
                return;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(_currentFile.Name) + "." + _selectedOutputFormat.ToLower(),
                Filter = $"{_selectedOutputFormat.ToUpper()} Files|*.{_selectedOutputFormat.ToLower()}|All Files|*.*",
                DefaultExt = _selectedOutputFormat.ToLower()
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_convertedFilePath) && File.Exists(_convertedFilePath))
                    {
                        File.Copy(_convertedFilePath, saveDialog.FileName, true);
                    }
                    else
                    {
                        File.Copy(_currentFile.FullName, saveDialog.FileName, true);
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

        #endregion

        #region File Processing

        private void SelectFile()
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Title = "Select a file to convert",
                Filter = "All Files|*.*|" +
                        "Documents|*.pdf;*.doc;*.docx;*.txt;*.rtf;*.odt|" +
                        "Images|*.jpg;*.jpeg;*.jfif;*.png;*.gif;*.bmp;*.webp;*.svg;*.ico;*.tiff;*.tif;*.heic;*.avif|" +
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
                _currentFile = new FileInfo(filePath);
                
                // Detect file type
                var fileType = _fileTypeDetector.DetectFileType(_currentFile);
                
                // Update UI with file information
                UpdateFileInformation(fileType);
                
                // Show conversion options
                ShowConversionOptions(fileType);
                
                // Switch to conversion screen
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
            if (_currentFile == null) return;

            // Update file name
            FileNameText.Text = _currentFile.Name;

            // Update file type and icon
            FileIconText.Text = fileType.Icon;
            FileTypeText.Text = $"{fileType.Category} • {fileType.Format.ToUpper()} Format";

            // Update file size
            FileSizeText.Text = FormatFileSize(_currentFile.Length);
        }

        private void ShowConversionOptions(DetectedFileType fileType)
        {
            // Clear existing options
            RecommendedConversions.Children.Clear();
            AllConversions.Children.Clear();

            var conversions = _conversionService.GetConversionOptions(fileType.Category, fileType.Format);

            // Add recommended conversions
            foreach (var format in conversions.Recommended)
            {
                var button = CreateConversionButton(format, true);
                RecommendedConversions.Children.Add(button);
            }

            // Add all available conversions
            foreach (var format in conversions.AllFormats)
            {
                var button = CreateConversionButton(format, false);
                AllConversions.Children.Add(button);
            }

            // Show quality settings for image/video conversions
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
                Content = isRecommended ? $"★  {format.ToUpper()}" : format.ToUpper(),
                Tag = format,
                Style = (Style)FindResource(isRecommended ? "RecommendedButton" : "ConversionButton")
            };
            button.Click += ConversionOption_Click;
            return button;
        }

        #endregion

        #region Conversion Process

        private async void StartConversion(string targetFormat)
        {
            if (_currentFile == null) return;

            SwitchToProgressScreen();

            ProgressStatusText.Text = $"Converting to {targetFormat.ToUpper()}...";
            ProgressDetailText.Text = "Processing your file...";

            try
            {
                var fileType = _fileTypeDetector.DetectFileType(_currentFile);
                
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
            if (_currentFile == null) return;

            if (!_ffmpegConverter.IsFFmpegAvailable())
            {
                throw new Exception("FFmpeg is not installed. Please install FFmpeg to convert videos to GIF.\n\nDownload from: https://ffmpeg.org/download.html");
            }

            var tempOutputPath = Path.Combine(Path.GetTempPath(), 
                Path.GetFileNameWithoutExtension(_currentFile.Name) + ".gif");

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

            bool success = await _ffmpegConverter.ConvertVideoToGif(_currentFile.FullName, tempOutputPath, quality, progress);

            if (!success)
            {
                throw new Exception("Video conversion failed. Please check if the video file is valid.");
            }

            _convertedFilePath = tempOutputPath;

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

        #endregion

        #region Screen Transitions

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

            // Reset progress
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

            if (!string.IsNullOrEmpty(_convertedFilePath) && File.Exists(_convertedFilePath))
            {
                try { File.Delete(_convertedFilePath); } catch { }
            }

            _currentFile = null;
            _selectedOutputFormat = null;
            _convertedFilePath = null;
        }

        #endregion

        #region Utility Methods

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

        #endregion
    }
}
