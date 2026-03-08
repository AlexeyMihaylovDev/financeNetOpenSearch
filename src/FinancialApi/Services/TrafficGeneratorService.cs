namespace FinancialApi.Services;

public class TrafficGeneratorService : BackgroundService
{
    private readonly ILogger<TrafficGeneratorService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string[] _currencies = { "USD", "EUR", "GBP", "JPY", "RUB" };

    public TrafficGeneratorService(ILogger<TrafficGeneratorService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the app to start up essentially
        await Task.Delay(5000, stoppingToken);
        
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("http://localhost:8080");

        _logger.LogInformation("Traffic generator started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new
                {
                    AccountId = $"ACC_{Random.Shared.Next(1000, 9999)}",
                    Amount = Math.Round((decimal)Random.Shared.NextDouble() * 12000, 2),
                    Currency = _currencies[Random.Shared.Next(_currencies.Length)]
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                
                // Fire and forget
                _ = client.PostAsync("/api/transactions/transfer", content, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating traffic");
            }

            // Generate traffic every 1-5 seconds
            await Task.Delay(Random.Shared.Next(1000, 5000), stoppingToken);
        }
    }
}
