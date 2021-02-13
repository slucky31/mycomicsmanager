using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using MyComicsManagerWeb.Services;

namespace MyComicsManagerWeb.Pages
{
    public partial class UploadComics
    {
        
        [Inject] 
        private ComicService ComicService { get; set; }
        
        public List<ComicFile> uploadedFiles = new List<ComicFile>();

        public readonly int maxAllowedFiles = 10;

        private readonly string[] AllowedExtensions = new[] { ".cbr", ".cbz", ".pdf" };

        public async Task LoadFiles(InputFileChangeEventArgs e)
        {

            uploadedFiles.Clear();
            string exceptionMessage = string.Empty;


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
            }
        }

        public class ComicFile
        {

            public string Name { get; set; }

            public long Size { get; set; }

            public double UploadDuration { get; set; }

            public string ExceptionMessage { get; set; }
        }

        private class FileValidationAttribute : ValidationAttribute
        {
            public FileValidationAttribute(string[] allowedExtensions)
            {
                AllowedExtensions = allowedExtensions;
            }

            private string[] AllowedExtensions { get; }

            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                var file = (IBrowserFile)value;

                var extension = System.IO.Path.GetExtension(file.Name);

                if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return new ValidationResult($"File must have one of the following extensions: {string.Join(", ", AllowedExtensions)}.", new[] { validationContext.MemberName });
                }

                return ValidationResult.Success;
            }
        }
    }
}
