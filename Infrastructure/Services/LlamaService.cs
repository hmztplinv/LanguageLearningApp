using System.Net.Http.Json;
using LanguageLearningApp.Api.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LanguageLearningApp.Api.Infrastructure.Services
{
    public class LlamaService : ILlamaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LlamaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetChatResponseAsync(string prompt, List<string> conversationHistory)
        {
            // Ollama API için istek payload'ını oluşturuyoruz.
            var requestPayload = new
            {
                model = "llama2:13b-chat",
                prompt = prompt,
                context = conversationHistory
            };

            // appsettings.json içerisindeki base URL'i alıyoruz.
            var baseUrl = _configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
            var endpoint = $"{baseUrl}/api/generate"; // API endpoint'i; gerekirse uyarlayın.

            var response = await _httpClient.PostAsJsonAsync(endpoint, requestPayload);
            if (response.IsSuccessStatusCode)
            {
                // Örneğin, dönen JSON {"response": "Cevap metni"} şeklinde olsun.
                var responseData = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                return responseData?.Response ?? "No response";
            }
            else
            {
                // Hata durumunu uygun şekilde ele alın.
                return "Error in Llama integration";
            }
        }
    }

    // Yanıtı temsil eden DTO
    public class OllamaResponse
    {
        public string Response { get; set; }
    }
}
