using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetBalance()
    {
        var balance = await _walletService.GetBalanceAsync(GetUserId());
        return Ok(balance);
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositDto request)
    {
        try
        {
            var result = await _walletService.DepositAsync(GetUserId(), request);
            return Ok(new { message = "Deposit successful", newBalance = result.Balance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var transactions = await _walletService.GetTransactionsAsync(GetUserId(), page, pageSize);
        return Ok(transactions);
    }
}