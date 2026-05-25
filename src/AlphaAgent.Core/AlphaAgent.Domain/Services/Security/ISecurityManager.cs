using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityEntity = AlphaAgent.Domain.Entities.Security;

namespace AlphaAgent.Domain.Services.Security;

public interface ISecurityManager
{
    Task<SecurityEntity> AddSecurityAsync(SecurityEntity security);
    Task<SecurityEntity> UpdateOrAddSecurityAsync(SecurityEntity security);
    Task AddSecuritiesAsync(IEnumerable<SecurityEntity> securities);
    Task AddSecuritiesAsync(IEnumerable<SecurityEntity> securities, int batchSize);
    Task<List<SecurityEntity>> SearchSecuritiesAsync(string keyword);
    Task<SecurityEntity?> GetSecurityByIdAsync(int id);
    Task<SecurityEntity?> GetSecurityByCodeAsync(string code);
    Task<List<SecurityEntity>> GetSecuritiesByExchangeAsync(string exchange);
    Task<List<SecurityEntity>> GetSecuritiesByTypeAsync(string type);
    Task<bool> ExistsAsync(string code);
    Task UpdateSecurityAsync(SecurityEntity security);
    Task DeleteSecurityAsync(int id);
}
