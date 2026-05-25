using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Services.Securities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace AlphaAgent.Abp.Domain.Services.Securities
{

public class SecurityManager : DomainService, ISecurityManager
{
    private readonly IRepository<AppSecurity, int> _securityRepository;

    public SecurityManager(IRepository<AppSecurity, int> securityRepository)
    {
            _securityRepository = securityRepository;
    }

    public async Task<AppSecurity?> FindAsync(string query)
    {
        var securityList = await _securityRepository.GetListAsync();
        return securityList.FirstOrDefault(s =>
            s.Code == query || s.Name.Contains(query));
    }

    public async Task<AppSecurity?> GetByIdAsync(int id)
    {
        return await _securityRepository.GetAsync(id, includeDetails: false);
    }

    public async Task<List<AppSecurity>> SearchAsync(string query)
    {
        var securityList = await _securityRepository.GetListAsync();
        return securityList.Where(s =>
            s.Code.Contains(query) || s.Name.Contains(query))
            .Take(20)
            .ToList();
    }

    public async Task<List<AppSecurity>> GetAllAsync()
    {
        return await _securityRepository.GetListAsync();
    }

    public async Task<AppSecurity?> GetByCodeAndTypeAsync(string code, string type)
    {
        var list = await _securityRepository.GetListAsync(s => s.Code == code && s.Type == type);
        return list.FirstOrDefault();
    }

    public async Task<(int added, int updated)> UpsertRangeAsync(List<AppSecurity> securities)
    {
        var codes = securities.Select(s => s.Code).Distinct().ToList();
        var types = securities.Select(s => s.Type).Distinct().ToList();

        var existing = await _securityRepository.GetListAsync(s => codes.Contains(s.Code) && types.Contains(s.Type));

        var existingDict = existing.ToDictionary(s => $"{s.Code}|{s.Type}");
        var toInsert = new List<AppSecurity>();
        var updated = 0;

        foreach (var security in securities)
        {
            var key = $"{security.Code}|{security.Type}";
            if (existingDict.TryGetValue(key, out var existingEntity))
            {
                existingEntity.Name = security.Name;
                existingEntity.Exchange = security.Exchange;
                existingEntity.BaseCode = security.BaseCode;
                existingEntity.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            else
            {
                security.UpdatedAt = DateTime.UtcNow;
                toInsert.Add(security);
            }
        }

        if (toInsert.Count > 0)
        {
            await _securityRepository.InsertManyAsync(toInsert, autoSave: true);
        }

        if (updated > 0)
        {
            await _securityRepository.UpdateManyAsync(existing, autoSave: true);
        }

        return (toInsert.Count, updated);
    }

    public async Task<List<AppSecurity>> GetUpdatedAfterAsync(DateTime after)
    {
        return await _securityRepository.GetListAsync(s => s.UpdatedAt > after);
    }
}
}