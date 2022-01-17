using System;
using System.Threading.Tasks;
using BlazorBarcodeScanner.ZXing.JS;
using MyComicsManagerWeb.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MyComicsManagerWeb.Models;

namespace MyComicsManagerWeb.Pages
{
    public partial class CreateBook
    {
        [Inject]
        private BookService BookService { get; set; }
        
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private Book _book = new Book();
        
        private async Task Create()
        {
            _book.Added = DateTime.Now;
            _book = await BookService.Create(_book);
            var tmp = await BookService.SearchBookInfoAsync(_book.Id);
            if (tmp != null)
            {
                _snackbar.Add("Recherche terminée avec succès !", Severity.Success);
                await BookService.Update(tmp);
                NavigationManager.NavigateTo("book", false);
            }
            else
            {
                _snackbar.Add("Comic inconnu au bataillon !,", Severity.Warning);
                NavigationManager.NavigateTo("book", false);
            }
            
        }
        
        private void LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
        {
            _book.Isbn = args.BarcodeText;
            StateHasChanged();
        }
        
    }
}
