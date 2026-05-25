using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Services.Security;
using AlphaAgent.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Quotes;

public class FailoverQuoteProvider : IFailoverQuoteProvider
{
    private readonly IEnumerable<IQuoteProvider> _providers;
    private readonly ILogger<FailoverQuoteProvider> _logger;

    public FailoverQuoteProvider(IEnumerable<IQuoteProvider> providers, ILogger<FailoverQuoteProvider> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger;
        _logger.LogInformation("Registered providers count: {ProviderCount}", providers.Count());
    }

    public async Task<List<Quote>> GetKlineAsync(string code, string freq, string type, string exchange)
    {
        _logger.LogInformation("GetKlineAsync - Code: {Code}, Freq: {Freq}, Type: {Type}, Exchange: {Exchange}", code, freq, type, exchange);

        var supportedProviders = _providers
            .Where(p => p.IsSupported(code, freq, type, exchange))
            .ToList();

        _logger.LogDebug("Supported providers count: {ProviderCount}", supportedProviders.Count);

        if (!supportedProviders.Any())
        {
            _logger.LogWarning("No supported providers found for Code: {Code}, Freq: {Freq}, Type: {Type}, Exchange: {Exchange}", code, freq, type, exchange);
            return new List<Quote>();
        }

        var random = new Random();
        var shuffledProviders = supportedProviders.OrderBy(_ => random.Next()).ToList();

        foreach (var provider in shuffledProviders)
        {
            try
            {
                var providerName = provider.GetType().Name;
                _logger.LogDebug("Trying provider: {ProviderName}", providerName);
                var result = await provider.GetKlineAsync(code, freq, type, exchange);
                if (result != null && result.Count > 0)
                {
                    _logger.LogInformation("Provider {ProviderName} returned {QuoteCount} quotes", providerName, result.Count);
                    return result;
                }
                else
                {
                    _logger.LogDebug("Provider {ProviderName} returned empty result", providerName);
                }
            }
            catch (Exception ex)
            {
                var providerName = provider.GetType().Name;
                _logger.LogWarning(ex, "Provider {ProviderName} threw exception: {Message}", providerName, ex.Message);
            }
        }

        _logger.LogWarning("All providers failed for Code: {Code}, Freq: {Freq}, Type: {Type}, Exchange: {Exchange}", code, freq, type, exchange);
        return new List<Quote>();
    }
}