using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using MyComicsManagerWeb.Services;
using System.IO;
using System.Text;
using MyComicsManager.Model.Shared.Models;

namespace MyComicsManagerWeb.Pages
{
    public partial class Upload
    {
        
        [Inject] 
        private ComicService ComicService { get; set; }
        [Inject]
        private LibraryService LibraryService { get; set; }
        private List<ComicFile> UploadedFiles { get; } = new();

        private List<Comic> ImportingComics { get; set; }  
        private int MaxAllowedFiles { get; } = 10;
        private bool Uploading { get; set; }

        private IList<IBrowserFile> BrowserFiles { get; set; } = new List<IBrowserFile>();        
        private string[] AllowedExtensions { get; } = new[] { ".cbr", ".cbz", ".pdf", ".zip", ".rar" };
        private int DragElevation { get; set; }
        
        private void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            BrowserFiles.Clear();
            var temp = (IList<IBrowserFile>)e.GetMultipleFiles(MaxAllowedFiles);
            foreach(var item in temp)
            {
                BrowserFiles.Add(item);
            }
            StateHasChanged();
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

                    StateHasChanged();
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
                StateHasChanged();
            }
            Uploading = false;
            StateHasChanged();
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
