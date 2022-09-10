using MyComicsManagerApi.Models;
using MyComicsManager.Model.Shared;
using MongoDB.Driver;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using MyComicsManagerApi.Exceptions;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class ImportService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComicService>();
        
        private readonly IMongoCollection<Comic> _comics;
        private readonly ComicService _comicService;
        private readonly ComicFileService _comicFileService;
        private readonly NotificationService _notificationService;

        public ImportService(IDatabaseSettings settings,
            ComicFileService comicFileService, NotificationService notificationService, ComicService comicService)
        {
            Log.Here().Debug("settings = {Settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
            _comicFileService = comicFileService;
            _notificationService = notificationService;
            _comicService = comicService;
        }
        
        [AutomaticRetry(Attempts = 0)]
        public async Task<Comic> Import(Comic comic)
        {
            // Mise à jour des données du comic (utile dans le cas d'un retry de job...)
            comic = _comicService.Get(comic.Id);
            if (comic == null)
            {
                throw new ComicImportException("Comic is null");
            }
            
            Log.Here().Information("############# Import d'un nouveau comic #############");
            Log.Here().Information("Traitement du fichier : {File}", comic.EbookPath);

            try
            {
                comic = await ConvertToCbz(comic);
                comic = await UpdateFromComicInfo(comic);
                comic = await SetComicPageCount(comic);
                comic = await ExtractCoverImage(comic);
            }
            catch (Exception e)
            {
                Log.Here().Error(e,"Erreur lors de l'import {File}", comic.EbookPath);
                await ManageImportError(comic, e);
                throw;
            }
            
            return await SetImportStatus(comic, ImportStatus.IMPORTED, true);
            
        }
        
        private async Task<Comic> ConvertToCbz(Comic comic)
        {
            // Vérification du statut d'import
            if (comic.ImportStatus >= ImportStatus.MOVED_TO_LIB)
            {
                return comic;
            }
            
            try
            {
                _comicFileService.ConvertComicFileToCbz(comic);
                _comicFileService.MoveInLib(comic);
                
                // A partir de ce point, EbookPath doit être le chemin relatif par rapport à la librairie
                comic.EbookPath = comic.EbookName;
                Log.Here().Information("comic.EbookPath = {EbookPath}", comic.EbookPath);
            }
            catch (Exception e)
            {
                Log.Here().Error(e, "Erreur lors de la conversion en CBZ");
                await ManageImportError(comic, e);
                throw new ComicImportException("Erreur lors de la conversion en CBZ");
            }
            
            // Mise à jour du statut et du comic
            return await SetImportStatus(comic, ImportStatus.MOVED_TO_LIB, false);
        }
        
        private async Task<Comic> UpdateFromComicInfo(Comic comic)
        {
            // Vérification du statut d'import
            if (comic.ImportStatus >= ImportStatus.COMICINFO_ADDED)
            {
                return comic;
            }

            // Récupération des données du fichier ComicInfo.xml si il existe
            if (_comicFileService.HasComicInfoInComicFile(comic))
            {
                comic = _comicFileService.ExtractDataFromComicInfo(comic);
            }
            
            // Mise à jour du statut et du comic
            return await SetImportStatus(comic, ImportStatus.COMICINFO_ADDED, true);
        }

        private async Task<Comic> SetComicPageCount(Comic comic)
        {
            if (comic.ImportStatus >= ImportStatus.MOVED_TO_LIB)
            {
                return comic;
            }
            
            // Calcul du nombre d'images dans le fichier CBZ
            comic.PageCount = _comicFileService.GetNumberOfImagesInCbz(comic);
            
            // Mise à jour du statut et du comic
            return await SetImportStatus(comic, ImportStatus.NB_IMAGES_SET, true);
        }
        
        private async Task<Comic> ExtractCoverImage(Comic comic)
        {
            if (comic.ImportStatus >= ImportStatus.COVER_GENERATED)
            {
                return comic;
            }
            
            // Extraction de l'image de couverture après enregistrement car nommé avec l'id du comic
            try
            {
                _comicFileService.SetAndExtractCoverImage(comic);
            }
            catch
            {
                Log.Here().Error("Erreur lors de l'extraction de l'image de couverture");
                throw;
            }
            
            // Mise à jour du statut et du comic
            return await SetImportStatus(comic, ImportStatus.COVER_GENERATED, true);
        }

        private async Task ConvertImagesToWebP(Comic comic)
        {
            try
            {
                if (comic.WebPFormated)
                {
                    return;
                }
                _comicFileService.ConvertImagesToWebP(comic);
                comic.WebPFormated = true;
                _comicService.Update(comic.Id, comic, false);
                await _notificationService.SendNotificationMessage(comic, "Conversion WebP terminée");
            }
            catch (Exception e)
            {
                Log.Here().Warning("La conversion des images en WebP a échoué : {Exception}", e.Message);
                await ManageImportError(comic, e);
                await _notificationService.SendNotificationMessage(comic, "La conversion des images en WebP a échoué");
            }
        }
        
        public async Task<Comic> ResetImportStatus(Comic comic)
        {
            if (comic.ImportStatus == ImportStatus.IMPORTED)
            {
                return comic;
            }

            comic.ImportErrorMessage = "";
            await SetImportStatus(comic, ImportStatus.CREATED, false);
            return comic;
        }
        
        private async Task<Comic> SetImportStatus(Comic comic, ImportStatus status, bool addComicInfo)
        {
            comic.ImportStatus = status;
            _comicService.Update(comic.Id, comic, addComicInfo);
            
            Log.Here().Information("comic.ImportStatus = {Status}", comic.ImportStatus);
            await _notificationService.SendNotificationImportStatus(comic, status);
            return comic;
        }
        
        private async Task<Comic> ManageImportError(Comic comic, Exception error)
        {
            comic.ImportStatus = ImportStatus.ERROR;
            comic.ImportErrorMessage = error.Message + " : " + Environment.NewLine + error.StackTrace;
            _comicService.Update(comic.Id, comic, false);
            
            Log.Here().Information("comic.ImportStatus = {Status}", comic.ImportStatus);
            await _notificationService.SendNotificationImportStatus(comic, comic.ImportStatus);
            return comic;
        }

        
    }
}
