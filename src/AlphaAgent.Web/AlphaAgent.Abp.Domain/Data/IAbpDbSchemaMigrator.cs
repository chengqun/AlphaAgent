using System.Threading.Tasks;

namespace AlphaAgent.Abp.Data;

public interface IAbpDbSchemaMigrator
{
    Task MigrateAsync();
}
