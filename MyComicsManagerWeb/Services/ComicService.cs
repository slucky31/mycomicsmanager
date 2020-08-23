using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using MyComicsManagerWeb.Models;

namespace MyComicsManagerWeb.Services {
    public class ComicService
    {
        private readonly HttpClient _httpClient;

        public ComicService(HttpClient client)
        {
            client.BaseAddress = new Uri("http://localhost:5000/");
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
    }
}