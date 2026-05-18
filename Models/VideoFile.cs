namespace FfmpegWrapper.Models
{
   
    public class VideoFile : MediaFile
    {
        public VideoFile(string filePath) : base(filePath)
        {
        }

        public string GetMediaDescription()
        {
            return $"Video Dosyası: {FileName}{FileExtension} (Boyut: {SizeInBytes / 1024 / 1024} MB)";
        }
    }
}
