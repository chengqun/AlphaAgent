using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Shared.Enums;

namespace AlphaAgent.Abp.Domain.Services.Moment
{
    /// <summary>
    /// 动态管理接口
    /// 定义动态相关的核心业务逻辑
    /// </summary>
    public interface IMomentManager
    {
        /// <summary>
        /// 创建动态
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="content">内容</param>
        /// <param name="imageUrl">图片URL</param>
        /// <param name="type">动态类型</param>
        /// <param name="visibility">可见性</param>
        /// <returns></returns>
        Task<AppMoment> CreateMomentAsync(Guid userId, string content, string imageUrl = null, string type = "User", string visibility = "Friends");

        /// <summary>
        /// 创建股票动态
        /// </summary>
        /// <param name="stockId">股票ID</param>
        /// <param name="content">内容</param>
        /// <param name="imageUrl">图片URL</param>
        /// <returns></returns>
        Task<AppMoment> CreateStockMomentAsync(int stockId, string content, string imageUrl = null);

        /// <summary>
        /// 创建设备动态
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="content">内容</param>
        /// <param name="imageUrl">图片URL</param>
        /// <returns></returns>
        Task<AppMoment> CreateDeviceMomentAsync(string deviceId, string content, string imageUrl = null);

        /// <summary>
        /// 创建群组动态
        /// </summary>
        /// <param name="groupId">群组ID</param>
        /// <param name="content">内容</param>
        /// <param name="imageUrl">图片URL</param>
        /// <returns></returns>
        Task<AppMoment> CreateGroupMomentAsync(string groupId, string content, string imageUrl = null);

        /// <summary>
        /// 删除动态
        /// </summary>
        /// <param name="momentId">动态ID</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns></returns>
        Task DeleteMomentAsync(Guid momentId, Guid currentUserId);

        /// <summary>
        /// 获取好友的动态
        /// </summary>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="limit">限制数量</param>
        /// <param name="offset">偏移量</param>
        /// <returns></returns>
        Task<List<AppMoment>> GetFriendsMomentsAsync(Guid currentUserId, int limit = 50, int offset = 0, DateTime? since = null);

        /// <summary>
        /// 获取指定目标的动态（支持用户/股票/设备/群组）
        /// </summary>
        /// <param name="targetId">目标ID（Guid格式）</param>
        /// <param name="type">类型：User/Stock/Device/Group</param>
        /// <param name="limit">限制数量</param>
        /// <param name="offset">偏移量</param>
        /// <returns></returns>
        Task<List<AppMoment>> GetMomentsAsync(Guid targetId, string type, int limit = 50, int offset = 0, DateTime? since = null);

        /// <summary>
        /// 获取动态详情
        /// </summary>
        /// <param name="momentId">动态ID</param>
        /// <returns></returns>
        Task<AppMoment> GetMomentAsync(Guid momentId);

        /// <summary>
        /// 将股票ID转换为Guid
        /// </summary>
        Guid CreateStockGuid(int stockId);

        /// <summary>
        /// 将目标ID转换为Guid（适用于设备、群组等字符串ID）
        /// </summary>
        Guid CreateTargetGuid(string targetId);
    }
}