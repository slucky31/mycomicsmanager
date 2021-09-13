using System;
using System.Linq;
using System.Threading.Tasks;
using MyComicsManagerWeb.Models;
using MyComicsManagerWeb.Services;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MyComicsManagerWeb.Pages
{
    public partial class CreateComic
    {
        [Inject]
        private ComicService ComicService { get; set; }

        [Inject]
        private LibraryService LibraryService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        public Comic comic = new Comic();
        public string Status1 { get; set; }

        private readonly string[] AllowedExtensions = new[] { ".cbr", ".cbz", ".pdf" };
       


        protected override async Task OnInitializedAsync()
        {
            Library lib = await LibraryService.GetSelectedLibrary();
            
            comic.LibraryId ??= lib.Id;
        }

        private async Task Create()
        {
            await ComicService.CreateComicAsync(comic);
            NavigationManager.NavigateTo("comics/list", false);
        }

        public async Task ReadFile(InputFileChangeEventArgs e)
        {
            var file = e.File;

            Status1 = $"Reading file...";
            this.StateHasChanged();

            Stopwatch stopWatch = new Stopwatch();

            // Check extension
            var extension = System.IO.Path.GetExtension(file.Name);
            if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                _ = "File must have one of the following extensions: " + string.Join(", ", AllowedExtensions);
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
                    _ = ex.Message;
                }

                StateHasChanged();
                stopWatch.Stop();
            }

            Status1 = $"Done reading file.";
            stopWatch.Stop();
            Status1 += " Transfert Duration : {stopWatch.ElapsedMilliseconds} ms.";

            comic.Title ??= Path.GetFileNameWithoutExtension(file.Name);
            comic.EbookPath = "tmp/";
            comic.EbookName = file.Name;
            this.StateHasChanged();

        }
    }
}
