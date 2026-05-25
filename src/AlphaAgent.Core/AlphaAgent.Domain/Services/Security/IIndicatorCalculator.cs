using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Services.Security;

public interface IIndicatorCalculator
{
    Task<string> CalculateAsCsvAsync(List<Quote> quotes, string indicators, int rowCount = 60);
}