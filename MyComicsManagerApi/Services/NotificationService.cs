using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MyComicsManager.Model.Shared;
using MyComicsManagerWeb.Settings;
using Serilog;

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
        
        private async Task Send(string title, string message)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("title", title),
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("priority", "5")
            });

            try
            {
                using var httpResponse =
                    await _httpClient.PostAsync($"/message?token={_token}", content);
                httpResponse.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                Log.Error("L'envoi de la notification a échoué : Vérifier si le service est démarré");
            }
            
        }
        
        public async Task SendNotificationImportStatus(Comic comic, ImportStatus status)
        {
            var message = $"comic.ImportStatus = {status}";
            await SendNotificationMessage(comic, message);
        }
        
        public async Task SendNotificationMessage(Comic comic, string message)
        {
            var title = $"{comic.Id} : {comic.Title}";
            await Send(title, message);
        }
        
    }
}