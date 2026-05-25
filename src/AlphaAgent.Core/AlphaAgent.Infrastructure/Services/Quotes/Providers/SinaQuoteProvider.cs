using AlphaAgent.Domain.Entities;
using AlphaAgent.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Quotes.Providers;

public class SinaQuoteProvider : IQuoteProvider
{
    private readonly HttpClient _httpClient;

    public SinaQuoteProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public bool IsSupported(string code, string freq, string type, string exchange)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(freq) || string.IsNullOrWhiteSpace(type))
            return false;

        return type == "期货";
    }

    public async Task<List<Quote>> GetKlineAsync(string code, string freq, string type, string exchange)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var url = BuildRequestUrl(code, freq, type, exchange);
            var response = await _httpClient.GetByteArrayAsync(url);
            var content = Encoding.GetEncoding("GBK").GetString(response);
            return ParseKlineData(content);
        }
        catch
        {
            return new List<Quote>();
        }
    }

    private string BuildRequestUrl(string code, string freq, string type, string exchange)
    {
        string freqParam = freq switch
        {
            "101" => "getDailyKLine",
            _ => "getFewMinLine"
        };
        return $"https://stock2.finance.sina.com.cn/futures/api/jsonp.php/=/InnerFuturesNewService.{freqParam}?symbol={code}&type={freq}";
    }

    private List<Quote> ParseKlineData(string content)
    {
        var quotes = new List<Quote>();
        try
        {
            string jsonData = Regex.Match(content, @"=\((.*)\);").Groups[1].Value;
            var data = JsonSerializer.Deserialize<List<List<object>>>(jsonData);

            if (data != null)
            {
                foreach (var item in data)
                {
                    if (item.Count >= 6)
                    {
                        var date = DateTime.Parse(item[0]?.ToString() ?? "");
                        var open = decimal.TryParse(item[1]?.ToString(), out var o) ? o : 0;
                        var close = decimal.TryParse(item[2]?.ToString(), out var c) ? c : 0;
                        var high = decimal.TryParse(item[3]?.ToString(), out var h) ? h : 0;
                        var low = decimal.TryParse(item[4]?.ToString(), out var l) ? l : 0;
                        var volume = decimal.TryParse(item[5]?.ToString(), out var v) ? v : 0;

                        quotes.Add(new Quote(0, date, open, high, low, close, volume));
                    }
                }
            }
        }
        catch { }

        return quotes;
    }
}