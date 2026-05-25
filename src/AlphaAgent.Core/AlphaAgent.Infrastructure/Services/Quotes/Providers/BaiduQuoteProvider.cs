using AlphaAgent.Domain.Entities;
using AlphaAgent.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Quotes.Providers;

public class BaiduQuoteProvider : IQuoteProvider
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://finance.pae.baidu.com/vapi/v1/getquotation";
    private const string DefaultMarketType = "us";

    private readonly Dictionary<string, string> _frequencyMap = new()
    {
        {"101", "day"},
    };

    public BaiduQuoteProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public bool IsSupported(string code, string freq, string type, string exchange)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(freq) || string.IsNullOrWhiteSpace(type))
            return false;

        return _frequencyMap.ContainsKey(freq)
              &&
               (type == "汇率" || type == "指数" || type == "期货" || type == "股票"
               || type == "美股指数" || type == "虚拟币"
               );
    }

    public async Task<List<Quote>> GetKlineAsync(string code, string freq, string type, string exchange)
    {
        if (!IsSupported(code, freq, type, exchange))
            return new List<Quote>();

        if (type == "期货")
        {
            bool hasMonthNumber = Regex.IsMatch(code, @"0[1-9]|1[0-2]");
            if (!hasMonthNumber)
            {
                code = code + "888";
            }
        }

        try
        {
            var url = BuildRequestUrl(code, freq, type, exchange);
            var response = await _httpClient.GetStringAsync(url);
            return ParseKlineData(response);
        }
        catch
        {
            return new List<Quote>();
        }
    }

    private string BuildRequestUrl(string code, string freq, string type, string exchange)
    {
        string groupParam = type switch
        {
            "汇率" => "huilv_kline",
            "虚拟币" => "huilv_kline",
            "指数" => "quotation_index_kline",
            "美股指数" => "quotation_index_kline",
            "期货" => "quotation_futures_kline",
            "股票" => "quotation_kline_ab",
            _ => "quotation_kline_ab"
        };

        string ktype = _frequencyMap.TryGetValue(freq, out var val) ? val : "day";

        return $"{BaseUrl}?group={groupParam}&ktype={ktype}&code={code}&market_type={DefaultMarketType}";
    }

    private List<Quote> ParseKlineData(string response)
    {
        var quotes = new List<Quote>();
        try
        {
            using var doc = JsonDocument.Parse(response);

            if (!doc.RootElement.TryGetProperty("Result", out var resultElement) ||
                !resultElement.TryGetProperty("newMarketData", out var newMarketDataElement) ||
                !newMarketDataElement.TryGetProperty("keys", out var keysElement) ||
                !newMarketDataElement.TryGetProperty("marketData", out var marketDataElement))
            {
                return new List<Quote>();
            }

            var keys = keysElement.EnumerateArray()
                .Select(k => k.GetString())
                .ToList();

            int openIndex = keys.IndexOf("open");
            int closeIndex = keys.IndexOf("close");
            int highIndex = keys.IndexOf("high");
            int lowIndex = keys.IndexOf("low");
            int timeIndex = keys.IndexOf("time");
            int volumeIndex = keys.IndexOf("volume");

            if (openIndex == -1 || closeIndex == -1 || highIndex == -1 || lowIndex == -1 || timeIndex == -1)
            {
                return new List<Quote>();
            }

            var marketDataStr = marketDataElement.GetString();
            if (string.IsNullOrEmpty(marketDataStr))
            {
                return new List<Quote>();
            }

            foreach (var item in marketDataStr.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(item)) continue;

                var parts = item.Split(',');
                int maxIndex = new[] { openIndex, closeIndex, highIndex, lowIndex, timeIndex, volumeIndex }.Max();
                if (parts.Length <= maxIndex) continue;

                if (!DateTime.TryParse($"{parts[timeIndex]} 15:00", out DateTime date)) continue;

                quotes.Add(new Quote(
                    0,
                    date,
                    decimal.TryParse(parts[openIndex], out var open) ? open : 0,
                    decimal.TryParse(parts[highIndex], out var high) ? high : 0,
                    decimal.TryParse(parts[lowIndex], out var low) ? low : 0,
                    decimal.TryParse(parts[closeIndex], out var close) ? close : 0,
                    volumeIndex != -1 && decimal.TryParse(parts[volumeIndex], out var volume) ? volume : 0
                ));
            }
        }
        catch { }

        return quotes;
    }
}