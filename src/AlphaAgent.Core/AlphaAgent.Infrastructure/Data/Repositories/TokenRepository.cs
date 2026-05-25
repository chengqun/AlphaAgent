using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly SharesDbContext _context;

    public TokenRepository(SharesDbContext context)
    {
        _context = context;
    }

    public async Task<List<Token>> GetAllAsync()
    {
        return await _context.Tokens.ToListAsync();
    }

    public async Task<Token?> GetByIdAsync(int id)
    {
        return await _context.Tokens.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Token?> GetByUsernameAsync(string username)
    {
        return await _context.Tokens.FirstOrDefaultAsync(t => t.Username == username);
    }

    public async Task<bool> ExistsAsync(string username)
    {
        return await _context.Tokens.AnyAsync(t => t.Username == username);
    }

    public async Task<Token> AddAsync(Token token)
    {
        _context.Tokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<Token> UpdateAsync(Token token)
    {
        _context.Tokens.Update(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task DeleteAsync(int id)
    {
        var token = await _context.Tokens.FirstOrDefaultAsync(t => t.Id == id);
        if (token != null)
        {
            _context.Tokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByUsernameAsync(string username)
    {
        var token = await _context.Tokens.FirstOrDefaultAsync(t => t.Username == username);
        if (token != null)
        {
            _context.Tokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Token?> GetActiveAsync()
    {
        return await _context.Tokens.FirstOrDefaultAsync(t => t.IsActive);
    }

    public async Task SetActiveAsync(string username)
    {
        var allTokens = await _context.Tokens.ToListAsync();
        foreach (var t in allTokens)
        {
            if (t.Username == username)
            {
                t.SetActive();
            }
            else
            {
                t.SetInactive();
            }
        }
        await _context.SaveChangesAsync();
    }
}