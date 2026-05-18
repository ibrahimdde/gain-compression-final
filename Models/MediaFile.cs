using System.IO;

namespace FfmpegWrapper.Models
{
    public abstract class MediaFile
    {
        private string _filePath;
        private string _fileName;
        private string _fileExtension;
        private long _sizeInBytes;

        public string FilePath
        {
            get { return _filePath; }
            protected set { _filePath = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            protected set { _fileName = value; }
        }

        public string FileExtension
        {
            get { return _fileExtension; }
            protected set { _fileExtension = value; }
        }

        public long SizeInBytes
        {
            get { return _sizeInBytes; }
            protected set { _sizeInBytes = value; }
        }

        protected MediaFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dosya bulunamadı.", filePath);

            FilePath = filePath;
            FileName = Path.GetFileNameWithoutExtension(filePath);
            FileExtension = Path.GetExtension(filePath);
            SizeInBytes = new FileInfo(filePath).Length;
        }

    
    }
}