using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MyComicsManager.Model.Shared;
using MyComicsManagerWeb.Models;

namespace MyComicsManagerWeb.Services {
    public class LibraryService
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public LibraryService(HttpClient client, IWebserviceSettings settings)
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            
            client.BaseAddress = new Uri(settings.WebserviceUri);
            _httpClient = client;
        }

        public async Task<IEnumerable<Library>> GetLibraries()
        {
            var response = await _httpClient.GetAsync("/api/libraries");

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync
                <IEnumerable<Library>>(responseStream);
        }

        public async Task<Library> GetLibrary(string id)
        {
            var response = await _httpClient.GetAsync("/api/libraries/" + id);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Library>(responseStream);
        }

        //TODO : Revoir le calcul du selected : pour l'instant, le premier est pris par défaut
        public async Task<Library> GetSelectedLibrary()
        {
            var libraries = await GetLibraries().ConfigureAwait(false);
            
            Library lib = null;
            using (IEnumerator<Library> enumer = libraries.GetEnumerator()) {
                if (enumer.MoveNext())
                {
                    lib = enumer.Current;
                }
            }

            return lib;
        }

        public async Task DeleteLibrary(String itemId)
        {
            using var httpResponse = await _httpClient.DeleteAsync($"/api/libraries/{itemId}");

            httpResponse.EnsureSuccessStatusCode();

        }

        public async Task CreateLibraryAsync(Library LibraryItem)
        {
            var LibraryItemJson = new StringContent(
                JsonSerializer.Serialize(LibraryItem, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PostAsync("/api/libraries", LibraryItemJson);

            httpResponse.EnsureSuccessStatusCode();
        }

        public async Task UpdateLibraryAsync(Library LibraryItem)
        {
            var LibraryItemJson = new StringContent(
                JsonSerializer.Serialize(LibraryItem, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PutAsync("/api/libraries", LibraryItemJson);

            httpResponse.EnsureSuccessStatusCode();
        }
        
        public async Task<List<Comic>> GetUploadedFiles()
        {
            var response = await _httpClient.GetAsync("/api/Libraries/uploaded");

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<Comic>>(responseStream);
        }
    }
}