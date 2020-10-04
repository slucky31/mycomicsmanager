using System.IO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MyComicsManagerWeb.Models;
using Tewr.Blazor.FileReader;

namespace MyComicsManagerWeb.Services {
    public class ComicService
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly IWebserviceSettings _settings;
        private readonly LibraryService _libraryService;

        public ComicService(HttpClient client, IWebserviceSettings settings, LibraryService libraryService)
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };
            
            _settings = settings;
            _libraryService = libraryService;
            client.BaseAddress = new Uri(settings.WebserviceUri);
            _httpClient = client;
        }

        public async Task<IEnumerable<Comic>> GetComics()
        {
            var response = await _httpClient.GetAsync("/api/comics");

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync
                <IEnumerable<Comic>>(responseStream);
        }

        public async Task<Comic> GetComic(string id)
        {
            var response = await _httpClient.GetAsync("/api/comics/" + id);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Comic>(responseStream);
        }

        public async Task DeleteComic(String itemId)
        {
            using var httpResponse = await _httpClient.DeleteAsync($"/api/Comics/{itemId}");

            httpResponse.EnsureSuccessStatusCode();

        }

        public async Task CreateComicAsync(Comic comicItem)
        {
            var comicItemJson = new StringContent(
                JsonSerializer.Serialize(comicItem, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PostAsync("/api/Comics", comicItemJson);

            httpResponse.EnsureSuccessStatusCode();
        }

        public async Task UpdateComicAsync(Comic comicItem)
        {
            var comicItemJson = new StringContent(
                JsonSerializer.Serialize(comicItem, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PutAsync("/api/Comics", comicItemJson);

            httpResponse.EnsureSuccessStatusCode();
        }

        public async Task UploadFile(IFileReference file, string fileName)
        {          
            Library lib = await _libraryService.GetSelectedLibrary();
            
            var filePath = Path.Combine(_settings.FileUploadDirRootPath, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var fs = await file.OpenReadAsync())
            {
                await fs.CopyToAsync(fileStream);
            }
        }

        
    }
}