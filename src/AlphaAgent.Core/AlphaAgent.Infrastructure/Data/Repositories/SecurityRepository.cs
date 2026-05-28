using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class SecurityRepository : ISecurityRepository
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public SecurityRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Security>> GetAllAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities.ToListAsync();
    }

    public async Task<Security?> GetByIdAsync(int id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities.FindAsync(id);
    }

    public async Task<Security?> GetByCodeAsync(string code)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities.FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<List<Security>> GetByExchangeAsync(string exchange)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities.Where(s => s.Exchange == exchange).ToListAsync();
    }

    public async Task<List<Security>> GetByTypeAsync(string type)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities.Where(s => s.Type == type).ToListAsync();
    }

    public async Task<bool> ExistsAsync(string code)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities.AnyAsync(s => s.Code == code);
    }

    public async Task<Security> AddAsync(Security security)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var existingSecurity = await dbContext.Securities.FirstOrDefaultAsync(s => s.Code == security.Code && s.Type == security.Type);

        if (existingSecurity != null)
        {
            existingSecurity.UpdateFrom(security);
            await dbContext.SaveChangesAsync();
            return existingSecurity;
        }
        else
        {
            dbContext.Securities.Add(security);
            await dbContext.SaveChangesAsync();
            return security;
        }
    }

    public async Task<Security> UpdateAsync(Security security)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Securities.Update(security);
        await dbContext.SaveChangesAsync();
        return security;
    }

    public async Task DeleteAsync(int id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var security = await dbContext.Securities.FindAsync(id);
        if (security != null)
        {
            dbContext.Securities.Remove(security);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<Security>> SearchAsync(string keyword)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Securities
            .Where(s => s.Code.Contains(keyword) || s.Name.Contains(keyword))
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Security> securities)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        foreach (var security in securities)
        {
            var existingSecurity = await dbContext.Securities.FirstOrDefaultAsync(s => s.Code == security.Code && s.Type == security.Type);

            if (existingSecurity != null)
            {
                existingSecurity.UpdateFrom(security);
            }
            else
            {
                dbContext.Securities.Add(security);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Security> securities, int batchSize = 1000)
    {
        var securityList = securities.ToList();
        for (int i = 0; i < securityList.Count; i += batchSize)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var batch = securityList.Skip(i).Take(batchSize);

            foreach (var security in batch)
            {
                var existingSecurity = await dbContext.Securities.FirstOrDefaultAsync(s => s.Code == security.Code && s.Type == security.Type);

                if (existingSecurity != null)
                {
                    existingSecurity.UpdateFrom(security);
                }
                else
                {
                    dbContext.Securities.Add(security);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
