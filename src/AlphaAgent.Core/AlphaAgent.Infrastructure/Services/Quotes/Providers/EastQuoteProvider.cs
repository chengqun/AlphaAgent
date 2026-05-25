using AlphaAgent.Domain.Entities;
using AlphaAgent.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Quotes.Providers;

public class EastQuoteProvider : IQuoteProvider
{
    private readonly HttpClient _httpClient;

    public EastQuoteProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public bool IsSupported(string code, string freq, string type, string exchange)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(freq) || string.IsNullOrWhiteSpace(type))
            return false;

        return (type == "股票"
           || (type == "期货" && freq == "101")
           || (type == "指数")
            );
    }

    public async Task<List<Quote>> GetKlineAsync(string code, string freq, string type, string exchange)
    {
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
        var endDate = DateTime.Now.ToString("yyyyMMdd");
        var pureCode = code.Contains('.') ? code.Split('.')[0] : code;
        var secid = exchange + "." + pureCode;
        return $"https://push2his.eastmoney.com/api/qt/stock/kline/get?secid={secid}&fields1=f1%2Cf2%2Cf3%2Cf4%2Cf5%2Cf6&fields2=f51%2Cf52%2Cf53%2Cf54%2Cf55%2Cf56%2Cf57%2Cf58%2Cf59%2Cf60%2Cf61&klt={freq}&fqt=1&end={endDate}&lmt=1488";
    }

    private List<Quote> ParseKlineData(string response)
    {
        var quotes = new List<Quote>();
        try
        {
            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind == JsonValueKind.Null ||
                !dataElement.TryGetProperty("klines", out var klines))
            {
                return new List<Quote>();
            }

            foreach (var k in klines.EnumerateArray())
            {
                var str = k.GetString();
                if (string.IsNullOrEmpty(str)) continue;
                var parts = str.Split(',');
                if (parts.Length < 6) continue;
                var dateStr = parts[0];

                DateTime date;
                if (dateStr.Length == 10)
                {
                    if (!DateTime.TryParse($"{dateStr} 15:00", out date))
                        continue;
                }
                else
                {
                    if (!DateTime.TryParse(dateStr, out date))
                        continue;
                }

                quotes.Add(new Quote(
                    0,
                    date,
                    decimal.TryParse(parts[1], out var open) ? open : 0,
                    decimal.TryParse(parts[3], out var high) ? high : 0,
                    decimal.TryParse(parts[4], out var low) ? low : 0,
                    decimal.TryParse(parts[2], out var close) ? close : 0,
                    decimal.TryParse(parts[5], out var volume) ? volume : 0
                ));
            }
        }
        catch { }

        return quotes;
    }
}