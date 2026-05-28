using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public QuoteRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Quote>> GetBySecurityIdAsync(int securityId, string freq, int limit = 100)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Quotes
            .Where(q => q.SecurityId == securityId && q.Freq == freq)
            .OrderByDescending(q => q.Date)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Quote> AddAsync(Quote quote)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Quotes.Add(quote);
        await dbContext.SaveChangesAsync();
        return quote;
    }

    public async Task AddRangeAsync(IEnumerable<Quote> quotes)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.Quotes.AddRangeAsync(quotes);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Quote?> GetLatestBySecurityIdAsync(int securityId, string freq)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.Quotes
            .Where(q => q.SecurityId == securityId && q.Freq == freq)
            .OrderByDescending(q => q.Date)
            .FirstOrDefaultAsync();
    }

    public async Task DeleteBySecurityIdAsync(int securityId, string freq)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var quotes = await dbContext.Quotes.Where(q => q.SecurityId == securityId && q.Freq == freq).ToListAsync();
        dbContext.Quotes.RemoveRange(quotes);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Quote> UpdateAsync(Quote quote)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var existingQuote = await dbContext.Quotes
            .FirstOrDefaultAsync(q => q.SecurityId == quote.SecurityId && q.Date == quote.Date && q.Freq == quote.Freq);

        if (existingQuote != null)
        {
            existingQuote.UpdatePrice(quote.Open, quote.High, quote.Low, quote.Close);
            existingQuote.SetVolume(quote.Volume);
        }
        else
        {
            dbContext.Quotes.Add(quote);
        }

        await dbContext.SaveChangesAsync();
        return quote;
    }
}
