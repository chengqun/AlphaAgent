using System;

namespace AlphaAgent.Abp.Application.Contracts.DTOs.Moment
{
    public class CreateGroupMomentDto
    {
        public string GroupId { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
    }
}
