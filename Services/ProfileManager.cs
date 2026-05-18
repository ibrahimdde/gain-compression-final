using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FfmpegWrapper.Models;

namespace FfmpegWrapper.Services
{
    public class ProfileManager
    {
        private readonly string _filePath = "profiles.json";
        private List<CompressionProfile> _profiles;

        public ProfileManager()
        {
            _profiles = new List<CompressionProfile>();
            LoadProfiles();
        }
        public List<CompressionProfile> GetAllProfiles()
        {
            return _profiles;
        }

        public void AddProfile(CompressionProfile profile)
        {
            _profiles.Add(profile);
            SaveProfiles();
        }

        public void UpdateProfile(CompressionProfile updatedProfile)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].Id == updatedProfile.Id)
                {
                    _profiles[i].ProfileName = updatedProfile.ProfileName;
                    _profiles[i].Resolution = updatedProfile.Resolution;
                    _profiles[i].Bitrate = updatedProfile.Bitrate;
                    _profiles[i].Fps = updatedProfile.Fps;
                    _profiles[i].VideoKodek = updatedProfile.VideoKodek;
                    _profiles[i].HizOnayari = updatedProfile.HizOnayari;
                    SaveProfiles();
                    break; 
                }
            }
        }


        public void DeleteProfile(string id)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].Id == id)
                {
                    _profiles.RemoveAt(i); 
                    SaveProfiles();
                    break; 
                }
            }
        }

        private void LoadProfiles()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                _profiles = JsonSerializer.Deserialize<List<CompressionProfile>>(json);
                
                if (_profiles == null)
                {
                    _profiles = new List<CompressionProfile>();
                }
            }
            else
            {
                _profiles.Add(new CompressionProfile { ProfileName = "Varsayılan", Resolution = "1920x1080", Bitrate = 4000, Fps = 30, VideoKodek = "libx264", HizOnayari = "medium" });
                _profiles.Add(new CompressionProfile { ProfileName = "Düşük Boyut", Resolution = "1280x720", Bitrate = 1500, Fps = 24, VideoKodek = "libx265", HizOnayari = "fast" });
                SaveProfiles();
            }
        }

        private void SaveProfiles()
        {
            string json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
