﻿using MyComicsManagerApi.Models;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using MyComicsManager.Model.Shared.Models;
using MyComicsManager.Model.Shared.Services;
using MyComicsManagerApi.Exceptions;
using MyComicsManagerApi.Utils;
using Serilog;
using SharpCompress.Archives.Rar;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using UglyToad.PdfPig;

namespace MyComicsManagerApi.Services
{
    public class ComicFileService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComicFileService>();
        
        private readonly ILibraryService _libraryService;
        private readonly ComputerVisionService _computerVisionService;
        private readonly ApplicationConfigurationService _applicationConfigurationService;

        private readonly string[] _extensionsFileArchive = {".jpeg", ".jpg", ".png", ".gif", ".webp", ".xml"};
        private readonly string[] _extensionsImageArchive = {".jpeg", ".jpg", ".png", ".gif", ".webp"};
        private readonly string[] _extensionsImageArchiveWithoutWebp = {".jpeg", ".jpg", ".png", ".gif"};

        private const int ResizedWidth = 1400;

        public ComicFileService(ILibraryService libraryService, ComputerVisionService computerVisionService, ApplicationConfigurationService applicationConfigurationService)
        {
            _libraryService = libraryService;
            _computerVisionService = computerVisionService;
            _applicationConfigurationService = applicationConfigurationService;
        }

        public void SetAndExtractCoverImage(Comic comic)
        {
            // Normalizes the path.
            string extractPath = Path.GetFullPath(_applicationConfigurationService.GetPathCovers());

            try
            {
                // Update comic with file
                comic.CoverPath = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, 0));    
            }
            catch (ComicIoException e)
            {
                Log.Here().Error(e, "Le format de l'image n'est pas supporté");
                throw;
            }
        }

        public List<string> ExtractFirstImages(Comic comic, int nbImagesToExtract)
        {
            // Normalizes the path.
            var extractPath = Path.GetFullPath(_applicationConfigurationService.GetPathIsbn());
            Directory.CreateDirectory(extractPath);

            List<string> firstImages = new();
            for (int i = 0; i < nbImagesToExtract; i++)
            {
                string fileName = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, i));
                firstImages.Add(fileName);
            }

            return firstImages;
        }

        public List<string> ExtractLastImages(Comic comic, int nbImagesToExtract)
        {
            // Normalizes the path.
            var extractPath = Path.GetFullPath(_applicationConfigurationService.GetPathIsbn());
            Directory.CreateDirectory(extractPath);

            var lastImages = new List<string>();

            for (var i = comic.PageCount - nbImagesToExtract; i < comic.PageCount; i++)
            {
                var fileName = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, i));
                lastImages.Add(fileName);
            }

            return lastImages;
        }

        // https://docs.microsoft.com/fr-fr/dotnet/standard/io/how-to-compress-and-extract-files
        private string ExtractImageFromCbz(Comic comic, string extractPath, int imageIndex)
        {
            Log.Here().Information("Les fichiers seront extraits dans {Path}", extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                extractPath += Path.DirectorySeparatorChar;
            }

            var zipPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            using var archive = ZipFile.OpenRead(zipPath);
            if (imageIndex < 0 || imageIndex >= archive.Entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(imageIndex),
                    "imageIndex (" + imageIndex + ") doit être compris entre 0 et " + archive.Entries.Count + ".");
            }

            var images = archive.Entries
                .Where(file =>
                    _extensionsImageArchive.Any(x => file.FullName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.FullName);

            ZipArchiveEntry entry = images.ElementAt(imageIndex);
            Log.Here().Information("Fichier à extraire {FileName}", entry.FullName);
            var destinationPath = Path.GetFullPath(Path.Combine(extractPath,
                comic.Id + "-" + imageIndex + Path.GetExtension(entry.FullName)));
            Log.Here().Information("Destination {Destination}", destinationPath);

            if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
            {
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                    // TODO : Supprimer toutes les images dans le cache !!!
                }

                entry.ExtractToFile(destinationPath);
                // TODO : Créer une image plus petite

                // Resize de l'image
                try
                {
                    using var image = Image.Load(destinationPath, out _);
                    Rectangle rectangle;
                    switch (comic.CoverType)
                    {
                        case CoverType.LANDSCAPE_LEFT:
                            rectangle = new Rectangle(0, 0, image.Width / 2, image.Height);
                            image.Mutate(context => context.Crop(rectangle));
                            image.Save(destinationPath);
                            break;
                        case CoverType.LANDSCAPE_RIGHT:
                            rectangle = new Rectangle(image.Width / 2, 0, image.Width / 2, image.Height);
                            image.Mutate(context => context.Crop(rectangle));
                            image.Save(destinationPath);
                            break;
                        case CoverType.PORTRAIT:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    Log.Here().Error(e, "Le format de l'image n'est pas supporté");
                    throw new ComicIoException("Le format de l'image n'est pas supporté", e);
                }
            }
            else
            {
                throw new SecurityException("Destination path is not within the extraction path.");
            }
            return destinationPath;
        }

        public void ConvertComicFileToCbz(Comic comic)
        {
            Log.Here().Information("Comic : {Path}",comic.EbookPath);
            var tempDir = CreateTempDirectory();

            // Extraction des images
            Log.Here().Information("Extraction des images");
            var extension = GetArchiveType(comic);
            switch (extension)
            {
                case ".cbz":
                case ".zip":
                    Log.Here().Information("ExtractImagesFromCbz");
                    try
                    {
                        ExtractImagesFromCbz(comic, tempDir);
                    }
                    catch (Exception)
                    {
                        Log.Here().Error("Erreur lors de l'extraction des images à partir du fichier CBZ {File}",
                            comic.EbookPath);
                        throw;
                    }
                    break;

                case ".pdf":
                    Log.Here().Information("ExtractImagesFromCbz");
                    ExtractImagesFromPdf(comic.EbookPath, tempDir);
                    break;

                case ".cbr":
                case ".rar":
                    Log.Here().Information("ExtractImagesFromCbz");
                    ExtractImagesFromCbr(comic.EbookPath, tempDir);
                    break;

                default:
                    // TODO : Faudrait faire quelque chose là, non ?
                    Log.Here().Error("L'extension de ce fichier n'est pas pris en compte : {Extension}", extension);
                    return;
            }

            // Suppression du fichier origine
            if (File.Exists(comic.EbookPath))
            {
                if (comic.EbookPath != null)
                {
                    Log.Here().Information("Suppression du fichier origine {File}", comic.EbookPath);
                    File.Delete(comic.EbookPath);
                }
            }
            
            // Changement de l'extension du fichier de destination
            // Nettoyage du nom de destination : Suppression des accents, (), {}, [], ...
            comic.EbookPath = Path.GetDirectoryName(comic.EbookPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(comic.EbookPath).ToCamlCase() + ".cbz";
            
            Log.Here().Information("comic.EbookPath = {Path}", comic.EbookPath);

            // Déplacement des images au même niveau dans un répertoire archive
            Log.Here().Information("Création du répertoire d'archive");
            var extractedFiles = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories).ToList();
            var archiveDirectoryPath = Path.Combine(tempDir, "archive");
            Directory.CreateDirectory(archiveDirectoryPath);
            
            Log.Here().Information("Déplacement des {NbFiles} fichiers dans le répertoire d'archive", extractedFiles.Count);
            foreach (var file in extractedFiles)
            {
                // Passage du nom du fichier en PascalCase et de l'extension en minuscule
                var destFile = Path.GetFileNameWithoutExtension(file) + Path.GetExtension(file).ToLower();
                var destination = Path.Combine(archiveDirectoryPath, destFile);
                
                // Déplacement du fichier dans le repertoire archive
                File.Move(file, destination, true);
            }
            
            // Nettoyage des fichiers commençant par .
            var files = Directory.EnumerateFiles(archiveDirectoryPath, ".*", SearchOption.AllDirectories).ToList();
            Log.Here().Information("Suppression des {NbFiles} fichiers commençant par un point", files.Count);
            foreach (var file in files)
            {
                Log.Here().Debug("Suppression du fichier {File}", file);
                File.Delete(file);
            }                       

            if (comic.EbookPath != null)
            {
                // Création de l'archive à partir du répertoire
                // https://khalidabuhakmeh.com/create-a-zip-file-with-dotnet-5
                // https://stackoverflow.com/questions/163162/can-you-call-directory-getfiles-with-multiple-filters
                
                var filesToArchive = Directory.EnumerateFiles(archiveDirectoryPath, "*.*", SearchOption.AllDirectories)
                    .Where(file =>
                        _extensionsFileArchive.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToList();
                Log.Here().Information("Archivages des {NbFiles} fichiers", filesToArchive.Count);
                
                // Construction de l'archive
                using var archive = ZipFile.Open(comic.EbookPath, ZipArchiveMode.Create);
                foreach (var file in filesToArchive)
                {
                    var entry = archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                    Log.Here().Debug("{FullName} was compressed", entry.FullName);
                }
            }
            
            // Suppression du dossier temporaire
            CleanTempDirectory(tempDir);

            // Mise à jour de l'objet Comic avec le nouveau fichier CBZ et le nouveau chemin
            comic.EbookName = Path.GetFileName(comic.EbookPath);
        }
        


        public void ConvertImagesToWebP(Comic comic)
        {
            Log.Here().Information("Comic : {Path}",comic.EbookPath);
            
            if (comic.EbookPath == null)
            {
                Log.Here().Warning("Il n'y a pas d'archive à convertir");
                throw new ComicIoException("Erreur lors de l'extraction de l'archive. Consulter le répertoire errors.");
            }
            
            var zipPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            if (!File.Exists(zipPath))
            {
                Log.Here().Warning("L'archive n'existe pas");
                throw new ComicIoException("Erreur lors de l'extraction de l'archive. Consulter le répertoire errors.");
            }
            
            var tempDir = CreateTempDirectory();
            try
            {
                ExtractImagesFromCbz(comic, tempDir);
            }
            catch (Exception)
            {
                Log.Here().Error("Erreur lors de l'extraction des images à partir du fichier CBZ {File}",
                    comic.EbookPath);
                // Suppression du dossier temporaire
                CleanTempDirectory(tempDir);
                throw;
            }
            
            var filesToConvert = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                    _extensionsImageArchiveWithoutWebp.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToList();
            Log.Here().Information("Conversion des {NbFiles} images en WebP et resize à {Width} pixels de large",
                filesToConvert.Count, ResizedWidth);

            var index = 1;
            foreach(var file in filesToConvert)
            {
                Log.Here().Information("[{Index}/{NbFiles}] Conversion du fichier {File}", index++,filesToConvert.Count, file);
                var webpConvertedFile = Path.ChangeExtension(file, ".webp");
                using (var image = Image.Load(file, out _))
                {
                    try
                    {
                        // Resize de l'image
                        if (image.Width > ResizedWidth)
                        {
                            if (image.Width > image.Height)
                            {
                                // Cas d'une double page
                                image.Mutate(x => x.Resize(2 * ResizedWidth, 0));
                            }
                            else
                            {
                                // Cas d'une page simple
                                image.Mutate(x => x.Resize(ResizedWidth, 0));
                            }
                        }

                        // Conversion en WebP
                        image.SaveAsWebp(webpConvertedFile, new WebpEncoder {FileFormat = WebpFileFormatType.Lossy});
                        Log.Here().Debug("Image {Image} was converted into WebP {WebpImage}", file, webpConvertedFile);
                    }
                    catch (Exception)
                    {
                        Log.Here().Error("Erreur lors de l'extraction de l'image {Image} à partir du fichier CBZ {File}",
                            file, comic.EbookPath);
                        // Suppression du dossier temporaire
                        CleanTempDirectory(tempDir);
                        throw;
                    }
                }

                // Suppression du fichier original
                File.Delete(file);
            }
            
            // Renommage de l'archive pour pouvoir construire la nouvelle archive
            var destBackUp = Path.ChangeExtension(zipPath, ".old");
            if (File.Exists(zipPath))
            {
                File.Delete(destBackUp);
            }
            Log.Here().Information("Renommage du fichier origine {File} en {Dest}", comic.EbookPath, destBackUp);
            File.Move(zipPath,  destBackUp);
            
            // Création de l'archive à partir du répertoire
            // https://khalidabuhakmeh.com/create-a-zip-file-with-dotnet-5
            // https://stackoverflow.com/questions/163162/can-you-call-directory-getfiles-with-multiple-filters
            
            var filesToArchive = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                    _extensionsFileArchive.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToList();
            Log.Here().Information("Archivages des {NbFiles} fichiers", filesToArchive.Count);
            
            // Construction de l'archive
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var file in filesToArchive)
            {
                var entry = archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                Log.Here().Debug("{FullName} was compressed", entry.FullName);
            }
            
            // Suppression du dossier temporaire
            CleanTempDirectory(tempDir);
            
            // Suppression de l'archive backup
            File.Delete(destBackUp);
            Log.Here().Information("Suppression du fichier origine {Dest}", destBackUp);
            
        }

        private void ExtractImagesFromCbz(Comic comic, string tempDir)
        {
            try
            {
                var comicEbookPath = GetComicFileAbsolutePath(comic);
                Log.Here().Information("Absolute comic File Path : {Path}",comicEbookPath);
                
                ZipFile.ExtractToDirectory(comicEbookPath, tempDir, overwriteFiles: true);
            }
            catch (Exception e)
            {
                Log.Here().Error("Erreur lors de l'extraction de l'archive {Archive}", comic.EbookPath);
                throw new ComicIoException("Erreur lors de l'extraction de l'archive", e);
            }
        }

        private void ExtractImagesFromPdf(string comicEbookPath, string tempDir)
        {
            var document = PdfDocument.Open(comicEbookPath);
            foreach (var page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    if (!image.TryGetBytes(out _))
                    {
                        IReadOnlyList<byte> b = image.RawBytes;
                        string imageName = Path.Combine(tempDir, "P" + page.Number.ToString("D5") + ".jpg");
                        File.WriteAllBytes(imageName, b.ToArray());
                        Log.Here().Debug("Image with {Size} bytes on page {Page}. Location: {Image}", b.Count,
                            page.Number, imageName);
                    }
                }
            }
        }

        private static void ExtractImagesFromCbr(string comicEbookPath, string tempDir)
        {
            using Stream stream = File.OpenRead(comicEbookPath);
            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                // au cas où : https://docs.microsoft.com/fr-fr/dotnet/csharp/language-reference/keywords/continue
                if (reader.Entry.IsDirectory)
                {
                    continue;
                }

                Log.Here().Debug("Key : {Key}", reader.Entry.Key);

                reader.WriteEntryToDirectory(tempDir, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
        }

        public int GetNumberOfImagesInCbz(Comic comic)
        {
            var zipPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            using var archive = ZipFile.OpenRead(zipPath);
            var images = archive.Entries.Where(file =>
                _extensionsImageArchive.Any(x => file.FullName.EndsWith(x, StringComparison.OrdinalIgnoreCase)));
            return images.Count();
        }

        public async Task<List<string>> ExtractIsbnFromCbz(Comic comic, int imageIndex)
        {
            var tempDir = CreateTempDirectory();
            Log.Here().Information("tempDir : {Dir}", tempDir);

            var imagePath = ExtractImageFromCbz(comic, tempDir, imageIndex);
            Log.Here().Information("imagePath : {Path}", imagePath);

            var extractedText = await _computerVisionService.ReadTextFromLocalImage(imagePath);
            Log.Here().Information("extractedText : {Text}", extractedText);

            // Reference : https://regexlib.com/Search.aspx?k=isbn
            const string isbnPattern = "(ISBN[-]*(1[03])*[ ]*(: ){0,1})*(([0-9Xx][- ]*){13}|([0-9Xx][- ]*){10})";
            var rgx = new Regex(isbnPattern);

            var isbnList = new List<string>();
            foreach (Match match in rgx.Matches(extractedText))
            {
                isbnList.Add(match.Value);
            }
            
            // Suppression du dossier temporaire
            CleanTempDirectory(tempDir);

            return isbnList;
        }

        public async Task<string> ExtractTitleFromCbz(Comic comic)
        {
            var tempDir = CreateTempDirectory();
            Log.Here().Information("tempDir : {Dir}", tempDir);

            // Extraction des infos de la page de couverture
            var imagePath = ExtractImageFromCbz(comic, tempDir, 0);
            Log.Here().Information("imagePath : {Path}", imagePath);

            var extractedText = await _computerVisionService.ReadTextFromLocalImage(imagePath);
            Log.Here().Information("extractedText : {Text}", extractedText);
            
            // Suppression du dossier temporaire
            CleanTempDirectory(tempDir);

            return extractedText;
        }

        public bool HasComicInfoInComicFile(Comic comic)
        {
            var comicEbookPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);

            using var zipToOpen = new FileStream(comicEbookPath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);
            var entry = archive.GetEntry("ComicInfo.xml");
            return (entry != null);
        }
        
        public void DeleteFilesBeginningWithDots(Comic comic)
        {
            Log.Here().Information("Traitement du fichier : {Path}", comic.EbookPath);
            var zipPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            using var archive = ZipFile.Open(zipPath,ZipArchiveMode.Update);
            var entriesWithDots = archive.Entries.Where(file => file.Name[0] == '.').ToList();
            Log.Here().Information("Détection de {Count} fichier(s) commençant par un . à supprimer", entriesWithDots.Count);
            foreach (var entry in entriesWithDots)
            {
                entry.Delete();
            }
        }

        public void AddComicInfoInComicFile(Comic comic)
        {
            var comicInfo = new ComicInfo
            {
                Title = comic.Title,
                Series = comic.Serie,
                Writer = comic.Writer,
                Penciller = comic.Penciller,
                Colorist = comic.Colorist,
                Editor = comic.Editor,
                PageCount = comic.PageCount,
                LanguageISO = comic.LanguageIso,
                ISBN = comic.Isbn,
                Web = comic.FicheUrl,
                Price = comic.Price,
                Published = comic.Published,
                Tags = comic.Category,
                Review = comic.Review,
                Volume = comic.Volume
            };

            var comicEbookPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);

            using var zipToOpen = new FileStream(comicEbookPath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);

            // Suppression du fichier ComicInfo.xml si il existe
            var entry = archive.GetEntry("ComicInfo.xml");
            entry?.Delete();

            // Ajout du fichier ComicInfo.xml dans l'archive
            var comicInfoEntry = archive.CreateEntry("ComicInfo.xml");
            var xmlWriterSettings = new XmlWriterSettings { Indent = true };
            using var writer = new StreamWriter(comicInfoEntry.Open());
            
            
            var mySerializer = new XmlSerializer(typeof(ComicInfo));
            mySerializer.Serialize(XmlWriter.Create(writer, xmlWriterSettings), comicInfo);
            writer.Close();
        }

        public Comic ExtractDataFromComicInfo(Comic comic)
        {
            var comicEbookPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);

            using var zipToOpen = new FileStream(comicEbookPath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);

            // Vérification de la présence du fichier ComicInfo.xml
            var entry = archive.GetEntry("ComicInfo.xml");
            if (entry == null)
            {
                return comic;
            }

            // Construction de l'objet ComicInfo à partir de l'XML
            using var reader = new StreamReader(entry.Open());
            var serializer = new XmlSerializer(typeof(ComicInfo));
            var comicInfo = (ComicInfo) serializer.Deserialize(reader);

            // Récupération des informations
            if (comicInfo == null)
            {
                return comic;
            }

            comic.Title = comicInfo.Title;
            comic.Serie = comicInfo.Series;
            comic.Writer = comicInfo.Writer;
            comic.Penciller = comicInfo.Penciller;
            comic.Colorist = comicInfo.Colorist;
            comic.Editor = comicInfo.Editor;
            comic.LanguageIso = comicInfo.LanguageISO;
            comic.Isbn = comicInfo.ISBN;
            comic.FicheUrl = comicInfo.Web;
            comic.Price = comicInfo.Price;
            comic.Published = comicInfo.Published;
            comic.Category = comicInfo.Tags;
            comic.Review = comicInfo.Review;
            comic.Volume = comicInfo.Volume;

            return comic;
        }

        private static string CreateTempDirectory()
        {
            // Création d'un répertoire temporaire pour stocker les images
            var tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);
            Log.Here().Information("Création du répertoire temporaire : {Dir}", tempDir);

            if (!tempDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                tempDir += Path.DirectorySeparatorChar;
            }

            return tempDir;
        }

        private static void CleanTempDirectory(string tempDir)
        {
            // Suppression du dossier temporaire
            try
            {
                Directory.Delete(tempDir, true);
                Log.Here().Information("Suppression du dossier temporaire {Path}",tempDir);
            }
            catch (Exception e)
            {
                Log.Here().Error(e, "La suppression du répertoire temporaire {Path} a échoué",tempDir);
            }
        }

        public string GetComicEbookPath(Comic comic, LibraryService.PathType pathType)
        {
            return _libraryService.GetLibraryPath(comic.LibraryId, pathType) + comic.EbookPath;
        }

        private static string GetArchiveType(Comic comic)
        {
            if (SharpCompress.Archives.Zip.ZipArchive.IsZipFile(comic.EbookPath))
            {
                return ".zip";
            }

            if (RarArchive.IsRarFile(comic.EbookPath))
            {
                return ".rar";
            }
            else
            {
                return Path.GetExtension(comic.EbookPath);
            }
        }

        public string GetComicFileAbsolutePath(Comic comic)
        {
            return comic.ImportStatus switch
            {
                // CREATED : Le fichier est dans /import et EbookPath est déjà en absolute
                // ERROR : Le fichier est dans /import/errors et EbookPath est déjà en absolute
                ImportStatus.CREATED or ImportStatus.ERROR => comic.EbookPath,
                
                // Cas nominaux
                ImportStatus.COMICINFO_ADDED or ImportStatus.MOVED_TO_LIB or ImportStatus.NB_IMAGES_SET or ImportStatus.COVER_GENERATED or ImportStatus.IMPORTED => 
                    GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH),// Le fichier est dans /lib
                
                // Autres cas
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        
        private Comic Move(Comic comic, string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                return comic;
            }

            try
            {
                var absolutePath = GetComicFileAbsolutePath(comic);
                if (absolutePath == destination)
                {
                    // Le fichier est déjà au bon endroit, pas besoin de le déplacer
                    return comic;
                }
                
                while (File.Exists(destination))
                {
                    Log.Warning("Le fichier {File} existe déjà", destination);
                    var fileName = Path.GetFileNameWithoutExtension(destination) + "-Duplicate" + Path.GetExtension(destination);
                    Log.Warning("Il va être renommé en {FileName}", fileName);
                    destination = Path.GetPathRoot(destination) + fileName;
                }
            
                Log.Here().Information("Origine : {Origine}", absolutePath);
                Log.Here().Information("Destination : {Destination}", destination);

                File.Move(absolutePath, destination);
                Log.Warning("Le fichier {Origin} a été déplacé dans {Destination}", absolutePath, destination);
                comic.EbookPath = destination;
            }
            catch (Exception e)
            {
                Log.Here().Error("Erreur lors du déplacement du fichier {Origin} vers {Destination}", comic.EbookPath, destination);
                throw new ComicIoException("Erreur lors du déplacement du fichier. Consulter le répertoire errors.", e);
            }

            return comic;
        }

        public Comic MoveToLib(Comic comic, string relPathinLib)
        {
            // Déplacement du fichier dans la librairie
            string destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH);
            Directory.CreateDirectory(destination + Path.GetDirectoryName(relPathinLib));
            var ebookPath = relPathinLib + comic.EbookName;
            destination += ebookPath;
            
            comic = Move(comic, destination);
            comic.EbookPath = ebookPath;
            return comic;
        }

        public Comic MoveToError(Comic comic)
        {
            // Déplacement du fichier vers le répertoire d'erreurs d'import
            var destination = _applicationConfigurationService.GetPathImportErrors();
            destination += comic.EbookName;
            
            comic = Move(comic, destination);
            comic.EbookPath = destination;
            return comic;
        }
        
        public Comic MoveToImport(Comic comic)
        {
            // Déplacement du fichier vers le répertoire d'erreurs d'import
            var destination = _applicationConfigurationService.GetPathFileImport();
            destination += comic.EbookName;
            
            comic = Move(comic, destination);
            comic.EbookPath = destination;
            return comic;
        }

        public long GetComicFileSize(Comic comic)
        {
            var absolutePath = GetComicFileAbsolutePath(comic);
            try
            {
                return new FileInfo(absolutePath).Length;
            }
            catch (FileNotFoundException)
            {
                Log.Here().Error("FileNotFoundException : Le fichier {File} n'a pas été trouvé (Id : {Id})", absolutePath, comic.Id);
                return 0;
            }
            catch (IOException)
            {
                Log.Here().Error("IOException : Le traitement du fichier {File} a échoué (Id : {Id})", absolutePath, comic.Id);
                return 0;
            }
            
        }
    }
}