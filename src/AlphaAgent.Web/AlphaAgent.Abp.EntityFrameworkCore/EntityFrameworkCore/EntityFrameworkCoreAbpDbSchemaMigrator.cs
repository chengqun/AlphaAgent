using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AlphaAgent.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace AlphaAgent.Abp.EntityFrameworkCore;

public class EntityFrameworkCoreAbpDbSchemaMigrator
    : IAbpDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAbpDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the AbpDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AbpDbContext>()
            .Database
            .MigrateAsync();
    }
}
