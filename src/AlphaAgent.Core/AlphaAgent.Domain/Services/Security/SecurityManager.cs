using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurityEntity = AlphaAgent.Domain.Entities.Security;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Domain.Services.Security;

public class SecurityManager : ISecurityManager
{
    private readonly ISecurityRepository _securityRepository;

    public SecurityManager(ISecurityRepository securityRepository)
    {
        _securityRepository = securityRepository;
    }

    public async Task<SecurityEntity> AddSecurityAsync(SecurityEntity security)
    {
        if (security == null)
            throw new ArgumentNullException(nameof(security));

        ValidateSecurity(security);

        var existing = await _securityRepository.GetByCodeAsync(security.Code);
        if (existing != null)
            throw new InvalidOperationException($"证券代码 {security.Code} 已存在");

        await _securityRepository.AddAsync(security);
        return security;
    }

    public async Task<SecurityEntity> UpdateOrAddSecurityAsync(SecurityEntity security)
    {
        if (security == null)
            throw new ArgumentNullException(nameof(security));

        ValidateSecurity(security);

        var existing = await _securityRepository.GetByCodeAsync(security.Code);
        if (existing != null)
        {
            // 使用实体的 UpdateFrom 方法更新现有记录
            existing.UpdateFrom(security);
            await _securityRepository.UpdateAsync(existing);
            return existing;
        }

        await _securityRepository.AddAsync(security);
        return security;
    }

    public async Task AddSecuritiesAsync(IEnumerable<SecurityEntity> securities)
    {
        if (securities == null)
            throw new ArgumentNullException(nameof(securities));

        var securityList = securities.ToList();
        ValidateSecurities(securityList);

        await _securityRepository.AddRangeAsync(securityList);
    }

    public async Task AddSecuritiesAsync(IEnumerable<SecurityEntity> securities, int batchSize)
    {
        if (securities == null)
            throw new ArgumentNullException(nameof(securities));

        var securityList = securities.ToList();
        ValidateSecurities(securityList);

        await _securityRepository.AddRangeAsync(securityList, batchSize);
    }

    public async Task<List<SecurityEntity>> SearchSecuritiesAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await _securityRepository.GetAllAsync();

        return await _securityRepository.SearchAsync(keyword);
    }

    public async Task<SecurityEntity?> GetSecurityByIdAsync(int id)
    {
        return await _securityRepository.GetByIdAsync(id);
    }

    public async Task<SecurityEntity?> GetSecurityByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        return await _securityRepository.GetByCodeAsync(code);
    }

    public async Task<List<SecurityEntity>> GetSecuritiesByExchangeAsync(string exchange)
    {
        if (string.IsNullOrWhiteSpace(exchange))
            throw new ArgumentNullException(nameof(exchange));

        return await _securityRepository.GetByExchangeAsync(exchange);
    }

    public async Task<List<SecurityEntity>> GetSecuritiesByTypeAsync(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentNullException(nameof(type));

        return await _securityRepository.GetByTypeAsync(type);
    }

    public async Task<bool> ExistsAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        return await _securityRepository.ExistsAsync(code);
    }

    public async Task UpdateSecurityAsync(SecurityEntity security)
    {
        if (security == null)
            throw new ArgumentNullException(nameof(security));

        ValidateSecurity(security);

        var existing = await _securityRepository.GetByIdAsync(security.Id);
        if (existing == null)
            throw new InvalidOperationException($"未找到ID为 {security.Id} 的证券");

        await _securityRepository.UpdateAsync(security);
    }

    public async Task DeleteSecurityAsync(int id)
    {
        var security = await _securityRepository.GetByIdAsync(id);
        if (security == null)
            throw new InvalidOperationException($"未找到ID为 {id} 的证券");

        await _securityRepository.DeleteAsync(id);
    }

    private void ValidateSecurity(SecurityEntity security)
    {
        if (string.IsNullOrWhiteSpace(security.Code))
            throw new ArgumentException("证券代码不能为空", nameof(security.Code));

        if (string.IsNullOrWhiteSpace(security.Name))
            throw new ArgumentException("证券名称不能为空", nameof(security.Name));

        if (string.IsNullOrWhiteSpace(security.Type))
            throw new ArgumentException("证券类型不能为空", nameof(security.Type));

        if (string.IsNullOrWhiteSpace(security.Exchange))
            throw new ArgumentException("交易所不能为空", nameof(security.Exchange));

        if (!IsValidSecurityCode(security.Code, security.Exchange))
            throw new ArgumentException($"无效的证券代码格式: {security.Code}", nameof(security.Code));
    }

    private void ValidateSecurities(List<SecurityEntity> securities)
    {
        foreach (var security in securities)
        {
            ValidateSecurity(security);
        }
    }

    private bool IsValidSecurityCode(string code, string exchange)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(exchange))
            return false;

        code = code.Trim();

        // 允许纯数字（股票）或字母+数字组合（期货）
        if (long.TryParse(code, out _))
            return true;

        // 期货代码格式：字母前缀 + 数字（如 lh2607, IF2607）
        return code.Length >= 4 && code.Length <= 10 && 
               code.All(c => char.IsLetterOrDigit(c));
    }
}
