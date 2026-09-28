using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;

namespace Slush.Application.Interfaces;

public interface IWalletService
{
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId);
    Task<WalletBalanceDto> DepositAsync(Guid userId, DepositDto request);
    Task<PagedResultDto<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize);
}