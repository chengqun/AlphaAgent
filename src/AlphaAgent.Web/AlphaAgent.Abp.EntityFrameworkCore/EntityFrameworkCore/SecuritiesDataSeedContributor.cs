using System;
using System.Threading.Tasks;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.InitData;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AlphaAgent.Abp.EntityFrameworkCore;

public class SecuritiesDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<AppSecurity, int> _securityRepository;

    public SecuritiesDataSeedContributor(IRepository<AppSecurity, int> securityRepository)
    {
        _securityRepository = securityRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var dbContext = await _securityRepository.GetDbContextAsync();

        if (await dbContext.Set<AppSecurity>().AnyAsync())
            return;

        var allSecurities = SecuritiesData.All;
        var now = DateTime.UtcNow;
        const int batchSize = 1000;

        for (int i = 0; i < allSecurities.Length; i += batchSize)
        {
            var batch = allSecurities[i..System.Math.Min(i + batchSize, allSecurities.Length)];
            foreach (var s in batch) s.UpdatedAt = now;
            await _securityRepository.InsertManyAsync(batch, autoSave: true);
        }
    }
}
