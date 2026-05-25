using System;

namespace AlphaAgent.Domain.Entities;

public class Token
{
    public int Id { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public string TokenExpiration { get; private set; } = string.Empty;
    public string IsLoggedIn { get; private set; } = "false";
    public string Username { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    protected Token() { }

    public Token(int id, string accessToken, string refreshToken, string tokenExpiration, string username)
    {
        Id = id;
        SetAccessToken(accessToken);
        SetRefreshToken(refreshToken);
        SetTokenExpiration(tokenExpiration);
        SetUsername(username);
        IsLoggedIn = "false";
        IsActive = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("访问令牌不能为空", nameof(accessToken));
        AccessToken = accessToken;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string refreshToken)
    {
        RefreshToken = refreshToken ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTokenExpiration(string tokenExpiration)
    {
        TokenExpiration = tokenExpiration ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空", nameof(username));
        Username = username;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsLoggedIn()
    {
        IsLoggedIn = "true";
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsLoggedOut()
    {
        IsLoggedIn = "false";
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInactive()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired()
    {
        if (string.IsNullOrEmpty(TokenExpiration))
            return true;

        if (DateTime.TryParse(TokenExpiration, out var expiration))
        {
            return expiration < DateTime.UtcNow;
        }

        return true;
    }

    public bool IsAboutToExpire(TimeSpan threshold)
    {
        if (string.IsNullOrEmpty(TokenExpiration))
            return true;

        if (DateTime.TryParse(TokenExpiration, out var expiration))
        {
            var timeUntilExpiration = expiration - DateTime.UtcNow;
            return timeUntilExpiration < threshold;
        }

        return true;
    }

    public void Update(string accessToken, string refreshToken, int? expiresIn)
    {
        SetAccessToken(accessToken);
        SetRefreshToken(refreshToken);
        
        if (expiresIn.HasValue)
        {
            var expiration = DateTime.UtcNow.AddSeconds(expiresIn.Value).ToString("o");
            SetTokenExpiration(expiration);
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
}