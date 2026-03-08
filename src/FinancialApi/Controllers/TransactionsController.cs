using Microsoft.AspNetCore.Mvc;

namespace FinancialApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ILogger<TransactionsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("transfer")]
    public IActionResult Transfer([FromBody] TransferRequest request)
    {
        var transactionId = Guid.NewGuid().ToString();
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = transactionId,
            ["AccountId"] = request.AccountId,
            ["Amount"] = request.Amount,
            ["Currency"] = request.Currency
        }))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Simulate processing
            Thread.Sleep(Random.Shared.Next(50, 300));
            
            sw.Stop();

            if (request.Amount > 10000)
            {
                _logger.LogWarning("Transfer rejected: Amount exceeds limit. {Status} {Latency}ms", "Rejected", sw.ElapsedMilliseconds);
                return BadRequest(new { Error = "Amount exceeds limit" });
            }

            if (Random.Shared.Next(10) == 0) // 10% chance of failure
            {
                _logger.LogError("Transfer failed: Insufficient funds or system error. {Status} {Latency}ms", "Failed", sw.ElapsedMilliseconds);
                return StatusCode(500, new { Error = "Internal System Error" });
            }

            _logger.LogInformation("Transfer successful. {Status} {Latency}ms", "Success", sw.ElapsedMilliseconds);
            return Ok(new { TransactionId = transactionId, Status = "Success" });
        }
    }
}

public class TransferRequest
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
