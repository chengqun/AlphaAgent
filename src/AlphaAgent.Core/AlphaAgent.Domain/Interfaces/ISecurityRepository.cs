using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityEntity = AlphaAgent.Domain.Entities.Security;

namespace AlphaAgent.Domain.Interfaces;

public interface ISecurityRepository
{
    Task<List<SecurityEntity>> GetAllAsync();
    Task<SecurityEntity?> GetByIdAsync(int id);
    Task<SecurityEntity?> GetByCodeAsync(string code);
    Task<List<SecurityEntity>> GetByExchangeAsync(string exchange);
    Task<List<SecurityEntity>> GetByTypeAsync(string type);
    Task<bool> ExistsAsync(string code);
    Task<SecurityEntity> AddAsync(SecurityEntity security);
    Task<SecurityEntity> UpdateAsync(SecurityEntity security);
    Task DeleteAsync(int id);
    Task<List<SecurityEntity>> SearchAsync(string keyword);
    Task AddRangeAsync(IEnumerable<SecurityEntity> securities);
    Task AddRangeAsync(IEnumerable<SecurityEntity> securities, int batchSize);
}
