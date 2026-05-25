using System.Threading.Tasks;

namespace AlphaAgent.Domain.Services.Security;

public interface IAnalysisManager
{
    Task<TechnicalAnalysisResult> CalculateAsync(
        string keyword,
        string freq,
        string indicators,
        int rowCount = 60);
}