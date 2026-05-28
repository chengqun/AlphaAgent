using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public TokenRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Token?> GetByIdAsync(int id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Tokens.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Token?> GetByUsernameAsync(string username)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Tokens.FirstOrDefaultAsync(t => t.Username == username);
    }

    public async Task<Token> AddAsync(Token token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();
        return token;
    }

    public async Task<Token> UpdateAsync(Token token)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Tokens.Update(token);
        await dbContext.SaveChangesAsync();
        return token;
    }

    public async Task DeleteAsync(int id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var token = await dbContext.Tokens.FirstOrDefaultAsync(t => t.Id == id);
        if (token != null)
        {
            dbContext.Tokens.Remove(token);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<Token?> GetActiveAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Tokens.FirstOrDefaultAsync(t => t.IsActive);
    }

}
