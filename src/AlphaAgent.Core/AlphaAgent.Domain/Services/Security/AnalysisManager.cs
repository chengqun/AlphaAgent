using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using SecurityEntity = AlphaAgent.Domain.Entities.Security;

namespace AlphaAgent.Domain.Services.Security;

public class AnalysisManager : IAnalysisManager
{
    private readonly ISecurityRepository _securityRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IFailoverQuoteProvider _quoteProvider;
    private readonly IIndicatorCalculator _indicatorCalculator;

    public AnalysisManager(
        ISecurityRepository securityRepository,
        IQuoteRepository quoteRepository,
        IFailoverQuoteProvider quoteProvider,
        IIndicatorCalculator indicatorCalculator)
    {
        _securityRepository = securityRepository;
        _quoteRepository = quoteRepository;
        _quoteProvider = quoteProvider;
        _indicatorCalculator = indicatorCalculator;
    }

    public async Task<TechnicalAnalysisResult> CalculateAsync(
        string keyword,
        string freq,
        string indicators,
        int rowCount = 60)
    {
        var securities = await _securityRepository.SearchAsync(keyword);
        if (securities.Count == 0)
        {
            return TechnicalAnalysisResult.Failure("未找到匹配的证券");
        }

        var security = securities[0];
        var existingQuotes = await _quoteRepository.GetBySecurityIdAsync(security.Id, freq, 1000);
        List<Quote> quotes;

        if (existingQuotes.Count == 0)
        {
            quotes = await _quoteProvider.GetKlineAsync(security.Code, freq, security.Type, security.Exchange);
            if (quotes.Count > 0)
            {
                foreach (var quote in quotes)
                {
                    quote.SetSecurityId(security.Id);
                    quote.SetFreq(freq);
                }
                await _quoteRepository.AddRangeAsync(quotes);
            }
            else
            {
                return TechnicalAnalysisResult.Failure($"无法获取证券 {security.Code} 的行情数据，请检查网络连接");
            }
        }
        else
        {
            quotes = existingQuotes;
        }

        if (quotes.Count < 5)
        {
            return TechnicalAnalysisResult.Failure($"行情数据不足（仅{quotes.Count}条），无法计算指标");
        }

        var csv = await _indicatorCalculator.CalculateAsCsvAsync(quotes, indicators, rowCount);
        return TechnicalAnalysisResult.Success(csv, quotes.Count, security);
    }
}
