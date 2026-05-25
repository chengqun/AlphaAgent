using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Domain.Services.Security;
using AlphaAgent.Application.Dtos.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurityEntity = AlphaAgent.Domain.Entities.Security;

namespace AlphaAgent.Application.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly ISecurityManager _securityManager;
    private readonly IAnalysisManager _analysisManager;

    public SecurityService(
        ISecurityManager securityManager,
        IAnalysisManager analysisManager)
    {
        _securityManager = securityManager;
        _analysisManager = analysisManager;
    }

    public async Task<SecurityDto> AddSecurityAsync(SecurityDto dto)
    {
        var security = ToEntity(dto);
        var result = await _securityManager.AddSecurityAsync(security);
        return ToDto(result);
    }

    public async Task<SecurityDto> UpdateOrAddSecurityAsync(SecurityDto dto)
    {
        var security = ToEntity(dto);
        var result = await _securityManager.UpdateOrAddSecurityAsync(security);
        return ToDto(result);
    }

    public async Task AddSecuritiesAsync(IEnumerable<SecurityDto> dtos)
    {
        var securities = dtos.Select(ToEntity);
        await _securityManager.AddSecuritiesAsync(securities);
    }

    public async Task AddSecuritiesAsync(IEnumerable<SecurityDto> dtos, int batchSize)
    {
        var securities = dtos.Select(ToEntity);
        await _securityManager.AddSecuritiesAsync(securities, batchSize);
    }

    public async Task<List<SecurityDto>> SearchSecuritiesAsync(string keyword)
    {
        var securities = await _securityManager.SearchSecuritiesAsync(keyword);
        return securities.Select(ToDto).ToList();
    }

    public async Task<string> CalculateIndicatorsAsync(string keyword, string freq, string indicators, int rowCount = 60)
    {
        var result = await _analysisManager.CalculateAsync(keyword, freq, indicators, rowCount);

        if (result.IsSuccess)
        {
            return result.CsvData ?? string.Empty;
        }

        return result.ErrorMessage ?? "计算失败";
    }

    private static SecurityDto ToDto(SecurityEntity security) => new()
    {
        Id = security.Id,
        Code = security.Code,
        Name = security.Name,
        Type = security.Type,
        Exchange = security.Exchange,
        BaseCode = security.BaseCode,
        UpdatedAt = security.UpdatedAt
    };

    private static SecurityEntity ToEntity(SecurityDto dto) => new(
        dto.Id, dto.Code, dto.Name, dto.Type, dto.Exchange, dto.BaseCode, dto.UpdatedAt);

    }
