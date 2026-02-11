using System.Net.Http.Json;
using DeliveryService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Services;

namespace DeliveryService.Infrastructure.Services;

public class CourierDirectoryHttpClient : ICourierDirectory
{
    private readonly HttpClient _client;
    private readonly ILogger<CourierDirectoryHttpClient> _logger;
    private readonly string _baseUrl;

    public CourierDirectoryHttpClient(
        HttpClient client,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<CourierDirectoryHttpClient> logger)
    {
        _client = client;
        _logger = logger;
        _baseUrl = ConfigurationGuard.GetRequired(config, env, "Services:CourierService:Url", "http://localhost:5206");
    }

    public async Task<IReadOnlyList<CourierCandidate>> GetActiveCouriersAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetFromJsonAsync<ApiResponseWrapper<List<CourierCandidateDto>>>(
                $"{_baseUrl}/api/couriers/active", ct);

            if (response?.Success != true || response.Data == null)
                return Array.Empty<CourierCandidate>();

            return response.Data.Select(c => new CourierCandidate
            {
                Id = c.Id,
                Latitude = c.CurrentLatitude,
                Longitude = c.CurrentLongitude,
                Rating = c.Rating,
                LastLocationUpdate = c.LastLocationUpdate
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active couriers");
            return Array.Empty<CourierCandidate>();
        }
    }

    private class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private class CourierCandidateDto
    {
        public Guid Id { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public double Rating { get; set; }
    }
}
