using AlphaAgent.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Interfaces;

public interface IQuoteRepository
{
    Task<List<Quote>> GetBySecurityIdAsync(int securityId, string freq, int limit = 100);
    Task<Quote> AddAsync(Quote quote);
    Task AddRangeAsync(IEnumerable<Quote> quotes);
    Task<Quote?> GetLatestBySecurityIdAsync(int securityId, string freq);
    Task DeleteBySecurityIdAsync(int securityId, string freq);
    Task<Quote> UpdateAsync(Quote quote);
}