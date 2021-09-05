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
        public List<ComicFile> uploadedFiles { get; } = new();
        public List<ComicFile> importingFiles { get; }  = new();
        public int maxAllowedFiles { get; } = 10;
        public bool Uploading { get; set; } = false;
        public IList<IBrowserFile> browserFiles { get; set; } = new List<IBrowserFile>();        
        string[] AllowedExtensions { get; } = new[] { ".cbr", ".cbz", ".pdf" };
        public string _dragEnterStyle { get; set; }


        protected override Task OnInitializedAsync()
        {
            ListImportingFiles();
            return base.OnInitializedAsync();
        }

        public void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            var temp = (IList<IBrowserFile>)e.GetMultipleFiles(maxAllowedFiles);
            foreach(var item in temp)
            {
                browserFiles.Clear();
                browserFiles.Add(item);
            }
        }

        public async Task UploadFiles()
        {

            uploadedFiles.Clear();
            string exceptionMessage = string.Empty;
            Uploading = true;

            foreach (IBrowserFile file in browserFiles)
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
            Uploading = false;
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

        private async Task ListImportingFiles()
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

        public string getAllowedExtensions()
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
