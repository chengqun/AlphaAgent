using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace AlphaAgent.Abp.Domain.Entities
{
    public class AppMoment : Entity<Guid>
    {
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public string Type { get; set; } = "Text";
        
        public string Visibility { get; set; } = "Friends";
        
        public AppMoment(Guid id, Guid userId, string content)
            : base(id)
        {
            UserId = userId;
            Content = content;
            CreatedAt = DateTime.UtcNow;
        }
    }
}