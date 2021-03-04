using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using MyComicsManagerApi.Models;
using Serilog;

namespace MyComicsManagerApi.Services
{
    public class ComicFileService
    {

        private readonly LibraryService _libraryService;

        public ComicFileService(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }


        //
        // https://docs.microsoft.com/fr-fr/dotnet/standard/io/how-to-compress-and-extract-files
        //
        public void SetAndExtractCoverImage(Comic comic)
        {
            string zipPath = comic.EbookPath;

            // Normalizes the path.
            string extractPath = Path.GetFullPath(_libraryService.GetCoversDirRootPath());
            Log.Information("Les fichiers seront extraits dans {path}", extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            string destinationPath = "";

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                ZipArchiveEntry entry = archive.Entries.First();
                if (null != entry)
                {
                    Log.Information("Fichier à extraire {FileName}", entry.FullName);                    
                    destinationPath = Path.GetFullPath(Path.Combine(extractPath, comic.EbookName + "_" + entry.FullName));
                    Log.Information("Destination {destination}", destinationPath);

                    if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                        entry.ExtractToFile(destinationPath);
                }
                                
            }

            // Update comic with file
            comic.CoverPath = destinationPath;

        }
    }
}
