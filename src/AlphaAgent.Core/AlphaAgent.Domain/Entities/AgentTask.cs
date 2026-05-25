using System;

namespace AlphaAgent.Domain.Entities;

public class AgentTask
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string TaskType { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? Input { get; private set; }
    public string? Output { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    protected AgentTask() { }

    public AgentTask(Guid sessionId, string taskType, string? input = null)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        TaskType = taskType;
        Input = input;
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
    }

    public void Start() => Status = "Running";

    public void Complete(string output)
    {
        Status = "Completed";
        Output = output;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = "Failed";
        Output = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}