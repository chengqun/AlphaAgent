using AlphaAgent.Domain.Entities;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Interfaces;

public interface ITokenRepository
{
    Task<Token?> GetByIdAsync(int id);
    Task<Token?> GetByUsernameAsync(string username);
    Task<Token> AddAsync(Token token);
    Task<Token> UpdateAsync(Token token);
    Task DeleteAsync(int id);
    Task<Token?> GetActiveAsync();
}