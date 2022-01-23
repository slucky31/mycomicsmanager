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

        private Book Book { get;  } = new Book();
        
        private async Task Create()
        {

            var tmp = await BookService.SearchBookInfoAsync(Book.Isbn);
            if (tmp != null)
            {
                _snackbar.Add("Recherche terminée avec succès !", Severity.Success);
                tmp.Added = DateTime.Now;
                tmp.Review = Book.Review;
                await BookService.Create(tmp);
                NavigationManager.NavigateTo("book", false);
            }
            else
            {
                _snackbar.Add("Comic inconnu au bataillon !", Severity.Warning);
                NavigationManager.NavigateTo("book", false);
            }
            
        }
        
        private void LocalReceivedBarcodeText(BarcodeReceivedEventArgs args)
        {
            Book.Isbn = args.BarcodeText;
            StateHasChanged();
        }
        
    }
}
