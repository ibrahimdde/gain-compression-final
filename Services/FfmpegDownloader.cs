using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FfmpegWrapper.Services
{
    public class FfmpegDownloader
    {
        private const string FfmpegUrlWin = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        private const string FfmpegUrlMac = "https://evermeet.cx/ffmpeg/getrelease/zip";
        
        public event Action<double> OnDownloadProgressChanged;
        public event Action<string> OnDownloadStatusChanged;

        public async Task DownloadFfmpegIfNeededAsync()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegDir = Path.Combine(baseDir, "ffmpeg");
            
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            string exeName = isMac ? "ffmpeg" : "ffmpeg.exe";
            string ffmpegExe = Path.Combine(ffmpegDir, exeName);

            if (File.Exists(ffmpegExe))
            {
                return; 
            }

            OnDownloadStatusChanged?.Invoke("Sistemde FFMPEG bulunamadı. İlk kurulum için indiriliyor");

            if (!Directory.Exists(ffmpegDir))
            {
                Directory.CreateDirectory(ffmpegDir);
            }

            string zipPath = Path.Combine(baseDir, "ffmpeg_temp.zip");
            string extractPath = Path.Combine(baseDir, "ffmpeg_extracted");

            try
            {
                string downloadUrl = isMac ? FfmpegUrlMac : FfmpegUrlWin;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "FfmpegWrapper/1.0");

                    using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (canReportProgress)
                                {
                                    double progress = (double)totalRead / totalBytes * 100;
                                    OnDownloadProgressChanged?.Invoke(progress);
                                }
                            }
                        }
                    }
                }

                OnDownloadStatusChanged?.Invoke("İndirme tamamlandı. Dosyalar arşivden çıkarılıyor...");

                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                ZipFile.ExtractToDirectory(zipPath, extractPath);

                var extractedExePath = Directory.GetFiles(extractPath, exeName, SearchOption.AllDirectories).FirstOrDefault();
                
                if (extractedExePath != null)
                {
                    File.Copy(extractedExePath, ffmpegExe, true);
                    
                    if (isMac)
                    {
                        var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{ffmpegExe}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        process?.WaitForExit();
                    }
                }

                OnDownloadStatusChanged?.Invoke("Kurulum Başarılı! Uygulama kullanıma hazır.");
            }
            finally
            {
                OnDownloadStatusChanged?.Invoke("Geçici dosyalar temizleniyor...");
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                try { if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true); } catch { }
            }
        }
    }
}
