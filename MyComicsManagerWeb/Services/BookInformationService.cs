using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Google.Apis.Books.v1.Data;
using MyComicsManager.Model.Shared;
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
        
        public async Task<Comic> SearchBookInfoAsync(string isbn)
        {
            Log.Information("SearchBookInfoAsync avec la requête {Query}", isbn);
            
            using var httpResponse = await _httpClient.GetAsync($"api/BdPhile/{isbn}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                Log.Warning("Aucun livre trouvé pour la requête {Query}", isbn);
                return null;
            }

            await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<Comic>(responseStream);
        }
    }
}