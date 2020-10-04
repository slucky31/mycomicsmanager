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
        private readonly IMongoCollection<Library> _libraries;

        private readonly ILibrairiesSettings _libSettings;

        private readonly char[] charsToTrim = {'/', '\\'};

        public LibraryService(IDatabaseSettings dbSettings, ILibrairiesSettings libSettings)
        {
            Log.Debug("settings = {@settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _libraries = database.GetCollection<Library>(dbSettings.LibrariesCollectionName);
            _libSettings = libSettings;
        }

        public List<Library> Get() =>
            _libraries.Find(library => true).ToList();

        public Library Get(string id) =>
            _libraries.Find<Library>(library => library.Id == id).FirstOrDefault();

        public Library Create(Library libraryIn)
        {
            // Création de la librairie dans libs
            Directory.CreateDirectory(_libSettings.LibrairiesDirRootPath + libraryIn.RelPath);

            // Création de la librairie dans import
            Directory.CreateDirectory(_libSettings.FileImportDirRootPath + libraryIn.RelPath);
            
            // Création de la librairie dans MangoDB
            _libraries.InsertOne(libraryIn);

            return libraryIn;
        }

        public void Update(string id, Library libraryIn) {
            _libraries.ReplaceOne(library => library.Id == id, libraryIn);
        }

        public void Remove(Library libraryIn)
        {
            // Suppression des fichiers et du répértoire dans libs
            Directory.Delete(_libSettings.LibrairiesDirRootPath + libraryIn.RelPath, true);

            // Suppression des fichiers et du répértoire dans import
            Directory.Delete(_libSettings.FileImportDirRootPath + libraryIn.RelPath, true);
            
            // Suppression de la référence en base de données
            _libraries.DeleteOne(library => library.Id == libraryIn.Id);
        }

        public string GetLibrayRelPath(string id) {
            
            Library lib = this.Get(id);
            if (lib == null) {
                return null;
            } else {
                return lib.RelPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar;
            }
        }

        public string GetLibrayFullPath(string id) {

            Library lib = this.Get(id);
            if (lib == null) {
                return null;
            } else {
                return _libSettings.LibrairiesDirRootPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar + GetLibrayRelPath(id);
            }
        }

        public string GetLibrairiesDirRootPath() {

            return _libSettings.LibrairiesDirRootPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar;
        }

        public string GetFileUploadDirRootPath() {

            return _libSettings.FileUploadDirRootPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar;
        }

    }
}