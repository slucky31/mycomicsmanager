using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyComicsManager.Model.Shared.Services;
using MyComicsManagerWeb.Models;


namespace MyComicsManagerWeb.Services {
    
    public class ThumbnailService
    {
        private readonly string _pathThumbs;

        public ThumbnailService(ApplicationConfigurationService applicationConfigurationService)
        {
            _pathThumbs = applicationConfigurationService.GetPathThumbs();
        }

        public void ClearAll()
        {

            if (!Directory.Exists(_pathThumbs))
            {
                return;
            }
            
            //Delete all files from the Directory
            foreach (var file in Directory.GetFiles(_pathThumbs))
            {
                File.Delete(file);
            }
            
            //Delete all child Directories
            foreach (var directory in Directory.GetDirectories(_pathThumbs))
            {
                Directory.Delete(directory, true);
            }
        }

        public void Clear(string id)
        {
            var deleteFiles = Directory.EnumerateFiles(_pathThumbs, id + "*", SearchOption.AllDirectories).ToList();
            foreach (var file in deleteFiles)
            {
                File.Delete(file);
            }
        }
    }
}