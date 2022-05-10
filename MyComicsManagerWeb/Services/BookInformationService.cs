using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Google.Apis.Books.v1.Data;
using MyComicsManagerWeb.Models;
using Serilog;

namespace MyComicsManagerWeb.Services {
    public class BookInformationService
    {
        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public BookInformationService(HttpClient client, IWebserviceSettings settings)
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            
            client.BaseAddress = new Uri(settings.WebserviceUri);
            _httpClient = client;
        }
        
        public async Task<Volumes> SearchBookInfoAsync(string query)
        {
            Log.Information("SearchBookInfoAsync avec la requête {Query}", query);
            
            using var httpResponse = await _httpClient.GetAsync($"api/BookInformation/{query}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                Log.Warning("Aucun livre trouvé pour la requête {Query}", query);
                return null;
            }

            await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Volumes>(responseStream);
        }
    }
}