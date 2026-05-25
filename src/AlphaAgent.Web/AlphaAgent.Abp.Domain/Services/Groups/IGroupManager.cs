using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Abp.Domain.Entities;

namespace AlphaAgent.Abp.Domain.Services.Groups
{
    public interface IGroupManager
    {
        Task<AppGroup> CreateGroupAsync(string name, Guid ownerId, string description = "");
        Task<AppGroup> GetGroupAsync(Guid groupId);
        Task<List<AppGroup>> SearchGroupsAsync(string keyword);
        Task DisbandGroupAsync(Guid groupId);
    }
}