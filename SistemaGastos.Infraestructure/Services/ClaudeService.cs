using Microsoft.Extensions.Configuration;
using SistemaGastos.Application.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaGastos.Infraestructure.Services
{
    public class ClaudeService : IAiService
    {
        private const string Model = "claude-sonnet-4-5-20250929";
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public ClaudeService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
            _apiKey = configuration["AiProviders:Claude:ApiKey"];
        }

        public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("No hay una API key de Claude configurada (AiProviders:Claude:ApiKey).");

            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(new
            {
                model = Model,
                max_tokens = 1024,
                messages = new[] { new { role = "user", content = prompt } }
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: cancellationToken);
            return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
        }

        private class ClaudeResponse
        {
            [JsonPropertyName("content")]
            public List<ClaudeContent>? Content { get; set; }
        }

        private class ClaudeContent
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
