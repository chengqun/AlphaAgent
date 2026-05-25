using System.Text.Json.Serialization;

namespace AlphaAgent.Application.Dtos.Auth;

public class LoginRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "password";

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "alphaagent_chat";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = "chat_secret";

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "Abp alphaagent_chat";
}

public class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("appName")]
    public string AppName { get; set; } = "AlphaAgent_Chat";
}

public class AbpErrorResponse
{
    [JsonPropertyName("error")]
    public AbpErrorDetail? Error { get; set; }
}

public class AbpErrorDetail
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}