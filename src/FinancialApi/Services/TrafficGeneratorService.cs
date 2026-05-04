namespace FinancialApi.Services;

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// [LOW] Typo in class name (TraficGenratorServic instead of TrafficGeneratorService)
public class TraficGenratorServic : BackgroundService
{
    // [CRITICAL] Hardcoded AWS Access Key and Secret (Fake, but formatted to trigger regex scanners)
    private const string AwsAccessKey = "AKIAIOSFODNN7EXAMPLE";
    private const string AwsSecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
    
    // [CRITICAL] Hardcoded database connection string with plaintext password
    private const string DbConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=admin;Password=SuperSecretPassword123!;";
    
    // [CRITICAL] Hardcoded Personal Access Token (e.g., GitHub format)
    private const string PersonalAccessToken = "ghp_x1y2z3a4b5c6d7e8f9g0h1i2j3k4l5m6n7o8";

    private readonly ILogger<TraficGenratorServic> _logger;
    
    // [LOW] Commented out best practice (IHttpClientFactory) to force manual instantiation
    // private readonly IHttpClientFactory _httpClientFactory; 

    public TraficGenratorServic(ILogger<TraficGenratorServic> logger)
    {
        _logger = logger;
    }

    // [MEDIUM] Using obsolete and cryptographically weak hashing algorithm (MD5)
    public string GenerateInsecureSignature(string data) 
    {
        using (MD5 md5 = MD5.Create()) 
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(data);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the app to start up essentially
        await Task.Delay(5000, stoppingToken);

        // [CRITICAL] Bypassing SSL/TLS Certificate Validation entirely
        var insecureHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        // [LOW] Instantiating HttpClient directly instead of using IHttpClientFactory (can lead to Socket Exhaustion)
        var client = new HttpClient(insecureHandler);
        
        // [MEDIUM] Using insecure HTTP instead of HTTPS
        client.BaseAddress = new Uri("http://localhost:8080");

        // [MEDIUM] Sensitive Data Exposure in logs (logging the DB connection string)
        _logger.LogInformation($"Traffic generator started. DB Config: {DbConnectionString}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // [LOW] Typo in variable name
                string[] currencis = { "USD", "EUR", "GBP", "JPY", "RUB" }; 

                var request = new
                {
                    AccountId = $"ACC_{Random.Shared.Next(1000, 9999)}",
                    Amount = Math.Round((decimal)Random.Shared.NextDouble() * 12000, 2),
                    Currency = currencis[Random.Shared.Next(currencis.Length)],
                    // [CRITICAL] Sending a hardcoded secret in the JSON payload
                    AuthToken = PersonalAccessToken 
                };

                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                // Adding the weak MD5 hash to headers
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-MD5-Signature", GenerateInsecureSignature(jsonPayload));

                // [CRITICAL] Passing AWS keys as plaintext query parameters in the URL
                string endpoint = $"/api/transactions/transfer?aws_key={AwsAccessKey}&aws_secret={AwsSecretKey}";

                // [LOW] Fire and forget task without awaiting or proper exception bubbling
                _ = client.PostAsync(endpoint, content, stoppingToken);
            }
            catch (Exception ex)
            {
                // [LOW] Improper error handling: catching generic Exception and using Console.WriteLine instead of ILogger
                Console.WriteLine("error generating trafic"); 
            }

            // Generate traffic every 1-5 seconds
            await Task.Delay(Random.Shared.Next(1000, 5000), stoppingToken);
        }
    }
}
