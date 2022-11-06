using MongoDB.Driver;
using System.Collections.Generic;
using Serilog;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using MongoDB.Bson;
using MyComicsManager.Model.Shared.Models;
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Settings;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class ComicService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComicService>();
        
        private readonly IMongoCollection<Comic> _comics;
        private readonly ComicFileService _comicFileService;
        private readonly NotificationService _notificationService;

        private const int MaxComicsPerRequest = 100;

        public ComicService(IDatabaseSettings settings,
            ComicFileService comicFileService, NotificationService notificationService)
        {
            Log.Here().Debug("settings = {Settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
            _comicFileService = comicFileService;
            _notificationService = notificationService;

        }
        
        public List<Comic> Get() =>
            _comics.Find(comic => true).ToList();
        
        public List<Comic> GetOrderByLastAddedLimitBy(int limit) =>
            _comics.Find(comic => comic.ImportStatus == ImportStatus.IMPORTED).SortByDescending(comic => comic.Added)
                .Limit(limit < MaxComicsPerRequest ? limit : MaxComicsPerRequest).ToList();

        public List<Comic> GetOrderBySerieAndTome(int limit) =>
                    _comics.Find(comic => comic.ImportStatus == ImportStatus.IMPORTED)
                        .SortBy(comic => comic.Serie)
                        .ThenBy(comic => comic.Volume)
                        .Limit(limit < MaxComicsPerRequest ? limit : MaxComicsPerRequest).ToList();
        
        public List<Comic> GetWithoutIsbnLimitBy(int limit) =>
            _comics.Find(comic => comic.ImportStatus == ImportStatus.IMPORTED && string.IsNullOrEmpty(comic.Isbn)).SortBy(comic => comic.Added)
                .Limit(limit < MaxComicsPerRequest ? limit : MaxComicsPerRequest).ToList();

        public List<Comic> GetRandomLimitBy(int limit)
        {
            var list = _comics.Find(comic => comic.ImportStatus == ImportStatus.IMPORTED).ToList();
            return list.OrderBy(_ => Guid.NewGuid())
                .Take(limit < MaxComicsPerRequest ? limit : MaxComicsPerRequest).ToList();
        }
        
        public Comic Get(string id) =>
            _comics.Find(comic => comic.Id == id).FirstOrDefault();
        
        public Comic Create(Comic comic)
        {
            // Note du développeur : 
            // EbookPath est en absolu au début du traitement pour localiser le fichier dans le répertoire d'upload
            if (comic.EbookName == null || comic.EbookPath == null)
            {
                Log.Here().Error("Une des valeurs suivantes est null et ne devrait pas l'être");
                Log.Here().Error("EbookName : {Value}", comic.EbookName);
                Log.Here().Error("EbookPath : {Value}", comic.EbookPath);
                return null;
            }
            
            // Insertion en base de données
            comic.Title = Path.GetFileNameWithoutExtension(comic.EbookName);
            comic.ImportStatus = ImportStatus.CREATED;
            comic.CoverType = CoverType.PORTRAIT;
            comic.Added = DateTime.Now;
            comic.Edited = comic.Added;
            _comics.InsertOne(comic);

            return comic;
        }

        public void Update(string id, Comic comic, bool addComicInfo)
        {
            Log.Here().Information("Mise à jour du comic {Comic}", id.Replace(Environment.NewLine, ""));
            
            // Mise à jour du nom du fichier et du chemin si titre et série ont été modifiés
            UpdateDirectoryAndFileName(comic);

            // Mise à jour de la date de dernière modification
            comic.Edited = DateTime.Now;
            
            // Mise à jour du fichier ComicInfo.xml
            if (addComicInfo)
            {
                _comicFileService.AddComicInfoInComicFile(comic);
            }
            
            // Mise à jour en base de données
            _comics.ReplaceOne(c => c.Id == id, comic);
        }
        
        private void UpdateDirectoryAndFileName(Comic comic)
        {
            // Mise à jour du nom du fichier
            if (!string.IsNullOrEmpty(comic.Serie) && comic.Volume > 0)
            {
                // Mise à jour du nom du fichier pour le calcul de la destination
                if (comic.Serie == "One shot")
                {
                    comic.EbookName = comic.Title + ".cbz";
                }
                else
                {
                    comic.EbookName = comic.Serie.ToPascalCase() + "_T" + comic.Volume.ToString("000") + ".cbz";
                }
                
                var comicEbookPath = Path.GetDirectoryName(comic.EbookPath) + Path.DirectorySeparatorChar;

                // Renommage du fichier (si le fichier existe déjà, on ne fait rien, car il est déjà présent !)
                comic = _comicFileService.MoveToLib(comic, comicEbookPath);
            }

            // Mise à jour de l'arborescence du fichier
            if (!string.IsNullOrEmpty(comic.Serie))
            {
                var eBookPath = comic.Serie.ToPascalCase() + Path.DirectorySeparatorChar;
                _comicFileService.MoveToLib(comic, eBookPath);
            }
        }
        
        public List<Comic> Find(string item, int limit)
        {
            var filterTitle = Builders<Comic>.Filter.Regex(x => x.Title, new BsonRegularExpression(item, "i"));
            var filterSerie = Builders<Comic>.Filter.Regex(x => x.Serie, new BsonRegularExpression(item, "i"));
            var filter = filterTitle|filterSerie;
            
            var list = _comics.Find(filter).ToList();
            return list.OrderBy(x => x.Serie).ThenBy(x => x.Volume).ThenBy(x => x.Title)
                .Take(limit < MaxComicsPerRequest ? limit : MaxComicsPerRequest).ToList();
        }
        
        public List<Comic> FindBySerie(string serie, int limit)
        {
            var filterSerie = Builders<Comic>.Filter.Where(comic => comic.Serie == serie);
            
            var list = _comics.Find(filterSerie).ToList();
            return list.OrderBy(x => x.Volume).ThenBy(x => x.Title)
                .Take(limit < MaxComicsPerRequest ? limit : MaxComicsPerRequest).ToList();
        }

        public async Task<PaginationComics> FindByPageOrderBySerie(string searchItem, int pageId, int pageSize)
        {
            FilterDefinition<Comic> filter;
            if (string.IsNullOrEmpty(searchItem))
            {
                filter = Builders<Comic>.Filter.Empty;
            }
            else
            {
                var filterTitle = Builders<Comic>.Filter.Regex(x => x.Title, new BsonRegularExpression(searchItem, "i"));
                var filterSerie = Builders<Comic>.Filter.Regex(x => x.Serie, new BsonRegularExpression(searchItem, "i"));
                filter = filterTitle|filterSerie;
            }
            
            // On garde uniquement les comics qui sont correctement importés
            var filterStatus = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED);
            filter &= filterStatus;
            
            var result =  await _comics.AggregateByPage(
                filter,
                Builders<Comic>.Sort.Ascending(x => x.Serie),
                page: pageId,
                pageSize: pageSize);

            return  new PaginationComics
            {
                TotalPages = result.totalPages,
                Data = result.data
            };
        }

        public void Remove(Comic comic)
        {
            Log.Here().Information("");
            
            // Suppression du fichier
            Comic comicToDelete = _comics.Find(c => (c.Id == comic.Id)).FirstOrDefault();
            if (comicToDelete != null)
            {
                // Suppression du fichier
                if (File.Exists(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH)))
                {
                    File.Delete(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH));
                }

                // Suppression de l'image de couverture
                if (File.Exists(comicToDelete.CoverPath))
                {
                    File.Delete(comicToDelete.CoverPath);
                }
                //TODO : Gestion des exceptions
            }

            // Suppression de la référence en base de données
            _comics.DeleteOne(c => c.Id == comic.Id);
        }

        public void RemoveAllComicsFromLibrary(string libId)
        {
            Log.Here().Information("Suppression de tous les comics de la bibliothèque {LibId}", libId.Replace(Environment.NewLine, ""));

            List<Comic> comics = _comics.Find(c => (c.LibraryId == libId)).ToList();
            foreach (Comic c in comics)
            {
                Remove(c);
            }
        }

        public Comic SearchComicInfo(Comic comic, bool update)
        {
            if (string.IsNullOrEmpty(comic.Isbn))
            {
                return null;
            }

            var parser = new BdphileComicHtmlDataParser();
            var results = parser.Parse(comic.Isbn);

            if (results.Count == 0)
            {
                return null;
            }

            comic.Editor = results[ComicDataEnum.EDITEUR];
            comic.Isbn = results[ComicDataEnum.ISBN];
            comic.Penciller = results[ComicDataEnum.DESSINATEUR];
            comic.Serie = results[ComicDataEnum.SERIE];
            comic.Title = results[ComicDataEnum.TITRE];
            comic.Writer = results[ComicDataEnum.SCENARISTE];
            comic.FicheUrl = results[ComicDataEnum.URL];
            comic.Colorist = results[ComicDataEnum.COLORISTE];
            comic.LanguageIso = results[ComicDataEnum.LANGAGE];
            var frCulture = new CultureInfo("fr-FR");

            const DateTimeStyles dateTimeStyles = DateTimeStyles.AssumeUniversal;
            if (DateTime.TryParseExact(results[ComicDataEnum.DATE_PARUTION], "d MMMM yyyy", frCulture,
                    dateTimeStyles, out var dateValue))
            {
                comic.Published = dateValue;
            }
            else
            {
                Log.Warning(
                    "Une erreur est apparue lors de l'analyse de la date de publication : {DatePublication}",
                    results[ComicDataEnum.DATE_PARUTION]);
            }

            if (int.TryParse(results[ComicDataEnum.TOME], out var intValue))
            {
                comic.Volume = intValue;
            }
            else
            {
                Log.Here().Warning("Une erreur est apparue lors de l'analyse du volume : {Tome}",
                    results[ComicDataEnum.TOME]);
            }

            const NumberStyles style = NumberStyles.AllowDecimalPoint;
            if (double.TryParse(results[ComicDataEnum.NOTE], style, CultureInfo.InvariantCulture,
                    out var doubleValue))
            {
                comic.Review = doubleValue;
            }
            else
            {
                Log.Here().Warning("Une erreur est apparue lors de l'analyse de la note : {Note}",
                    results[ComicDataEnum.NOTE]);
                comic.Review = -1;
            }

            if (double.TryParse(results[ComicDataEnum.PRIX].Split('€')[0], style, CultureInfo.InvariantCulture,
                    out doubleValue))
            {
                comic.Price = doubleValue;
            }
            else
            {
                Log.Here().Warning("Une erreur est apparue lors de l'analyse du prix : {Prix}",
                    results[ComicDataEnum.PRIX]);
                comic.Price = -1;
            }

            if (update)
            {
                Update(comic.Id, comic, true);    
            }
            
            return comic;
        }

        public List<Comic> GetImportingComics()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus != ImportStatus.IMPORTED);
            return _comics.Find(filter).ToList();
        }

        public List<string> GetSeries()
        {
            var filter = Builders<Comic>.Filter.Ne(comic => comic.Serie, null);
            return _comics.Distinct(comic => comic.Serie, filter).ToList();
        }

        public IEnumerable<Comic> ListComicNotWebpConverted()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED && ! comic.WebPFormated );
            return _comics.Find(filter).ToList();
        }

        

        public void DeleteDotFiles()
        {
            var list = _comics.Find(comic => true).ToList();
            foreach (var comic in list)
            {
                _comicFileService.DeleteFilesBeginningWithDots(comic);
            }
        }
        
        [AutomaticRetry(Attempts = 0)]
        public async Task RecurringJobConvertComicsToWebP()
        {
            var comic = ListComicNotWebpConverted().Take(1).First();
            
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            if (monitoringApi.ProcessingCount() != 1)
            {
                return;
            }
            
            try
            {
                if (comic.WebPFormated)
                {
                    return;
                }
                _comicFileService.ConvertImagesToWebP(comic);
                comic.WebPFormated = true;
                Update(comic.Id, comic, false);
                await _notificationService.SendNotificationMessage(comic, "Conversion en WebP terminée");
            }
            catch (Exception e)
            {
                Log.Here().Warning("La conversion des images en WebP a échoué : {Exception}", e.Message);
                
                await _notificationService.SendNotificationMessage(comic, "La conversion des images en WebP a échoué");
            }
        }
        
    }
}
