using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.IO;

namespace MyComicsManagerApi.Services
{
    public class LibraryService
    {
        private readonly IMongoCollection<Comic> _comics;

        public LibraryService(IDatabaseSettings settings)
        {
            Log.Debug("settings = {@settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.LibrariesCollectionName);
        }

        public List<Comic> Get() =>
            _comics.Find(comic => true).ToList();

        public Comic Get(string id) =>
            _comics.Find<Comic>(comic => comic.Id == id).FirstOrDefault();

        public Comic Create(Comic comic)
        {
            _comics.InsertOne(comic);
            return comic;
        }

        public void Update(string id, Comic comicIn) =>
            _comics.ReplaceOne(comic => comic.Id == id, comicIn);

        public void Remove(Comic comicIn)
        {
            // Suppression du fichier
            Comic c = _comics.Find<Comic>(comic => (comic.Id == comicIn.Id) && (comic.EbookPath == comicIn.EbookPath)).FirstOrDefault();
            if (c != null) {    
                File.Delete(c.EbookPath);
            }

            //TODO : Gestion des exceptions

            // Suppression de la référence en base de données
            _comics.DeleteOne(comic => comic.Id == comicIn.Id);
        }

        public void Remove(string id) => 
            _comics.DeleteOne(comic => comic.Id == id);
    }
}