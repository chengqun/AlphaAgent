using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class SecurityRepository : ISecurityRepository
{
    private readonly SharesDbContext _context;

    public SecurityRepository(SharesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Security>> GetAllAsync()
    {
        return await _context.Securities.ToListAsync();
    }

    public async Task<Security?> GetByIdAsync(int id)
    {
        return await _context.Securities.FindAsync(id);
    }

    public async Task<Security?> GetByCodeAsync(string code)
    {
        return await _context.Securities.FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<List<Security>> GetByExchangeAsync(string exchange)
    {
        return await _context.Securities.Where(s => s.Exchange == exchange).ToListAsync();
    }

    public async Task<List<Security>> GetByTypeAsync(string type)
    {
        return await _context.Securities.Where(s => s.Type == type).ToListAsync();
    }

    public async Task<bool> ExistsAsync(string code)
    {
        return await _context.Securities.AnyAsync(s => s.Code == code);
    }

    public async Task<Security> AddAsync(Security security)
    {
        var existingSecurity = await _context.Securities.FirstOrDefaultAsync(s => s.Code == security.Code && s.Type == security.Type);

        if (existingSecurity != null)
        {
            existingSecurity.UpdateFrom(security);
            await _context.SaveChangesAsync();
            return existingSecurity;
        }
        else
        {
            _context.Securities.Add(security);
            await _context.SaveChangesAsync();
            return security;
        }
    }

    public async Task<Security> UpdateAsync(Security security)
    {
        _context.Securities.Update(security);
        await _context.SaveChangesAsync();
        return security;
    }

    public async Task DeleteAsync(int id)
    {
        var security = await _context.Securities.FindAsync(id);
        if (security != null)
        {
            _context.Securities.Remove(security);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Security>> SearchAsync(string keyword)
    {
        return await _context.Securities
            .Where(s => s.Code.Contains(keyword) || s.Name.Contains(keyword))
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Security> securities)
    {
        foreach (var security in securities)
        {
            var existingSecurity = await _context.Securities.FirstOrDefaultAsync(s => s.Code == security.Code && s.Type == security.Type);

            if (existingSecurity != null)
            {
                existingSecurity.UpdateFrom(security);
            }
            else
            {
                _context.Securities.Add(security);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Security> securities, int batchSize = 1000)
    {
        var securityList = securities.ToList();
        for (int i = 0; i < securityList.Count; i += batchSize)
        {
            var batch = securityList.Skip(i).Take(batchSize);

            foreach (var security in batch)
            {
                var existingSecurity = await _context.Securities.FirstOrDefaultAsync(s => s.Code == security.Code && s.Type == security.Type);

                if (existingSecurity != null)
                {
                    existingSecurity.UpdateFrom(security);
                }
                else
                {
                    _context.Securities.Add(security);
                }
            }

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }
    }
}