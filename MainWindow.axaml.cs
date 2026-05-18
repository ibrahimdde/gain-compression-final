using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using FfmpegWrapper.Models;
using FfmpegWrapper.Services;

namespace FfmpegWrapper
{
    public partial class MainWindow : Window
    {
        private readonly ProfileManager _profileManager;
        private readonly FfmpegEngine _ffmpegEngine;
        private VideoFile _currentVideo;
        private string _outputDirectory;
        private Stopwatch _stopwatch;
        private DispatcherTimer _timer;
        private bool _loadingProfiles = false;

        public MainWindow()
        {
            InitializeComponent();
            _profileManager = new ProfileManager();
            _ffmpegEngine = new FfmpegEngine();

            _ffmpegEngine.OnProgressChanged += FfmpegEngine_OnProgressChanged;
            _ffmpegEngine.OnLogReceived += FfmpegEngine_OnLogReceived;

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => txtTime.Text = $"Geçen Süre: {_stopwatch.Elapsed:hh\\:mm\\:ss}";

       // avalonianın drag&drop implementasyonu
            AddHandler(DragDrop.DropEvent, Pencere_DosyaBirakildiginda);
            AddHandler(DragDrop.DragOverEvent, (object? s, DragEventArgs ev) =>
            {
                ev.DragEffects = ev.DataTransfer.Contains(DataFormat.File)
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
            });

            LoadProfilesToUI();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var downloader = new FfmpegDownloader();
            
            downloader.OnDownloadProgressChanged += (percentage) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    progressBar.Value = percentage;
                    txtPercentage.Text = $"%{percentage:F1}";
                });
            };

            downloader.OnDownloadStatusChanged += (status) =>
            {
                Dispatcher.UIThread.Post(() => LogToUI(status));
            };

            btnStart.IsEnabled = false;
            grpFileOps.IsEnabled = false;
            grpProfileOps.IsEnabled = false;

            try
            {
                await downloader.DownloadFfmpegIfNeededAsync();
            }
            catch (Exception ex)
            {
                await ShowError("İndirme Hatası", "FFMPEG indirilemedi.\n\nDetay: " + ex.Message);
            }
            finally
            {
                btnStart.IsEnabled = true;
                grpFileOps.IsEnabled = true;
                grpProfileOps.IsEnabled = true;
                progressBar.Value = 0;
                txtPercentage.Text = "%0.0";
            }
        }

        private void LoadProfilesToUI()
        {
            _loadingProfiles = true;

            cmbProfiles.ItemsSource = new List<CompressionProfile>();
            cmbProfiles.ItemsSource = _profileManager.GetAllProfiles().ToList();

            _loadingProfiles = false;

            if (cmbProfiles.ItemCount > 0)
                cmbProfiles.SelectedIndex = 0;
        }

        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingProfiles) return;

            if (cmbProfiles.SelectedItem is not CompressionProfile profile)
                return;

            txtProfileName.Text = profile.ProfileName;
            txtResolution.Text = profile.Resolution;
            txtBitrate.Text = profile.Bitrate.ToString();
            txtFps.Text = profile.Fps.ToString();

            if (profile.VideoKodek == "libx265") cmbCodec.SelectedIndex = 1;
            else if (profile.VideoKodek == "libaom-av1") cmbCodec.SelectedIndex = 2;
            else cmbCodec.SelectedIndex = 0;

            if (profile.HizOnayari == "ultrafast") cmbPreset.SelectedIndex = 0;
            else if (profile.HizOnayari == "fast") cmbPreset.SelectedIndex = 1;
            else if (profile.HizOnayari == "slow") cmbPreset.SelectedIndex = 3;
            else cmbPreset.SelectedIndex = 2;
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            cmbProfiles.SelectedIndex = -1;
            txtProfileName.Text = "Yeni Profil";
            txtResolution.Text = "1920x1080";
            txtBitrate.Text = "2000";
            txtFps.Text = "30";
            cmbCodec.SelectedIndex = 0;
            cmbPreset.SelectedIndex = 2;
        }

        private async void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtBitrate.Text, out int bitrate) || !int.TryParse(txtFps.Text, out int fps))
            {
                await ShowError("Hata", "Bitrate ve FPS sadece sayı olmalıdır.");
                return;
            }

            string kodekDegeri = "libx264"; 
            if (cmbCodec.SelectedItem is ComboBoxItem codecItem)
            {
                string codecYazisi = codecItem.Content?.ToString() ?? "";
                if (codecYazisi.Contains("H265")) kodekDegeri = "libx265";
                else if (codecYazisi.Contains("AV1")) kodekDegeri = "libaom-av1";
            }

            string hizDegeri = "medium"; 
            if (cmbPreset.SelectedItem is ComboBoxItem presetItem)
            {
                string hizYazisi = presetItem.Content?.ToString() ?? "";
                if (hizYazisi.Contains("ultrafast")) hizDegeri = "ultrafast";
                else if (hizYazisi.Contains("fast")) hizDegeri = "fast";
                else if (hizYazisi.Contains("slow")) hizDegeri = "slow";
            }

            if (cmbProfiles.SelectedItem is CompressionProfile selectedProfile)
            {
                selectedProfile.ProfileName = txtProfileName.Text;
                selectedProfile.Resolution = txtResolution.Text;
                selectedProfile.Bitrate = bitrate;
                selectedProfile.Fps = fps;
                selectedProfile.VideoKodek = kodekDegeri;
                selectedProfile.HizOnayari = hizDegeri;
                _profileManager.UpdateProfile(selectedProfile);
                await ShowInfo("Başarılı", "Profil başarıyla güncellendi.");
            }
            else
            {
                var newProfile = new CompressionProfile
                {
                    ProfileName = txtProfileName.Text,
                    Resolution = txtResolution.Text,
                    Bitrate = bitrate,
                    Fps = fps,
                    VideoKodek = kodekDegeri,
                    HizOnayari = hizDegeri
                };
                _profileManager.AddProfile(newProfile);
                await ShowInfo("Başarılı", "Yeni profil başarıyla eklendi.");
            }
            LoadProfilesToUI();
            
            var tumProfiller = _profileManager.GetAllProfiles();
            foreach (var p in tumProfiller)
            {
                if (p.ProfileName == txtProfileName.Text)
                {
                    cmbProfiles.SelectedItem = p;
                    break;
                }
            }
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is not CompressionProfile selectedProfile)
                return;

            try
            {
                _profileManager.DeleteProfile(selectedProfile.Id);
                LoadProfilesToUI();
                LogToUI($"'{selectedProfile.ProfileName}' profili silindi.");
            }
            catch (Exception ex)
            {
                LogToUI($"Profil silme hatası: {ex.Message}");
            }
        }

    
        private void Pencere_DosyaBirakildiginda(object? sender, DragEventArgs e)
        {
            
            if (e.DataTransfer.Contains(DataFormat.File))
            {
                var files = e.DataTransfer.TryGetFiles();
                if (files != null)
                {
                    var firstFile = files.FirstOrDefault();
                    if (firstFile != null)
                    {
                        var path = firstFile.TryGetLocalPath();
                        if (path != null)
                            VideoyuAyarla(path);
                    }
                }
            }
        }

        private void VideoyuAyarla(string dosyaYolu)
        {
            try
            {
                _currentVideo = new VideoFile(dosyaYolu);
                txtInputFile.Text = _currentVideo.FilePath;
                _outputDirectory = System.IO.Path.GetDirectoryName(_currentVideo.FilePath);
                txtOutputDir.Text = _outputDirectory;
                LogToUI("Dosya Seçildi: " + _currentVideo.GetMediaDescription());
            }
            catch (Exception ex)
            {
                ShowError("Dosya Hatası", ex.Message);
            }
        }

        private async void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Sıkıştırılacak Videoyu Seçin",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Video Dosyaları")
                    {
                        Patterns = new[] { "*.mp4", "*.mkv", "*.avi", "*.mov" }
                    }
                }
            });

            if (files != null && files.Count > 0)
            {
                VideoyuAyarla(files[0].Path.LocalPath);
            }
        }

        private async void BtnSelectOutputDir_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Çıktı Klasörünü Seçin",
                AllowMultiple = false
            });

            if (folders != null && folders.Count > 0)
            {
                _outputDirectory = folders[0].Path.LocalPath;
                txtOutputDir.Text = _outputDirectory;
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_currentVideo == null)
            {
                await ShowInfo("Uyarı", "Lütfen önce bir video seçin.");
                return;
            }

            if (!int.TryParse(txtBitrate.Text, out int bitrate) || !int.TryParse(txtFps.Text, out int fps))
            {
                await ShowInfo("Uyarı", "Lütfen geçerli değerler girin.");
                return;
            }

            string kodekDegeri = "libx264";
            if (cmbCodec.SelectedItem is ComboBoxItem codecItem)
            {
                string codecYazisi = codecItem.Content?.ToString() ?? "";
                if (codecYazisi.Contains("H265")) kodekDegeri = "libx265";
                else if (codecYazisi.Contains("AV1")) kodekDegeri = "libaom-av1";
            }

            string hizDegeri = "medium";
            if (cmbPreset.SelectedItem is ComboBoxItem presetItem)
            {
                string hizYazisi = presetItem.Content?.ToString() ?? "";
                if (hizYazisi.Contains("ultrafast")) hizDegeri = "ultrafast";
                else if (hizYazisi.Contains("fast")) hizDegeri = "fast";
                else if (hizYazisi.Contains("slow")) hizDegeri = "slow";
            }

            var activeProfile = new CompressionProfile
            {
                Resolution = txtResolution.Text,
                Bitrate = bitrate,
                Fps = fps,
                VideoKodek = kodekDegeri,
                HizOnayari = hizDegeri
            };

            btnStart.IsEnabled = false;
            progressBar.Value = 0;
            txtPercentage.Text = "%0.0";
            txtTime.Text = "Geçen Süre: 00:00:00";
            _stopwatch.Restart();
            _timer.Start();
            LogToUI(" SIKIŞTIRMA İŞLEMİ BAŞLADI ");
            LogToUI($"Hedef Çözünürlük: {activeProfile.Resolution}, Bitrate: {activeProfile.Bitrate}k, FPS: {activeProfile.Fps}, Codec: {activeProfile.VideoKodek}, Preset: {activeProfile.HizOnayari}");

            try
            {
                string finalPath = await _ffmpegEngine.CompressVideoAsync(_currentVideo, _outputDirectory, activeProfile);
                
                _stopwatch.Stop();
                _timer.Stop();
                
                LogToUI(" İŞLEM BAŞARIYLA TAMAMLANDI ");
                await ShowInfo("Başarılı", "Sıkıştırma işlemi başarıyla tamamlandı!\n\nKaydedilen Dosya:\n" + finalPath);
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                _timer.Stop();
                
                LogToUI($"HATA: {ex.Message}");
                await ShowError("İşlem Hatası", ex.Message);
            }
            finally
            {
                _stopwatch.Stop();
                _timer.Stop();
                btnStart.IsEnabled = true;
            }
        }

        private void FfmpegEngine_OnProgressChanged(double percentage, string currentTime)
        {
            Dispatcher.UIThread.Post(() =>
            {
                progressBar.Value = percentage;
                txtPercentage.Text = $"%{percentage:F1}";
            });
        }

        private void FfmpegEngine_OnLogReceived(string log)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!log.StartsWith("frame="))
                {
                    LogToUI(log);
                }
            });
        }

        private void LogToUI(string message)
        {
            txtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
       
            txtLog.CaretIndex = int.MaxValue;
        }

        private async Task ShowInfo(string title, string text)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
        }

        private async Task ShowError(string title, string text)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowAsync();
        }
    }
}
