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

namespace MyComicsManagerWeb.Pages
{
    public partial class ImportComics
    {
        
        [Inject] 
        private ComicService ComicService { get; set; }

        [Inject]
        private LibraryService LibraryService { get; set; }

        public List<ComicFile> uploadedFiles = new List<ComicFile>();
        public List<ComicFile> importingFiles = new List<ComicFile>();

        public readonly int maxAllowedFiles = 10;
        public bool Uploading { get; set; } = false;

        private readonly string[] AllowedExtensions = new[] { ".cbr", ".cbz", ".pdf" };

        protected override Task OnInitializedAsync()
        {
            ListImportingFiles();
            return base.OnInitializedAsync();
        }

        public async Task LoadFiles(InputFileChangeEventArgs e)
        {

            uploadedFiles.Clear();
            string exceptionMessage = string.Empty;
            Library lib = await LibraryService.GetSelectedLibrary();

            foreach (IBrowserFile file in e.GetMultipleFiles(maxAllowedFiles))
            {
                Stopwatch stopWatch = new Stopwatch();

                // Check extension
                var extension = System.IO.Path.GetExtension(file.Name);
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
                uploadedFiles.Add(uploadedFile);                
                this.StateHasChanged();
            }
            this.ListImportingFiles();
            this.StateHasChanged();
        }

        public async Task AddComic(ComicFile file)
        {

            Comic comic = new Comic
            {
                EbookName = file.Name,
                Title = Path.GetFileNameWithoutExtension(file.Name),
                LibraryId = file.libId

            };
            await ComicService.CreateComicAsync(comic);
            importingFiles.Remove(file);
            this.StateHasChanged();
        }

        public async Task AddComics()
        {

            foreach(var file in importingFiles.ToList())
            {
                await AddComic(file);
            }
            
        }

        private async void ListImportingFiles()
        {
            var files = ComicService.ListImportingFiles();
            Library lib = await LibraryService.GetSelectedLibrary();
                   
            importingFiles.Clear();
            foreach (var file in files)
            {
                ComicFile uploadedFile = new ComicFile
                {
                    Name = file.Name,
                    Size = file.Length,
                    libId = lib.Id,
                    UploadDuration = 0,
                    ExceptionMessage = string.Empty
                };
                importingFiles.Add(uploadedFile);
            }
            this.StateHasChanged();
        }

        public class ComicFile
        {

            public string Name { get; set; }

            public long Size { get; set; }

            public string libId { get; set; }

            public double UploadDuration { get; set; }

            public string ExceptionMessage { get; set; }
        }

        
    }
}
