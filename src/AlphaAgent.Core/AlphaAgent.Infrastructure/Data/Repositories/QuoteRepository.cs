using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly SharesDbContext _context;

    public QuoteRepository(SharesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Quote>> GetBySecurityIdAsync(int securityId, string freq, int limit = 100)
    {
        return await _context.Quotes
            .Where(q => q.SecurityId == securityId && q.Freq == freq)
            .OrderByDescending(q => q.Date)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Quote> AddAsync(Quote quote)
    {
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();
        return quote;
    }

    public async Task AddRangeAsync(IEnumerable<Quote> quotes)
    {
        await _context.Quotes.AddRangeAsync(quotes);
        await _context.SaveChangesAsync();
    }

    public async Task<Quote?> GetLatestBySecurityIdAsync(int securityId, string freq)
    {
        return await _context.Quotes
            .Where(q => q.SecurityId == securityId && q.Freq == freq)
            .OrderByDescending(q => q.Date)
            .FirstOrDefaultAsync();
    }

    public async Task DeleteBySecurityIdAsync(int securityId, string freq)
    {
        var quotes = await _context.Quotes.Where(q => q.SecurityId == securityId && q.Freq == freq).ToListAsync();
        _context.Quotes.RemoveRange(quotes);
        await _context.SaveChangesAsync();
    }

    public async Task<Quote> UpdateAsync(Quote quote)
    {
        var existingQuote = await _context.Quotes
            .FirstOrDefaultAsync(q => q.SecurityId == quote.SecurityId && q.Date == quote.Date && q.Freq == quote.Freq);

        if (existingQuote != null)
        {
            existingQuote.UpdatePrice(quote.Open, quote.High, quote.Low, quote.Close);
            existingQuote.SetVolume(quote.Volume);
        }
        else
        {
            _context.Quotes.Add(quote);
        }

        await _context.SaveChangesAsync();
        return quote;
    }
}