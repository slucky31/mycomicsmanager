using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using MyComicsManager.Model.Shared.Models;
using MyComicsManager.Model.Shared.Services;
using MyComicsManagerApi.Exceptions;
using MyComicsManagerApi.Settings;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class ImportService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComicService>();
        
        private readonly ComicService _comicService;
        private readonly ComicFileService _comicFileService;
        private readonly NotificationService _notificationService;
        private readonly ApplicationConfigurationService _applicationConfigurationService;
        private readonly LibraryService _libraryService;

        public ImportService(IDatabaseSettings settings,
            ComicFileService comicFileService, NotificationService notificationService, ComicService comicService, ApplicationConfigurationService applicationConfigurationService, LibraryService libraryService)
        {
            Log.Here().Debug("settings = {Settings}", settings);
            _comicFileService = comicFileService;
            _notificationService = notificationService;
            _comicService = comicService;
            _applicationConfigurationService = applicationConfigurationService;
            _libraryService = libraryService;
        }
        
        public List<Comic> GetUploadedFiles()
        {
            var fileUploadDirRootPath = _applicationConfigurationService.GetPathFileImport();
            var extensions = _applicationConfigurationService.GetAuthorizedExtension();
            
            Log.Information("Recherche des fichiers dans {Path}", fileUploadDirRootPath);
            
            // Création du répertoire si il n'existe pas
            Directory.CreateDirectory(fileUploadDirRootPath);

            // Lister les fichiers
            var directory = new DirectoryInfo(fileUploadDirRootPath);        
            var files =  extensions.AsParallel().SelectMany(searchPattern  => directory.EnumerateFiles(searchPattern, SearchOption.AllDirectories)).ToList();
            
            var comics = new List<Comic>();
            foreach (var file in files)
            {
                Log.Information("Fichier trouvé {File}", file.FullName);
                var comic = new Comic
                {
                    EbookName = file.Name,
                    EbookPath = file.FullName,
                    Size = file.Length
                };
                
                // Les fichiers en erreurs ne sont pas considérés
                if (!file.FullName.Contains(Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar))
                {
                    comics.Add(comic);    
                }
            }
            
            return comics;
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
                comic = _comicFileService.Move(comic, _applicationConfigurationService.GetPathImportErrors());
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
                throw;
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
            if (comic.ImportStatus >= ImportStatus.NB_IMAGES_SET)
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

        [AutomaticRetry(Attempts = 0)]
        public async Task ResetImportStatus(Comic comic)
        {
            if (comic.ImportStatus == ImportStatus.IMPORTED)
            {
                return;
            }
            
            Log.Here().Information("Recherche du fichier dans la librairie");
            var path = _libraryService.Search(comic.LibraryId, Path.GetFileName(comic.EbookPath));
            if (comic.EbookPath != null)
            {
                Log.Here().Information("Le fichier recherché a été trouvé dans la bibliothèque : {File}", path);
                Log.Here().Information("Reset ImportMessage et ImportStatus : CREATED");
                comic.ImportErrorMessage = "";
                comic.EbookPath = path;
                comic = await SetImportStatus(comic, ImportStatus.CREATED, false);
                comic = _comicFileService.Move(comic, _applicationConfigurationService.GetPathFileImport());
                _comicService.Remove(comic);
                return;
            }
            
            Log.Here().Warning("Le fichier recherché est introuvable : {File}", Path.GetFileName(comic.EbookPath));
            comic.ImportErrorMessage = "Le fichier recherché est introuvable";
            await SetImportStatus(comic, ImportStatus.ERROR, false);
        }
        
        private async Task<Comic> SetImportStatus(Comic comic, ImportStatus status, bool addComicInfo)
        {
            comic.ImportStatus = status;
            _comicService.Update(comic.Id, comic, addComicInfo);
            
            Log.Here().Information("comic.ImportStatus = {Status}", comic.ImportStatus);
            await _notificationService.SendNotificationImportStatus(comic, status);
            return comic;
        }
        
        private async Task ManageImportError(Comic comic, Exception error)
        {
            comic.ImportStatus = ImportStatus.ERROR;
            comic.ImportErrorMessage = error.Message + " : " + Environment.NewLine + error.StackTrace;
            _comicService.Update(comic.Id, comic, false);
            
            Log.Here().Information("comic.ImportStatus = {Status}", comic.ImportStatus);
            await _notificationService.SendNotificationImportStatus(comic, comic.ImportStatus);
        }

        
    }
}
