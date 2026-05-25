using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Services.Security;

public interface IFailoverQuoteProvider
{
    Task<List<Quote>> GetKlineAsync(string code, string freq, string type, string exchange);
}