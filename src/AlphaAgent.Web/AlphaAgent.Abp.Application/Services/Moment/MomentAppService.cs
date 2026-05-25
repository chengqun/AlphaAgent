using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.Moment;
using AlphaAgent.Abp.Application.Contracts.Services.Moment;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Shared.Enums;
using AlphaAgent.Abp.Domain.Services.Moment;
using AlphaAgent.Abp.Domain.Services.Securities;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp;
using AlphaAgent.Abp.Permissions;

namespace AlphaAgent.Abp.Application.Services.Moment
{
    [Authorize]
    [Route("api/app/moment")]
    public class MomentAppService : ApplicationService, IMomentAppService
    {
        private readonly IMomentManager _momentManager;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<AppMoment, Guid> _momentRepository;
        private readonly ISecurityManager _securityManager;

        public MomentAppService(
            IMomentManager momentManager,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<AppMoment, Guid> momentRepository,
            ISecurityManager securityManager)
        {
            _momentManager = momentManager;
            _userRepository = userRepository;
            _momentRepository = momentRepository;
            _securityManager = securityManager;
        }

        [HttpPost("moment")]
        public async Task<MomentDto> CreateMomentAsync(CreateMomentDto input)
        {
            var currentUserId = CurrentUser.Id ?? throw new BusinessException("AlphaAgent:UserNotLoggedIn");

            // 如果有 StockId，创建股票动态
            if (input.StockId.HasValue)
            {
                var moment = await _momentManager.CreateStockMomentAsync(
                    input.StockId.Value,
                    input.Content,
                    input.ImageUrl
                );

                // 获取股票信息
                var stock = await _securityManager.GetByIdAsync(input.StockId.Value);
                var stockName = stock?.Name ?? "";

                return new MomentDto
                {
                    Id = moment.Id,
                    UserId = moment.UserId,
                    Username = stockName, // 使用股票名称作为Username
                    Content = moment.Content,
                    ImageUrl = moment.ImageUrl,
                    CreatedAt = moment.CreatedAt,
                    Type = moment.Type,
                    Visibility = moment.Visibility
                };
            }
            else
            {
                // 创建普通动态
                var moment = await _momentManager.CreateMomentAsync(
                    currentUserId,
                    input.Content,
                    input.ImageUrl,
                    input.Type,
                    input.Visibility
                );

                var user = await _userRepository.GetAsync(currentUserId);

                return new MomentDto
                {
                    Id = moment.Id,
                    UserId = moment.UserId,
                    Username = user.UserName,
                    Content = moment.Content,
                    ImageUrl = moment.ImageUrl,
                    CreatedAt = moment.CreatedAt,
                    Type = moment.Type,
                    Visibility = moment.Visibility
                };
            }
        }

        [HttpGet("friends-moments")]
        public async Task<List<MomentDto>> GetFriendsMomentsAsync(int limit = 50, int offset = 0, DateTime? since = null)
        {
            var currentUserId = CurrentUser.Id ?? throw new BusinessException("AlphaAgent:UserNotLoggedIn");

            var moments = await _momentManager.GetFriendsMomentsAsync(currentUserId, limit, offset, since);

            var securities = await _securityManager.GetAllAsync();
            var stockMap = securities.ToDictionary(s => s.Id, s => s.Name);
            var allUsers = await _userRepository.GetListAsync();
            var userMap = allUsers.ToDictionary(u => u.Id, u => u.UserName);

            return moments.Select(m => new MomentDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Username = m.Type == "Stock" ? GetStockName(m.UserId, stockMap) : userMap.TryGetValue(m.UserId, out var username) ? username : "",
                Content = m.Content,
                ImageUrl = m.ImageUrl,
                CreatedAt = m.CreatedAt,
                Type = m.Type,
                Visibility = m.Visibility
            }).ToList();
        }

        [HttpGet("moments/{targetId}")]
        public async Task<List<MomentDto>> GetMomentsAsync([FromRoute] string targetId, string type, int limit = 50, int offset = 0, DateTime? since = null)
        {
            var currentUserId = CurrentUser.Id ?? throw new BusinessException("AlphaAgent:UserNotLoggedIn");

            switch (type?.ToLower())
            {
                case "friendship":
                case "user":
                    var targetUserId = Guid.Parse(targetId);
                    var targetUser = await _userRepository.FindAsync(targetUserId);
                    if (targetUser == null) return new List<MomentDto>();

                    var userMoments = await _momentManager.GetMomentsAsync(targetUserId, "User", limit, offset, since);
                    return userMoments.Select(m => new MomentDto
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Username = targetUser.UserName,
                        Content = m.Content,
                        ImageUrl = m.ImageUrl,
                        CreatedAt = m.CreatedAt,
                        Type = m.Type,
                        Visibility = m.Visibility
                    }).ToList();

                case "stock":
                    if (int.TryParse(targetId, out var stockId))
                    {
                        var stock = await _securityManager.GetByIdAsync(stockId);
                        var stockUsername = stock?.Name ?? "";
                        var stockMoments = await _momentManager.GetMomentsAsync(_momentManager.CreateStockGuid(stockId), "Stock", limit, offset, since);
                        return stockMoments.Select(m => new MomentDto
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Username = stockUsername,
                            Content = m.Content,
                            ImageUrl = m.ImageUrl,
                            CreatedAt = m.CreatedAt,
                            Type = m.Type,
                            Visibility = m.Visibility
                        }).ToList();
                    }
                    else
                    {
                        var stockByCode = await _securityManager.FindAsync(targetId);
                        if (stockByCode == null) return new List<MomentDto>();
                        var stockCodeMoments = await _momentManager.GetMomentsAsync(_momentManager.CreateStockGuid(stockByCode.Id), "Stock", limit, offset, since);
                        return stockCodeMoments.Select(m => new MomentDto
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Username = stockByCode.Name,
                            Content = m.Content,
                            ImageUrl = m.ImageUrl,
                            CreatedAt = m.CreatedAt,
                            Type = m.Type,
                            Visibility = m.Visibility
                        }).ToList();
                    }

                case "device":
                    var deviceMoments = await _momentManager.GetMomentsAsync(_momentManager.CreateTargetGuid(targetId), "Device", limit, offset, since);
                    return deviceMoments.Select(m => new MomentDto
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Username = "设备",
                        Content = m.Content,
                        ImageUrl = m.ImageUrl,
                        CreatedAt = m.CreatedAt,
                        Type = m.Type,
                        Visibility = m.Visibility
                    }).ToList();

                case "group":
                    var groupMoments = await _momentManager.GetMomentsAsync(_momentManager.CreateTargetGuid(targetId), "Group", limit, offset, since);
                    return groupMoments.Select(m => new MomentDto
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Username = "群组",
                        Content = m.Content,
                        ImageUrl = m.ImageUrl,
                        CreatedAt = m.CreatedAt,
                        Type = m.Type,
                        Visibility = m.Visibility
                    }).ToList();

                default:
                    if (int.TryParse(targetId, out var numStockId))
                    {
                        var stockDefault = await _securityManager.GetByIdAsync(numStockId);
                        var stockDefaultUsername = stockDefault?.Name ?? "";
                        var stockDefaultMoments = await _momentManager.GetMomentsAsync(_momentManager.CreateStockGuid(numStockId), "Stock", limit, offset, since);
                        return stockDefaultMoments.Select(m => new MomentDto
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Username = stockDefaultUsername,
                            Content = m.Content,
                            ImageUrl = m.ImageUrl,
                            CreatedAt = m.CreatedAt,
                            Type = m.Type,
                            Visibility = m.Visibility
                        }).ToList();
                    }
                    else if (targetId.Contains("-"))
                    {
                        var guidUserId = Guid.Parse(targetId);
                        var user = await _userRepository.FindAsync(guidUserId);
                        if (user != null)
                        {
                            var defaultUserMoments = await _momentManager.GetMomentsAsync(guidUserId, "User", limit, offset, since);
                            return defaultUserMoments.Select(m => new MomentDto
                            {
                                Id = m.Id,
                                UserId = m.UserId,
                                Username = user.UserName,
                                Content = m.Content,
                                ImageUrl = m.ImageUrl,
                                CreatedAt = m.CreatedAt,
                                Type = m.Type,
                                Visibility = m.Visibility
                            }).ToList();
                        }
                    }
                    var stockByCodeDefault = await _securityManager.FindAsync(targetId);
                    if (stockByCodeDefault == null) return new List<MomentDto>();
                    var stockCodeDefaultMoments = await _momentManager.GetMomentsAsync(_momentManager.CreateStockGuid(stockByCodeDefault.Id), "Stock", limit, offset, since);
                    return stockCodeDefaultMoments.Select(m => new MomentDto
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Username = stockByCodeDefault.Name,
                        Content = m.Content,
                        ImageUrl = m.ImageUrl,
                        CreatedAt = m.CreatedAt,
                        Type = m.Type,
                        Visibility = m.Visibility
                    }).ToList();
            }
        }

        [HttpDelete("moment/{id}")]
        public async Task DeleteMomentAsync(Guid id)
        {
            var currentUserId = CurrentUser.Id ?? throw new BusinessException("AlphaAgent:UserNotLoggedIn");
            await _momentManager.DeleteMomentAsync(id, currentUserId);
        }

        private string GetStockName(Guid userId, Dictionary<int, string> stockMap)
        {
            try
            {
                int stockId = Convert.ToInt32(userId.ToString().Substring(0, 8), 16);
                return stockMap.TryGetValue(stockId, out var stockName) ? stockName : "";
            }
            catch
            {
                return "";
            }
        }

    }
}