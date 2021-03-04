using System.IO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyComicsManagerWeb.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq;
using Serilog;

namespace MyComicsManagerWeb.Services {
    public class ComicService
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly IWebserviceSettings _settings;
        private readonly LibraryService _libraryService;
        private readonly string[] extensions = { "*.cbr", "*.cbz", "*.pdf" };

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

        public async Task DeleteAllComicsFromLib(String libId)
        {
            using var httpResponse = await _httpClient.DeleteAsync($"/api/Comics/DeleteAllComicsFromLib/{libId}");

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

        public async Task UploadFile(IBrowserFile file)
        {
            long maxFileSize = 1024 * 1024 * 400; // 400 Mo
            await _libraryService.GetSelectedLibrary();

            // Création du répertoire si il n'existe pas
            Directory.CreateDirectory(_settings.FileUploadDirRootPath);
            
            // Upload du fichier
            using var savedFile = File.OpenWrite(Path.Combine(_settings.FileUploadDirRootPath, file.Name));
            using Stream stream = file.OpenReadStream(maxFileSize);
            await stream.CopyToAsync(savedFile);
        }

        public IEnumerable<FileInfo> ListImportingFiles()
        {
            Log.Information("Recherche des fichiers dans {path}", _settings.FileUploadDirRootPath);
            
            // Création du répertoire si il n'existe pas
            Directory.CreateDirectory(_settings.FileUploadDirRootPath);

            // Lister les fichiers
            var directory = new DirectoryInfo(_settings.FileUploadDirRootPath);        
            return extensions.SelectMany(directory.EnumerateFiles);
        }

    }
}