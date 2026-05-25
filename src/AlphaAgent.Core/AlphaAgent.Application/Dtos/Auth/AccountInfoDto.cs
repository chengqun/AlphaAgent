using System;

namespace AlphaAgent.Application.Dtos.Auth;

public class AccountInfoDto
{
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastUpdated { get; set; }
}