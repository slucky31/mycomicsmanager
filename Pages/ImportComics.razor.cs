using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using MyComicsManagerWeb.Services;
using MyComicsManagerWeb.Models;
using System.IO;
using System.Text;

namespace MyComicsManagerWeb.Pages
{
    public partial class ImportComics
    {
        
        [Inject] 
        private ComicService ComicService { get; set; }
        [Inject]
        private LibraryService LibraryService { get; set; }
        private List<ComicFile> UploadedFiles { get; } = new();
        private List<ComicFile> ImportingFiles { get; }  = new();
        private int MaxAllowedFiles { get; } = 10;
        private bool Uploading { get; set; }
        private bool Importing { get; set; }
        
        private bool Listing { get; set; }
        private bool ImportingComicFile { get; set; }
        private IList<IBrowserFile> BrowserFiles { get; set; } = new List<IBrowserFile>();        
        private string[] AllowedExtensions { get; } = new[] { ".cbr", ".cbz", ".pdf", ".zip", ".rar" };
        private int DragElevation { get; set; }


        protected override async Task OnInitializedAsync()
        {
            await ListImportingFiles().ConfigureAwait(false);
            Uploading = false;
            ImportingComicFile = false;
            Importing = false;
            Listing = false;
            await base.OnInitializedAsync();
        }

        private void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            BrowserFiles.Clear();
            var temp = (IList<IBrowserFile>)e.GetMultipleFiles(MaxAllowedFiles);
            foreach(var item in temp)
            {
                BrowserFiles.Add(item);
            }
        }

        private async Task UploadFiles()
        {

            UploadedFiles.Clear();
            string exceptionMessage = string.Empty;
            Uploading = true;

            foreach (IBrowserFile file in BrowserFiles)
            {
                Stopwatch stopWatch = new Stopwatch();

                // Check extension
                var extension = Path.GetExtension(file.Name);
                if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    exceptionMessage = "File must have one of the following extensions: " + string.Join(", ", AllowedExtensions);
                }
                else
                {
                    stopWatch.Start();

                    try
                    {
                        await ComicService.UploadFile(file);
                    }
                    catch (Exception ex)
                    {
                        exceptionMessage = ex.Message;
                    }

                    this.StateHasChanged();
                    stopWatch.Stop();
                }

                ComicFile uploadedFile = new ComicFile
                {
                    Name = file.Name,
                    Size = file.Size,
                    UploadDuration = stopWatch.ElapsedMilliseconds / 1000.0,                    
                    ExceptionMessage = exceptionMessage
                };
                UploadedFiles.Add(uploadedFile);                
                this.StateHasChanged();
            }
            Uploading = false;
            await this.ListImportingFiles();
            this.StateHasChanged();
        }

        private async Task AddComic(ComicFile file)
        {
            ImportingComicFile = true;
            Comic comic = new Comic
            {
                EbookName = file.Name,
                EbookPath = file.Path,
                Title = Path.GetFileNameWithoutExtension(file.Name),
                LibraryId = file.LibId

            };
            await ComicService.CreateComicAsync(comic);
            ImportingComicFile = false;  
            ImportingFiles.Remove(file);
            this.StateHasChanged();
        }

        private async Task AddComics()
        {
            Importing = true;
            foreach(var file in ImportingFiles.ToList())
            {
                await AddComic(file);
            }
            Importing = false;
            this.StateHasChanged();
        }

        private async Task ListImportingFiles()
        {
            Listing = true;
            var files = ComicService.ListImportingFiles();
            Library lib = await LibraryService.GetSelectedLibrary();
                   
            ImportingFiles.Clear();
            foreach (var file in files)
            {
                ComicFile uploadedFile = new ComicFile
                {
                    Name = file.Name,
                    Size = file.Length,
                    LibId = lib.Id,
                    Path = file.FullName,
                    UploadDuration = 0,
                    ExceptionMessage = string.Empty
                };
                ImportingFiles.Add(uploadedFile);
            }
            Listing = false;
            this.StateHasChanged();
        }

        public class ComicFile
        {

            public string Name { get; init; }

            public long Size { get; init; }

            public string LibId { get; init; }
            
            public string Path { get; init; }

            public double UploadDuration { get; init; }

            public string ExceptionMessage { get; init; }
        }

        private string GetAllowedExtensions()
        {
            StringBuilder sb = new();
            foreach (var ext in AllowedExtensions)
            {
                sb.Append(ext);
                if (ext != AllowedExtensions.Last())
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }

        
    }
}
