using System;
using Volo.Abp.Domain.Entities;

namespace AlphaAgent.Abp.Domain.Entities
{
    public class AppDevice : Entity<Guid>
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }

        /// <summary>
        /// 是否允许其他用户搜索到该设备。默认 false，只允许添加自己创建的设备。
        /// </summary>
        public bool IsSearchable { get; set; } = false;
    }
}
