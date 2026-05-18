using System;

namespace FfmpegWrapper.Models
{
    public class CompressionProfile
    {
        private string _id;
        private string _profileName;
        private string _resolution;
        private int _bitrate;
        private int _fps;
        private string _videoKodek;
        private string _hizOnayari;

        public CompressionProfile()
        {
            _id = Guid.NewGuid().ToString();
            _videoKodek = "libx264";
            _hizOnayari = "medium";
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string ProfileName
        {
            get { return _profileName; }
            set { _profileName = value; }
        }

        public string Resolution
        {
            get { return _resolution; }
            set { _resolution = value; }
        }

        public int Bitrate
        {
            get { return _bitrate; }
            set { _bitrate = value; }
        }

        public int Fps
        {
            get { return _fps; }
            set { _fps = value; }
        }

        public string VideoKodek
        {
            get { return _videoKodek; }
            set { _videoKodek = value; }
        }

        public string HizOnayari
        {
            get { return _hizOnayari; }
            set { _hizOnayari = value; }
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ProfileName) && Bitrate > 0 && Fps > 0
                && !string.IsNullOrWhiteSpace(VideoKodek) && !string.IsNullOrWhiteSpace(HizOnayari);
        }
    }
}
