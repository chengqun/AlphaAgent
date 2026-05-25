using AlphaAgent.Abp.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AlphaAgent.Abp.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpEntityFrameworkCoreModule),
    typeof(AbpApplicationContractsModule)
    )]
public class AbpDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
