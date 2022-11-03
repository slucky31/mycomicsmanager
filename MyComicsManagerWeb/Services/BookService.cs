using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MyComicsManager.Model.Shared.Models;
using MyComicsManagerWeb.Models;
using Serilog;

namespace MyComicsManagerWeb.Services {
    public class BookService
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public BookService(HttpClient client, IWebserviceSettings settings)
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            
            client.BaseAddress = new Uri(settings.WebserviceUri);
            _httpClient = client;
        }

        public async Task<IEnumerable<Book>> List()
        {
            var response = await _httpClient.GetAsync("/api/books");

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync
                <IEnumerable<Book>>(responseStream);
        }

        public async Task<Book> Get(string id)
        {
            var response = await _httpClient.GetAsync("/api/books/" + id);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Book>(responseStream);
        }

        public async Task Delete(String itemId)
        {
            using var httpResponse = await _httpClient.DeleteAsync($"/api/books/{itemId}");

            httpResponse.EnsureSuccessStatusCode();

        }

        public async Task<Book> Create(Book book)
        {
            var itemJson = new StringContent(
                JsonSerializer.Serialize(book, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PostAsync("/api/books", itemJson);

            httpResponse.EnsureSuccessStatusCode();
            
            using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Book>(responseStream);
        }

        public async Task Update(Book book)
        {
            var ItemJson = new StringContent(
                JsonSerializer.Serialize(book, _jsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            using var httpResponse =
                await _httpClient.PutAsync($"/api/books/{book.Id}", ItemJson);

            httpResponse.EnsureSuccessStatusCode();
        }
        
        public async Task<Book> SearchBookInfoAsync(string id)
        {
            using var httpResponse = await _httpClient.GetAsync($"api/books/getinfo/{id}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                Log.Warning("Le book {Id} n'a pas été trouvé dans la base de référence", id);
                return null;
            }

            await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Book>(responseStream);
        }
    }
}