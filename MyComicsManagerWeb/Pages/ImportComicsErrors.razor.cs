using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MyComicsManagerWeb.Services;
using MyComicsManager.Model.Shared;

namespace MyComicsManagerWeb.Pages
{
    public partial class ImportComicsErrors
    {
        
        [Inject] 
        private ComicService ComicService { get; set; }
        
        private List<Comic> ImportingComics { get; set; } = new();
        
        protected override async Task OnInitializedAsync()
        {
            await RefreshErrorComics();
            StateHasChanged();
        }

        private async Task Delete(string id)
        {
            await ComicService.DeleteComic(id);
            await RefreshErrorComics();
        }
        
        private async Task RefreshErrorComics()
        {
            ImportingComics = await ComicService.GetImportingComics();
            ImportingComics = ImportingComics.Where(comic => comic.ImportStatus == ImportStatus.ERROR).ToList();
            StateHasChanged();
        }
    }
}
