using SecurityEntity = AlphaAgent.Domain.Entities.Security;

namespace AlphaAgent.Domain.Services.Security;

public class TechnicalAnalysisResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? CsvData { get; private set; }
    public int QuoteCount { get; private set; }
    public SecurityEntity? Security { get; private set; }

    private TechnicalAnalysisResult() { }

    public static TechnicalAnalysisResult Success(string csvData, int quoteCount, SecurityEntity security)
    {
        return new TechnicalAnalysisResult
        {
            IsSuccess = true,
            CsvData = csvData,
            QuoteCount = quoteCount,
            Security = security
        };
    }

    public static TechnicalAnalysisResult Failure(string errorMessage)
    {
        return new TechnicalAnalysisResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
