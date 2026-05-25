using AlphaAgent.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Interfaces;

public interface IQuoteProvider
{
    bool IsSupported(string code, string freq, string type, string exchange);
    Task<List<Quote>> GetKlineAsync(string code, string freq, string type, string exchange);
}
