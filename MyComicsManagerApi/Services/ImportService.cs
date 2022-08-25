using MyComicsManagerApi.Models;
using MyComicsManager.Model.Shared;
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
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Exceptions;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class ImportService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComicService>();
        
        private readonly IMongoCollection<Comic> _comics;
        private readonly ComicService _comicService;
        private readonly LibraryService _libraryService;
        private readonly ComicFileService _comicFileService;
        private readonly NotificationService _notificationService;

        public ImportService(IDatabaseSettings settings, LibraryService libraryService,
            ComicFileService comicFileService, NotificationService notificationService, ComicService comicService)
        {
            Log.Here().Debug("settings = {Settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
            _libraryService = libraryService;
            _comicFileService = comicFileService;
            _notificationService = notificationService;
            _comicService = comicService;
            
            // https://www.freeformatter.com/cron-expression-generator-quartz.html
            // Remarque : Supprimer l'étoile des années car ne semble par gérer par HangFire
            // At second :00, at minute :00, every hour starting at 00am, of every day
            RecurringJob.AddOrUpdate("convertToWebP", () => ConvertComicsToWebP(), "0 0 0/1 ? * *");
        }
        
        public async Task<Comic> Import(Comic comic)
        {
            
            Log.Here().Information("############# Import d'un nouveau comic #############", comic.EbookPath);
            Log.Here().Information("Traitement du fichier : {File}", comic.EbookPath);
            
            comic = await ConvertToCbz(comic);
            if (comic == null)
            {
                return null;
            }
            
            // Récupération des données du fichier ComicInfo.xml si il existe
            if (_comicFileService.HasComicInfoInComicFile(comic))
            {
                comic = _comicFileService.ExtractDataFromComicInfo(comic);
                try
                {
                    _comicService.Update(comic.Id, comic);
                }
                catch (Exception)
                {
                    Log.Here().Error("Erreur lors de la mise à jour de l'arborescence du fichier");
                    await SetImportStatus(comic, ImportStatus.ERROR);
                    return null;
                }
            }

            // Calcul du nombre d'images dans le fichier CBZ
            _comicFileService.SetNumberOfImagesInCbz(comic);
            comic = await SetImportStatus(comic, ImportStatus.NB_IMAGES_SET);

            // Extraction de l'image de couverture après enregistrement car nommé avec l'id du comic       
            _comicFileService.SetAndExtractCoverImage(comic); 
            comic = await SetImportStatus(comic, ImportStatus.COVER_GENERATED); 
            
            comic = await SetImportStatus(comic, ImportStatus.IMPORTED);
            
            return comic;
        }
        
        private async Task<Comic> ConvertToCbz(Comic comic)
        {
            try
            {
                _comicFileService.ConvertComicFileToCbz(comic);
            }
            catch (Exception e)
            {
                Log.Here().Error(e, "Erreur lors de la conversion en CBZ");
                await SetImportStatus(comic, ImportStatus.ERROR);
                return null;
            }

            // Mise à jour du statut en base de données
            comic = await SetImportStatus(comic, ImportStatus.CBZ_CONVERTED);
            
            // Déplacement du fichier vers la racine de la librairie sélectionnée
            var destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH) +
                              comic.EbookName;

            // Gestion du cas où le fichier uploadé existe déjà dans la lib
            while (File.Exists(destination))
            {
                Log.Here().Warning("Le fichier {File} existe déjà", destination);
                comic.Title = Path.GetFileNameWithoutExtension(destination) + "-Rename";
                Log.Here().Information("comic.Title = {Title}", comic.Title);
                comic.EbookName = comic.Title + Path.GetExtension(destination);
                Log.Here().Information("comic.EbookName = {EbookName}", comic.EbookName);
                destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH) +
                              comic.EbookName;
            }

            try
            {
                _comicFileService.MoveComic(comic.EbookPath, destination);
            }
            catch (Exception)
            {
                Log.Here().Error("Erreur lors du déplacement du fichier {File} vers {Destination}", comic.EbookPath, destination);
                await SetImportStatus(comic, ImportStatus.ERROR);
                return null;
            }

            // A partir de ce point, EbookPath doit être le chemin relatif par rapport à la librairie
            comic.EbookPath = comic.EbookName;
            Log.Here().Information("comic.EbookPath = {EbookPath}", comic.EbookPath);
            
            return await SetImportStatus(comic, ImportStatus.MOVED_TO_LIB);
        }

        

        public async Task ConvertImagesToWebP(Comic comic)
        {
            try
            {
                if (comic.WebPFormated)
                {
                    return;
                }
                _comicFileService.ConvertImagesToWebP(comic);
                comic.WebPFormated = true;
                _comicService.Update(comic.Id, comic);
                await _notificationService.SendNotificationMessage(comic, "Conversion WebP terminée");
            }
            catch (Exception e)
            {
                Log.Here().Warning("La conversion des images en WebP a échoué : {Exception}", e.Message);
                await SetImportStatus(comic, ImportStatus.ERROR);
                await _notificationService.SendNotificationMessage(comic, "La conversion des images en WebP a échoué");
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public async Task ConvertComicsToWebP()
        {
            var comics = _comicService.ListComicNotWebpConverted().Take(1);

            var monitoringApi = JobStorage.Current.GetMonitoringApi();

            if (monitoringApi.ProcessingCount() != 0)
            {
                return;
            }
            
            await ConvertImagesToWebP(comics.First());
        }

        public void DeleteDotFiles()
        {
            var list = _comics.Find(comic => true).ToList();
            foreach (var comic in list)
            {
                _comicFileService.DeleteFilesBeginningWithDots(comic);
            }
        }
        
        private async Task<Comic> SetImportStatus(Comic comic, ImportStatus status)
        {
            comic.ImportStatus = status;
            comic.ImportMessage = $"comic.ImportStatus = {status}";
            _comicService.Update(comic.Id, comic);
            
            Log.Here().Information("comic.ImportStatus = {Status}", comic.ImportStatus);
            await _notificationService.SendNotificationImportStatus(comic, status);
            return comic;
        }

        
    }
}
