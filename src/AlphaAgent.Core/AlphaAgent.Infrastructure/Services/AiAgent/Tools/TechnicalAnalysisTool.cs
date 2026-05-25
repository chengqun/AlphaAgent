using AlphaAgent.Domain.Services.Security;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.AiAgent.Tools;

public class TechnicalAnalysisTool
{
    public class Input
    {
        [Description("股票代码或名称")]
        public string Keyword { get; set; } = string.Empty;

        [Description("K线周期代码: 101=日K, 1=1分钟, 5=5分钟, 15=15分钟, 30=30分钟, 60=60分钟")]
        public string Freq { get; set; } = "101";

        [Description("要计算的指标，用逗号分隔: SMA, EMA, RSI, MACD, BB, SAR, KDJ, ADX")]
        public string Indicators { get; set; } = "MACD";

        [Description("获取的数据行数, 默认60")]
        public int RowCount { get; set; } = 60;
    }

    public class Output
    {
        public string Result { get; set; } = string.Empty;
    }

    private readonly IAnalysisManager _analysisManager;

    public TechnicalAnalysisTool(IAnalysisManager analysisManager)
    {
        _analysisManager = analysisManager;
    }

    [Description("计算股票的技术指标，包括SMA、EMA、RSI、MACD、BB、SAR、KDJ、ADX等。当用户要求分析技术指标时调用此工具。")]
    public async Task<Output> CalculateIndicators(string keyword, string freq = "101", string indicators = "MACD", int rowCount = 60, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _analysisManager.CalculateAsync(keyword, freq, indicators, rowCount);

            return new Output
            {
                Result = result.IsSuccess
                    ? result.CsvData ?? "计算成功但无数据"
                    : $"计算失败: {result.ErrorMessage ?? "未知错误"}"
            };
        }
        catch (System.Exception ex)
        {
            return new Output { Result = $"技术分析失败: {ex.Message}" };
        }
    }
}
