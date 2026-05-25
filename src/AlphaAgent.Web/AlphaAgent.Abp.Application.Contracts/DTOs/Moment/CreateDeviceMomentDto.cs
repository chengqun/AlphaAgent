using System;

namespace AlphaAgent.Abp.Application.Contracts.DTOs.Moment
{
    public class CreateDeviceMomentDto
    {
        public string DeviceId { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
    }
}
