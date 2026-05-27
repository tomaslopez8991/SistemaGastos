using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SistemaGastos.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemaGastos.Infraestructure.Services
{
    public class DolarService : IDolarService
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private const string CacheKey = "DolarBolsa";
        private const int CacheDurationInHours = 24;
        private readonly string _dolarApiUrl;

        public DolarService(IMemoryCache cache, HttpClient httpClient, IConfiguration configuration)
        {
            _cache = cache;
            _httpClient = httpClient;
            _dolarApiUrl = configuration["DolarApi:BolsaUrl"];
        }

        public async Task<decimal> GetDolarTarjetaAsync()
        {
            if (!_cache.TryGetValue(CacheKey, out decimal dolarTarjeta))
            {
                dolarTarjeta = await ObtenerValorDolarTarjetaAsync();

                // Guardar en caché por 24 horas
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationInHours)
                };

                _cache.Set(CacheKey, dolarTarjeta, cacheEntryOptions);
            }
            return dolarTarjeta;
        }

        public async Task<decimal> GetDolarBolsaAsync()
        {
            if (!_cache.TryGetValue(CacheKey, out decimal dolarBolsa))
            {
                dolarBolsa = await ObtenerValorDolarBolsaAsync();

                // Guardar en caché por 24 horas
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationInHours)
                };

                _cache.Set(CacheKey, dolarBolsa, cacheEntryOptions);
            }
            return dolarBolsa;
        }

        private async Task<decimal> ObtenerValorDolarBolsaAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_dolarApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                decimal valorDolarTarjeta = root.GetProperty("venta").GetDecimal();
                return valorDolarTarjeta;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el valor del dólar bolsa: {ex.Message}");
                return 0m;
            }
        }

        private async Task<decimal> ObtenerValorDolarTarjetaAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_dolarApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                decimal valorDolarTarjeta = root.GetProperty("venta").GetDecimal();
                return valorDolarTarjeta;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el valor del dólar tarjeta: {ex.Message}");
                return 0m;
            }
        }
    }
}
