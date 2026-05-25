using System;
using Volo.Abp.Domain.Entities;

namespace AlphaAgent.Abp.Domain.Entities
{
    public class AppSecurity : Entity<int>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string BaseCode { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}