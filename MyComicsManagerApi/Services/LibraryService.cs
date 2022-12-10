using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyComicsManager.Model.Shared.Models;
using MyComicsManager.Model.Shared.Services;
using MyComicsManagerApi.Settings;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.Services
{
    public class LibraryService : ILibraryService
    {
        private static ILogger Log => Serilog.Log.ForContext<LibraryService>();
        
        private readonly IMongoCollection<Library> _libraries;
        private readonly char[] _charsToTrim = {'/', '\\'};
        private readonly ApplicationConfigurationService _applicationConfigurationService;
        private Dictionary<string,Dictionary<string, string>> _listOfLibraryFiles = new(); 

        public enum PathType
        {
            RELATIVE_PATH,
            ABSOLUTE_PATH
        }

        public LibraryService(IDatabaseSettings dbSettings, ApplicationConfigurationService applicationConfigurationService)
        {
            Log.Here().Debug("settings = {@Settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _libraries = database.GetCollection<Library>(dbSettings.LibrariesCollectionName);
            _applicationConfigurationService = applicationConfigurationService;
        }

        public List<Library> Get() =>
            _libraries.Find(library => true).ToList();

        public Library Get(string id) =>
            _libraries.Find(library => library.Id == id).FirstOrDefault();

        public Library Create(Library libraryIn)
        {
            // Création de la librairie dans MangoDB
            _libraries.InsertOne(libraryIn);

            // Création de la librairie dans libs
            Directory.CreateDirectory(GetLibraryPath(libraryIn.Id, PathType.ABSOLUTE_PATH));

            return libraryIn;
        }

        public void Update(string id, Library libraryIn) {
            _libraries.ReplaceOne(library => library.Id == id, libraryIn);
        }

        public void Remove(Library libraryIn)
        {
            
            // Suppression des fichiers et du répertoire dans libs
            string libPath = GetLibraryPath(libraryIn.Id, PathType.ABSOLUTE_PATH);
            if (Directory.Exists(libPath))
            {
                Directory.Delete(libPath, true);
            }

            // Suppression de la référence en base de données
            _libraries.DeleteOne(library => library.Id == libraryIn.Id);
        }

        public string GetLibraryPath(string id, PathType type)
        {    
            var lib = this.Get(id);
            if (lib == null)
            {
                return null;
            } else {
                string path = "";
                if (type == PathType.ABSOLUTE_PATH)
                {
                    path = _applicationConfigurationService.GetPathLibrairies();

                }
                return path + lib.RelPath.TrimEnd(_charsToTrim) + Path.DirectorySeparatorChar; 
            }
        }

        private void Scan(string id)
        {
            var absoluteLibPath = _applicationConfigurationService.GetPathApplication();
            var directory = new DirectoryInfo(absoluteLibPath);

            var extensions = _applicationConfigurationService.GetAuthorizedExtension();
            var files =  extensions.AsParallel().SelectMany(searchPattern  => directory.EnumerateFiles(searchPattern, SearchOption.AllDirectories)).ToList();
            var libraryFiles = files.ToDictionary(file => file.Name, file => file.FullName);

            if (_listOfLibraryFiles.ContainsKey(id))
            {
                _listOfLibraryFiles[id] = libraryFiles;
            }
            else
            {
                _listOfLibraryFiles.Add(id, libraryFiles);    
            }
        }

        public string Search(string id, string fileName)
        {
            Scan(id);
            return _listOfLibraryFiles[id].TryGetValue(fileName, out var fullPath) ? fullPath : null;
        }
        
    }

    public interface ILibraryService
    {
        public List<Library> Get();

        public Library Get(string id);

        public Library Create(Library libraryIn);
        
        public void Update(string id, Library libraryIn);

        public void Remove(Library libraryIn);

        public string GetLibraryPath(string id, LibraryService.PathType type);

        public string Search(string id, string fileName);
        
    }
}