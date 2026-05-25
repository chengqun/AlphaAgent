using System;
using AlphaAgent.Abp.Domain.Shared.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace AlphaAgent.Abp.Domain.Entities
{
    public class AppRelationship : FullAuditedAggregateRoot<Guid>
    {
        public Guid UserId { get; set; }
        public RelationshipType TargetType { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public RelationshipStatus Status { get; set; } = RelationshipStatus.Pending;
        public string? Notes { get; set; }

        public AppRelationship() { }

        public AppRelationship(Guid userId, RelationshipType targetType, string targetId, RelationshipStatus status = RelationshipStatus.Pending, string? notes = null)
        {
            UserId = userId;
            TargetType = targetType;
            TargetId = targetId;
            Status = status;
            Notes = notes;
        }

        public void Accept() => Status = RelationshipStatus.Accepted;
        public void Reject() => Status = RelationshipStatus.Rejected;
        public void UpdateNotes(string? notes) => Notes = notes;
    }
}