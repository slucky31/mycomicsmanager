using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using System;

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

            string origin = Path.GetFullPath(_libraryService.GetFileUploadDirRootPath() + comic.EbookName);
            

            // TODO : Gérer le champ EbookPath en rélatif ou en absolu ???
            // Mise à jour du champs EbookPath avec le champ relatif

            // Si PDF, conversion en CBZ
            // TODO : EbookPath ne devrait jamais être null,  car un comic ne peut exister sans fichier !
            comic.EbookPath = origin;
            _comicFileService.ConvertComicFileToCbz(comic);

            string destination = Path.GetFullPath(_libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.FULL_PATH) + comic.EbookName);
            try
            {
                
                File.Move(comic.EbookPath, destination);
                comic.EbookPath = destination;
                //TODO : Gestion des exceptions
            }
            catch (Exception e)
            {                
                Log.Error("Erreur lors du dépalcement du fichier : {0}", e.Message);
                Log.Error("Origin = {@origin}", comic.EbookPath);
                Log.Error("Destination = {@destination}", destination);
                return null;
            }
            
            // Insertion en base de données
            _comics.InsertOne(comic);
 
            // Extraction de l'image de couverture        
            _comicFileService.SetAndExtractCoverImage(comic);
            var filter = Builders<Comic>.Filter.Eq(comic => comic.Id,comic.Id);
            var update = Builders<Comic>.Update.Set(comic => comic.CoverPath, comic.CoverPath);            
            this.UpdateField(filter, update);

            return comic;
        }

        public void Update(string id, Comic comic)
        {
            _comics.ReplaceOne(comic => comic.Id == id, comic);
        }

        public void UpdateField(FilterDefinition<Comic> filter, UpdateDefinition<Comic> update)
        {
            var options = new UpdateOptions { IsUpsert = true };
            _comics.UpdateOne(filter, update, options);
        }

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