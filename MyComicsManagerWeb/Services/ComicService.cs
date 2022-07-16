using System.IO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyComicsManager.Model.Shared;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.Json.Serialization;
using MyComicsManagerWeb.Models;
using Serilog;

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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IEnumerable<Comic>>(responseStream);
        }
        
        public async Task<IEnumerable<Comic>> GetComicsOrderByLastAddedLimitBy(int limit)
        {
            var response = await _httpClient.GetAsync($"/api/comics/orderBy/lastAdded/limit/{limit}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IEnumerable<Comic>>(responseStream);
        }
        
        public async Task<IEnumerable<string>> GetComicsSeries()
        {
            var response = await _httpClient.GetAsync($"/api/comics/series");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IEnumerable<string>>(responseStream);
        }
        
        public async Task<IEnumerable<Comic>> GetComicsOrderBySerieLimitBy(int limit)
        {
            var response = await _httpClient.GetAsync($"/api/comics/orderBy/serie/limit/{limit}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IEnumerable<Comic>>(responseStream);
        }
        
        public async Task<IEnumerable<Comic>> GetComicsRandomLimitBy(int limit)
        {
            var response = await _httpClient.GetAsync($"/api/comics/random/limit/{limit}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<IEnumerable<Comic>>(responseStream);
        }

        public async Task<PaginationComics> FindByPageOrderBySerie(string searchItem, int pageId, int pageSize)
        {
            var response = await _httpClient.GetAsync($"/api/Comics/findbypageorderbyserie?searchItem={searchItem}&pageId={pageId}&pageSize={pageSize}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<PaginationComics>(responseStream);
        }

        public async Task<Comic> GetComic(string id)
        {
            var response = await _httpClient.GetAsync($"/api/comics/{id}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
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

            if (!httpResponse.IsSuccessStatusCode)
            {
                Log.Error("Une erreur est survenue lors de la création du comic {EbookPath}", comicItem.EbookPath);
            }
        }

        public async Task<Comic> UpdateComicAsync(Comic comicItem)
        {
            var comicItemJson = new StringContent(
                JsonSerializer.Serialize(comicItem, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PutAsync($"/api/comics/{comicItem.Id}", comicItemJson);

            httpResponse.EnsureSuccessStatusCode();

            return await GetComic(comicItem.Id);
        }

        public async Task<Comic> SearchComicInfoAsync(string id)
        {
            using var httpResponse = await _httpClient.GetAsync($"api/Comics/searchcomicinfo/{id}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                Log.Warning("Le comic {Id} n'a pas été trouvé dans la base de référence", id);
                return null;
            }

            await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Comic>(responseStream);
        }

        public async Task ExtractCover(string id)
        {
            using var httpResponse = await _httpClient.GetAsync($"api/Comics/extractcover/{id}");

            httpResponse.EnsureSuccessStatusCode();            
        }

        public async Task<List<string>> ExtractIsbn(string id, int indexImage)
        {
            using var response = await _httpClient.GetAsync($"api/Comics/extractisbn/{id}&{indexImage}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<string>>(responseStream);

        }

        public async Task<List<string>> ExtractImages(string id, int nbImagesToExtract, bool first)
        {
            using var response = await _httpClient.GetAsync($"api/Comics/extractimages/{id}&{nbImagesToExtract}&{first}");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<string>>(responseStream);

        }

        public async Task UploadFile(IBrowserFile file)
        {
            long maxFileSize = 1024 * 1024 * 400; // 400 Mo
            await _libraryService.GetSelectedLibrary();

            // Création du répertoire si il n'existe pas
            Directory.CreateDirectory(_settings.FileUploadDirRootPath);
            
            // Upload du fichier
            await using var savedFile = File.OpenWrite(Path.Combine(_settings.FileUploadDirRootPath, file.Name));
            await using var stream = file.OpenReadStream(maxFileSize);
            await stream.CopyToAsync(savedFile).ConfigureAwait(false);
        }

        
        
        public async Task<long> GetNbComics()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comics");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbComicsWithoutSerie()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comicsWithoutSeries");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbComicsWithoutIsbn()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comicsWithoutIsbn");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbComicsRead()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comicsRead");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbComicsUnRead()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comicsUnRead");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbSeries()
        {
            var response = await _httpClient.GetAsync($"/api/stats/series");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbComicsUnWebpFormated()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comicsUnWebpFormated");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<long> GetNbComicsImportedWithErrors()
        {
            var response = await _httpClient.GetAsync($"/api/stats/comicsImportedWithErrors");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<long>(responseStream);
        }
        
        public async Task<List<Comic>> GetImportingComics()
        {
            var response = await _httpClient.GetAsync($"/api/comics/importing");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Comic>>(responseStream);
        }
        
        public async Task<List<ComicFile>> ListUploadedFiles()
        {
            var files = await _libraryService.GetUploadedFiles();
            Library lib = await _libraryService.GetSelectedLibrary();

            var comicFiles = new List<ComicFile>();
            foreach (var file in files)
            {
                ComicFile uploadedFile = new ComicFile
                {
                    Name = file.EbookName,
                    Size = file.Size,
                    LibId = lib.Id,
                    Path = file.EbookPath,
                    UploadDuration = 0,
                    ExceptionMessage = string.Empty
                };
                comicFiles.Add(uploadedFile);
            }
            return comicFiles;
        }
        
        

    }
}