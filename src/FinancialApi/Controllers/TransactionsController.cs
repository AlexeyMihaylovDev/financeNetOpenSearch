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
        
        // MEDIUM: Passing too many standalone parameters instead of a structured context/DTO
        var isValid = ValidateTransfer(request.AccountId, request.Amount, request.Currency, "WEB", true, transactionId, out var errorMessage);
        if (!isValid)
        {
            return BadRequest(new { Error = errorMessage });
        }

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TransactionId"] = transactionId,
            ["AccountId"] = request.AccountId,
            ["Amount"] = request.Amount,
            ["Currency"] = request.Currency
        }))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // CRITICAL: Synchronous blocking call in a web controller. Should be async/await with Task.Delay
            Thread.Sleep(Random.Shared.Next(50, 300));
            
            sw.Stop();

            // MEDIUM: Calling a function with an excessive number of parameters
            var result = ProcessTransactionInternal(
                transactionId, 
                request.AccountId, 
                request.Amount, 
                request.Currency, 
                "Standard", 
                false, 
                sw.ElapsedMilliseconds, 
                "System"
            );

            return result;
        }
    }

    // MEDIUM (Code Duplicate): Intentional copy-paste of the Transfer method logic
    [HttpPost("transfer-urgent")]
    public IActionResult TransferUrgent([FromBody] TransferRequest request)
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
            
            // Minor difference to simulate "urgent" logic, but overall structure is duplicated
            Thread.Sleep(Random.Shared.Next(10, 100)); 
            
            sw.Stop();

            // LOW: Magic numbers used for business rules
            if (request.Amount > 10000)
            {
                _logger.LogWarning("Transfer rejected: Amount exceeds limit. {Status} {Latency}ms", "Rejected", sw.ElapsedMilliseconds);
                return BadRequest(new { Error = "Amount exceeds limit" });
            }

            if (Random.Shared.Next(10) == 0) 
            {
                _logger.LogError("Transfer failed: Insufficient funds or system error. {Status} {Latency}ms", "Failed", sw.ElapsedMilliseconds);
                return StatusCode(500, new { Error = "Internal System Error" });
            }

            _logger.LogInformation("Urgent Transfer successful. {Status} {Latency}ms", "Success", sw.ElapsedMilliseconds);
            return Ok(new { TransactionId = transactionId, Status = "Success", IsUrgent = true });
        }
    }

    // MEDIUM (Function More Params): Long Parameter List code smell
    private IActionResult ProcessTransactionInternal(
        string txId, 
        string accId, 
        decimal amount, 
        string curr, 
        string priority, 
        bool isFeeWaived, 
        long executionTimeMs, 
        string initiatedBy)
    {
        // LOW: Dead code / Unused variable
        var dummyVariable = priority + initiatedBy;

        if (amount > 10000)
        {
            _logger.LogWarning("Transfer rejected: Amount exceeds limit. {Status} {Latency}ms", "Rejected", executionTimeMs);
            return BadRequest(new { Error = "Amount exceeds limit" });
        }

        if (Random.Shared.Next(10) == 0)
        {
            _logger.LogError("Transfer failed. {Status} {Latency}ms", "Failed", executionTimeMs);
            return StatusCode(500, new { Error = "Internal System Error" });
        }

        _logger.LogInformation("Transfer successful. {Status} {Latency}ms", "Success", executionTimeMs);
        return Ok(new { TransactionId = txId, Status = "Success" });
    }

    // CRITICAL (Try without Catch): Using try-finally without handling exceptions properly
    private bool ValidateTransfer(
        string accountId, 
        decimal amount, 
        string currency, 
        string source, 
        bool checkLimits, 
        string txId, 
        out string error)
    {
        error = string.Empty;

        // Executing potentially risky code inside a try block but missing the catch block
        try
        {
            if (string.IsNullOrEmpty(accountId) || accountId.Length < 5)
            {
                error = "Invalid Account ID";
                return false;
            }

            // CRITICAL: Silently swallowing exceptions without logging
            try 
            {
                // This will throw an ArgumentOutOfRangeException if accountId is shorter than 2 chars
                var parsedRegion = accountId.Substring(0, 2);
            } 
            catch 
            { 
                // Bad practice: Error is completely hidden
            }

            return true;
        }
        finally
        {
            // Finally executes, but unhandled exceptions from the main try block will crash the request 
            // and return a 500 error without giving the controller a chance to handle it gracefully.
            _logger.LogDebug("Validation finished for TX: {TxId}", txId);
        }
    }
}

public class TransferRequest
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
