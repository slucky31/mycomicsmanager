using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MyComicsManager.Model.Shared;
using MyComicsManagerWeb.Settings;

namespace MyComicsManagerApi.Services {
    
    public class NotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        
        public NotificationService(HttpClient client, INotificationSettings settings)
        {
            client.BaseAddress = new Uri(settings.WebserviceUri);
            _httpClient = client;
            _token = settings.Token;
        }
        
        public async Task Send(string title, string message)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("title", title),
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("priority", "5")
            });

            using var httpResponse =
                await _httpClient.PostAsync($"/message?token={_token}", content);

            httpResponse.EnsureSuccessStatusCode();
        }
        
    }
}