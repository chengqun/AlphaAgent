using AlphaAgent.Application.Dtos.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Security;

public interface ISecurityService
{
    Task<SecurityDto> AddSecurityAsync(SecurityDto dto);
    Task<SecurityDto> UpdateOrAddSecurityAsync(SecurityDto dto);
    Task AddSecuritiesAsync(IEnumerable<SecurityDto> dtos);
    Task AddSecuritiesAsync(IEnumerable<SecurityDto> dtos, int batchSize);
    Task<List<SecurityDto>> SearchSecuritiesAsync(string keyword);
    Task<string> CalculateIndicatorsAsync(string keyword, string freq="101", string indicators="MACD", int rowCount = 60);
}
