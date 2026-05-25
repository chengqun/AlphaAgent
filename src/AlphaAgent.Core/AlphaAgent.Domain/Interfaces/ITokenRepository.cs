using AlphaAgent.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Interfaces;

public interface ITokenRepository
{
    Task<List<Token>> GetAllAsync();
    Task<Token?> GetByIdAsync(int id);
    Task<Token?> GetByUsernameAsync(string username);
    Task<bool> ExistsAsync(string username);
    Task<Token> AddAsync(Token token);
    Task<Token> UpdateAsync(Token token);
    Task DeleteAsync(int id);
    Task DeleteByUsernameAsync(string username);
    Task<Token?> GetActiveAsync();
    Task SetActiveAsync(string username);
}