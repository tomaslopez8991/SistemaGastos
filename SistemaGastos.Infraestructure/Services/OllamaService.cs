using Microsoft.Extensions.Configuration;
using SistemaGastos.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SistemaGastos.Infraestructure.Services
{
    public class OllamaService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public OllamaService(HttpClient httpClient, IConfiguration configuration)
        {
            var baseUrl = configuration["AiProviders:Ollama:BaseUrl"] ?? "http://localhost:11434";
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(baseUrl);
            _model = configuration["AiProviders:Ollama:Model"] ?? "llama3.2";
        }

        public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/generate", new
            {
                model = _model,
                prompt,
                stream = false
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"No se pudo conectar con Ollama en {_httpClient.BaseAddress} (¿está corriendo y con el modelo '{_model}' descargado?).");

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);
            return result?.Response ?? string.Empty;
        }

        private class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }
    }
}
