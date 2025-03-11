using System.Net.Http.Json;
using LanguageLearningApp.Api.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LanguageLearningApp.Api.Infrastructure.Services
{
    public class LlamaService : ILlamaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LlamaService> _logger;

        public LlamaService(HttpClient httpClient, IConfiguration configuration,ILogger<LlamaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetChatResponseAsync(string prompt, List<string> conversationHistory)
        {
            _logger.LogInformation("Sending prompt to Ollama: {Prompt}", prompt);
            // Ollama API'ye gönderilecek payload
            var requestPayload = new
            {
                model = "llama2-13b-chat",
                prompt,
                conversationHistory // Ollama bu alanı kullanıyorsa ekliyoruz
            };

            // appsettings.json veya Environment variable'dan "BaseUrl" alındığını varsayıyoruz
            var endpoint = $"{_configuration["BaseUrl"]}/api/generate";

            // Ollama servisine POST isteği
            var response = await _httpClient.PostAsJsonAsync(endpoint, requestPayload);

            _logger.LogInformation("Ollama responded with status code: {StatusCode}", response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                // Ollama'nın döndürdüğü JSON'u GenerateResponse modeline parse ediyoruz
                var data = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                 _logger.LogInformation("Ollama content: {Content}", data?.Content);

                // "content" alanını döndürüyoruz; yoksa bir uyarı
                return data?.Content ?? "No 'content' field found in Ollama response.";
            }
            else
            {
                // Hata durumunda gelen içeriği veya reason phrase'i döndürebilirsin
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama error response: {Error}", error);
                return $"Error calling Ollama: {error}";
            }
        }
    }

    // Yanıtı temsil eden DTO
    public class OllamaResponse
    {
        public int Code { get; set; }
        public string Model { get; set; }
        public string Prompt { get; set; }
        public string Content { get; set; } // Ollama cevabı genellikle "content" alanında döner
        public object State { get; set; }
        public List<object> Tokens { get; set; }
    }
}
