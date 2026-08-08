using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using SistemaGastos.Application.Interfaces;

namespace SistemaGastos.Infraestructure.Services;

public class TurnstileService(HttpClient httpClient, IConfiguration configuration) : ITurnstileService
{
    public async Task<bool> ValidateAsync(
        string? token,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("BotProtection:Turnstile:Enabled"))
            return true;

        var secretKey = configuration["BotProtection:Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(token))
            return false;

        var values = new Dictionary<string, string>
        {
            ["secret"] = secretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
            values["remoteip"] = remoteIp;

        try
        {
            using var response = await httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(values),
                cancellationToken);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken);
            return result is { Success: true, Action: "register" };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private sealed record TurnstileResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("action")] string? Action);
}
