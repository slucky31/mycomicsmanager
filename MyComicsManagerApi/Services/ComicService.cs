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

        public ComicService(IDatabaseSettings settings, LibraryService libraryService)
        {
            Log.Debug("settings = {@settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
            _libraryService = libraryService;
        }

        public List<Comic> Get() =>
            _comics.Find(comic => true).ToList();

        public Comic Get(string id) =>
            _comics.Find<Comic>(comic => comic.Id == id).FirstOrDefault();

        public Comic Create(Comic comic)
        {
            // Le fichier a été copié par le front dans le zone tmp.
            // Copie du fichier dans la bonne librairie
            // Path.DirectorySeparatorChar : https://docs.microsoft.com/fr-fr/dotnet/api/system.io.path.directoryseparatorchar?view=netcore-3.1
            char[] charsToTrim = {'/', '\\'};

            string origin = _libraryService.GetFileUploadDirRootPath() + comic.EbookName;
            string destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.FULL_PATH) + comic.EbookName;

            try
            {
                File.Move(origin,destination);
                //TODO : Gestion des exceptions
            }
            catch (System.Exception)
            {
                
                Log.Error("Erreur lors du dépalcement du fichier");
                Log.Error("Origin = {@origin}", origin);
                Log.Error("Destination = {@destination}", destination);
                return null;
            }
            
            // Mise à jour du champs EbookPath avec le champ relatif
            comic.EbookPath = comic.EbookName;

            // Insertion en base de données
            _comics.InsertOne(comic);
            
            return comic;
        }

        public void Update(string id, Comic comic) =>
            _comics.ReplaceOne(c => comic.Id == id, comic);

        public void Remove(Comic comic)
        {
            // Suppression du fichier
            Comic c = _comics.Find<Comic>(c => (c.Id == comic.Id) && (c.EbookName == comic.EbookName)).FirstOrDefault();
            if (c != null) {    
                
                string filePath = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.FULL_PATH) + comic.EbookPath;
                if (File.Exists(filePath)) {
                    File.Delete(filePath);
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