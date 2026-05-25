using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Shared.Enums;
using AlphaAgent.Abp.Domain.Services.Securities;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace AlphaAgent.Abp.Domain.Services.Moment
{
    /// <summary>
    /// 动态管理服务实现
    /// 处理动态相关的核心业务逻辑
    /// </summary>
    public class MomentManager : DomainService, IMomentManager
    {
        private readonly IRepository<AppMoment, Guid> _momentRepository;
        private readonly IRepository<AppRelationship, Guid> _relationshipRepository;
        private readonly ISecurityManager _securityManager;
        private readonly IGuidGenerator _guidGenerator;

        public MomentManager(
            IRepository<AppMoment, Guid> momentRepository,
            IRepository<AppRelationship, Guid> relationshipRepository,
            ISecurityManager securityManager,
            IGuidGenerator guidGenerator)
        {
            _momentRepository = momentRepository;
            _relationshipRepository = relationshipRepository;
            _securityManager = securityManager;
            _guidGenerator = guidGenerator;
        }

        public async Task<AppMoment> CreateMomentAsync(Guid userId, string content, string imageUrl = null, string type = "User", string visibility = "Friends")
        {
            var moment = new AppMoment(
                _guidGenerator.Create(),
                userId,
                content
            )
            {
                ImageUrl = imageUrl,
                Type = type,
                Visibility = visibility
            };

            return await _momentRepository.InsertAsync(moment);
        }

        public async Task<AppMoment> CreateStockMomentAsync(int stockId, string content, string imageUrl = null)
        {
            // 验证股票是否存在
            var securities = await _securityManager.GetAllAsync();
            var stock = securities.FirstOrDefault(s => s.Id == stockId);
            if (stock == null)
            {
                throw new BusinessException("AlphaAgent:StockNotFound").WithData("StockId", stockId);
            }

            var moment = new AppMoment(
                _guidGenerator.Create(),
                CreateStockGuid(stockId),
                content
            )
            {
                ImageUrl = imageUrl,
                Type = "Stock",
                Visibility = "Public"
            };

            return await _momentRepository.InsertAsync(moment);
        }

        public async Task<AppMoment> CreateDeviceMomentAsync(string deviceId, string content, string imageUrl = null)
        {
            var moment = new AppMoment(
                _guidGenerator.Create(),
                CreateTargetGuid(deviceId),
                content
            )
            {
                ImageUrl = imageUrl,
                Type = "Device",
                Visibility = "Public"
            };

            return await _momentRepository.InsertAsync(moment);
        }

        public async Task<AppMoment> CreateGroupMomentAsync(string groupId, string content, string imageUrl = null)
        {
            var moment = new AppMoment(
                _guidGenerator.Create(),
                CreateTargetGuid(groupId),
                content
            )
            {
                ImageUrl = imageUrl,
                Type = "Group",
                Visibility = "Public"
            };

            return await _momentRepository.InsertAsync(moment);
        }

        public async Task DeleteMomentAsync(Guid momentId, Guid currentUserId)
        {
            var moment = await _momentRepository.GetAsync(momentId);

            if (moment.UserId != currentUserId)
            {
                throw new BusinessException("AlphaAgent:UnauthorizedDelete");
            }

            await _momentRepository.DeleteAsync(moment);
        }

        public async Task<List<AppMoment>> GetMomentsAsync(Guid targetId, string type, int limit = 50, int offset = 0, DateTime? since = null)
        {
            var moments = await _momentRepository.GetListAsync();
            return moments.Where(m => m.UserId == targetId && m.Type == type && (since == null || m.CreatedAt > since.Value))
                .OrderByDescending(m => m.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }

        public async Task<List<AppMoment>> GetFriendsMomentsAsync(Guid currentUserId, int limit = 50, int offset = 0, DateTime? since = null)
        {
            var relationships = await _relationshipRepository.GetListAsync();
            var friendIds = new List<Guid>();

            friendIds.AddRange(relationships
                .Where(r => r.UserId == currentUserId && r.TargetType == RelationshipType.Friendship && r.Status == RelationshipStatus.Accepted)
                .Select(r => Guid.Parse(r.TargetId)));

            friendIds.Add(currentUserId);
            friendIds = friendIds.Distinct().ToList();

            var userStockRelationships = relationships.Where(r => r.UserId == currentUserId && r.TargetType == RelationshipType.Stock && r.Status == RelationshipStatus.Accepted).ToList();
            var userStockIds = userStockRelationships.Select(r => int.Parse(r.TargetId)).ToList();

            var moments = await _momentRepository.GetListAsync();
            return moments.Where(m =>
                ((friendIds.Contains(m.UserId) && m.Visibility == "Friends") ||
                (m.Type == "Stock" && IsUserStock(m.UserId, userStockIds)))
                && (since == null || m.CreatedAt > since.Value)
            )
                .OrderByDescending(m => m.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }

        public async Task<AppMoment> GetMomentAsync(Guid momentId)
        {
            return await _momentRepository.GetAsync(momentId);
        }

        public Guid CreateStockGuid(int stockId)
        {
            // 将股票ID转换为8位十六进制字符串
            string stockIdHex = stockId.ToString("X8");
            // 补齐到32位，使用固定的后缀
            string guidString = stockIdHex.PadRight(32, '0');
            // 格式化为Guid格式
            guidString = $"{guidString.Substring(0, 8)}-{guidString.Substring(8, 4)}-{guidString.Substring(12, 4)}-{guidString.Substring(16, 4)}-{guidString.Substring(20)}";
            return new Guid(guidString);
        }

        public Guid CreateTargetGuid(string targetId)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(targetId);
            byte[] hash = System.Security.Cryptography.SHA256.Create().ComputeHash(bytes);
            return new Guid(hash.Take(16).ToArray());
        }

        private bool IsUserStock(Guid userId, List<int> userStockIds)
        {
            try
            {
                // 尝试将Guid转换为int
                int stockId = Convert.ToInt32(userId.ToString().Substring(0, 8), 16);
                return userStockIds.Contains(stockId);
            }
            catch
            {
                return false;
            }
        }
    }
}