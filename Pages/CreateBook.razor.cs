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
    public partial class CreateBook
    {
        [Inject]
        private BookService BookService { get; set; }
        
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private Book _book = new Book();
        
        private async Task Create()
        {
            _book = await BookService.Create(_book);
            var tmp = await BookService.SearchBookInfoAsync(_book.Id);
            await BookService.Update(tmp);
            NavigationManager.NavigateTo("books", false);
        }
        
    }
}
