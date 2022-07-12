using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MyComicsManagerWeb.Services;
using MyComicsManager.Model.Shared;
using System.IO;

namespace MyComicsManagerWeb.Pages
{
    public partial class ImportComics
    {
        
        [Inject] 
        private ComicService ComicService { get; set; }
        [Inject]
        private LibraryService LibraryService { get; set; }

        private List<ComicFile> UploadedFiles { get; set; } = new();
        private List<Comic> ImportingComics { get; set; } = new();
        
        private bool Importing { get; set; }

        private Library Library { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            UploadedFiles = await ComicService.ListUploadedFiles();
            await RefreshImportingComics();
            Library = await LibraryService.GetSelectedLibrary();
            StateHasChanged();
        }

        private async Task AddComic(ComicFile file)
        {
            Importing = true;
            Comic comic = new Comic
            {
                EbookName = file.Name,
                EbookPath = file.Path,
                Title = Path.GetFileNameWithoutExtension(file.Name),
                LibraryId = file.LibId

            };
            await ComicService.CreateComicAsync(comic);
            Importing = false;  
            UploadedFiles.Remove(file);
            StateHasChanged();
        }

        private async Task AddComics()
        {
            foreach(var file in UploadedFiles.ToList())
            {
                await AddComic(file);
            }
            StateHasChanged();
        }
        
        private async Task Delete(string id)
        {
            await ComicService.DeleteComic(id);
            await RefreshImportingComics();
        }
        
        private async Task RefreshImportingComics()
        {
            ImportingComics = await ComicService.GetImportingComics();
            ImportingComics = ImportingComics.Where(comic => comic.ImportStatus != ImportStatus.ERROR).ToList();
            StateHasChanged();
        }
    }
}
