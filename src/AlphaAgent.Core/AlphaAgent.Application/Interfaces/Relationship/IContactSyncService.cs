using System;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Relationship;

namespace AlphaAgent.Application.Interfaces.Relationship;

public interface IContactSyncService
{
    Task<ContactBookDto?> GetCachedContactsAsync(Guid userId);
    Task<ContactBookDto?> SyncFromServerAsync(Guid userId);
}
