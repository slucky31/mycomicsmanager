using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace MyComicsManagerApi.Services
{
    public class ComicService
    {
        private readonly IMongoCollection<Comic> _comics;
        private readonly LibraryService _libraryService;
        private readonly ComicFileService _comicFileService;

        public ComicService(IDatabaseSettings settings, LibraryService libraryService, ComicFileService comicFileService)
        {
            Log.Debug("settings = {@settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
            _libraryService = libraryService;
            _comicFileService = comicFileService;
        }

        public List<Comic> Get() =>
            _comics.Find(comic => true).ToList();

        public Comic Get(string id) =>
            _comics.Find<Comic>(comic => comic.Id == id).FirstOrDefault();

        public Comic Create(Comic comic)
        {

            Log.Information("Comic à créer : {@comic}", comic);

            string origin = Path.GetFullPath(_libraryService.GetFileUploadDirRootPath() + comic.EbookName);
            string destination = Path.GetFullPath(_libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.FULL_PATH) + comic.EbookName);

            try
            {
                File.Move(origin,destination);
                comic.EbookPath = destination;
                //TODO : Gestion des exceptions
            }
            catch (System.Exception)
            {
                
                Log.Error("Erreur lors du dépalcement du fichier");
                Log.Error("Origin = {@origin}", origin);
                Log.Error("Destination = {@destination}", destination);
                return null;
            }

            Log.Information("Comic = {@comic}", comic);

            // TODO : Gérer le champ EbookPath en rélatif ou en absolu ???
            // Mise à jour du champs EbookPath avec le champ relatif
            //comic.EbookPath = comic.EbookName;

            // Extraction de l'image de couverture
            _comicFileService.SetAndExtractCoverImage(comic);

            Log.Information("Comic = {@comic}", comic);

            // Insertion en base de données
            _comics.InsertOne(comic);
            
            return comic;
        }

        public void Update(string id, Comic comic) =>
            _comics.ReplaceOne(c => comic.Id == id, comic);

        public void Remove(Comic comic)
        {
            // Suppression du fichier
            Comic c = _comics.Find<Comic>(c => (c.Id == comic.Id) && (c.EbookPath == comic.EbookPath)).FirstOrDefault();
            if (c != null) {    
                
                // Suppression du fichier
                if (File.Exists(c.EbookPath)) {
                    File.Delete(c.EbookPath);
                }

                // Suppression de l'image de couverture
                if (File.Exists(c.CoverPath))
                {
                    File.Delete(c.CoverPath);
                }
                //TODO : Gestion des exceptions
            }

            // Suppression de la référence en base de données
            _comics.DeleteOne(c => c.Id == comic.Id);
        }

        public void RemoveAllComicsFromLibrary(string libId)
        {
            // Suppression du fichier
            List<Comic> comics = _comics.Find<Comic>(c => (c.LibraryId == libId)).ToList();
            foreach(Comic c in comics) {
                Remove(c);
            }
        }

    }
}