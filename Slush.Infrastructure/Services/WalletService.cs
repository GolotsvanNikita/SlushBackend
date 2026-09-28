using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _uow;

    public WalletService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        return new WalletBalanceDto { Balance = user?.Balance ?? 0 };
    }

    public async Task<WalletBalanceDto> DepositAsync(Guid userId, DepositDto request)
    {
        if (request.Amount <= 0) throw new Exception("Amount must be greater than zero.");

        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        user.Balance += request.Amount;
        userRepo.Update(user);

        var transaction = new WalletTransaction
        {
            UserId = userId,
            Amount = request.Amount,
            Title = "Поповнення балансу",
            Type = TransactionType.Deposit,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Repository<WalletTransaction>().AddAsync(transaction);

        await _uow.SaveChangesAsync();

        return new WalletBalanceDto { Balance = user.Balance };
    }

    public async Task<PagedResultDto<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Repository<WalletTransaction>().AsQueryable()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id.ToString(),
                Amount = t.Amount,
                Title = t.Title,
                Type = t.Type.ToString(),
                CreatedAt = t.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToListAsync();

        return new PagedResultDto<WalletTransactionDto>(items, totalCount, page, pageSize);
    }
}