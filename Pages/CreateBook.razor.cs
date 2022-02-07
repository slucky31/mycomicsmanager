using System;
using System.Threading.Tasks;
using MyComicsManagerWeb.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
        
        [Inject] IJSRuntime JS { get; set; }

        [Parameter] public EventCallback<string> OnISBNDetected { get; set; }

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

        private void GetBookInformation(string isbn)
        {
            Book.Isbn = isbn;
            StateHasChanged();
        }
        
        string barcodeScannerElementStyle;

        private async Task InitializeBarcodeScanner()
        {
            barcodeScannerElementStyle = string.Empty;
            var dotNetObjectReference = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("InitBarcodeScanner", dotNetObjectReference);
        }

        [JSInvokable]
        public async Task OnDetected(string isbn)
        {
            barcodeScannerElementStyle = "display:none;";
            Book.Isbn = isbn;
            StateHasChanged();
            await OnISBNDetected.InvokeAsync(isbn);
        }
        
    }
}
