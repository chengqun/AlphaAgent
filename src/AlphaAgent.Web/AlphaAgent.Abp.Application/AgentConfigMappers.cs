using AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;
using AgentConfigEntity = AlphaAgent.Abp.Domain.Entities.AppAgentConfig;
using Volo.Abp.Mapperly;

namespace AlphaAgent.Abp.Application.Mappings;

public class AgentConfigCreateDtoToAgentConfigMapper : MapperBase<AgentConfigCreateDto, AgentConfigEntity>
{
    public override AgentConfigEntity Map(AgentConfigCreateDto source)
    {
        return new AgentConfigEntity
        {
            AgentName = source.AgentName,
            ModelName = source.ModelName,
            ApiKey = source.ApiKey,
            Endpoint = source.Endpoint,
            DefaultSystemPrompt = source.DefaultSystemPrompt,
            Temperature = source.Temperature,
            IsActive = source.IsActive
        };
    }

    public override void Map(AgentConfigCreateDto source, AgentConfigEntity destination)
    {
        destination.AgentName = source.AgentName;
        destination.ModelName = source.ModelName;
        destination.ApiKey = source.ApiKey;
        destination.Endpoint = source.Endpoint;
        destination.DefaultSystemPrompt = source.DefaultSystemPrompt;
        destination.Temperature = source.Temperature;
        destination.IsActive = source.IsActive;
    }
}

public class AgentConfigUpdateDtoToAgentConfigMapper : MapperBase<AgentConfigUpdateDto, AgentConfigEntity>
{
    public override AgentConfigEntity Map(AgentConfigUpdateDto source)
    {
        return new AgentConfigEntity
        {
            AgentName = source.AgentName,
            ModelName = source.ModelName,
            ApiKey = source.ApiKey,
            Endpoint = source.Endpoint,
            DefaultSystemPrompt = source.DefaultSystemPrompt,
            Temperature = source.Temperature,
            IsActive = source.IsActive
        };
    }

    public override void Map(AgentConfigUpdateDto source, AgentConfigEntity destination)
    {
        destination.AgentName = source.AgentName;
        destination.ModelName = source.ModelName;
        destination.ApiKey = source.ApiKey;
        destination.Endpoint = source.Endpoint;
        destination.DefaultSystemPrompt = source.DefaultSystemPrompt;
        destination.Temperature = source.Temperature;
        destination.IsActive = source.IsActive;
    }
}

public class AgentConfigToAgentConfigDtoMapper : MapperBase<AgentConfigEntity, AgentConfigDto>
{
    public override AgentConfigDto Map(AgentConfigEntity source)
    {
        return new AgentConfigDto
        {
            Id = source.Id,
            AgentName = source.AgentName,
            ModelName = source.ModelName,
            ApiKey = source.ApiKey,
            Endpoint = source.Endpoint,
            DefaultSystemPrompt = source.DefaultSystemPrompt,
            Temperature = source.Temperature,
            IsActive = source.IsActive,
            CreatorId = source.CreatorId
        };
    }

    public override void Map(AgentConfigEntity source, AgentConfigDto destination)
    {
        destination.Id = source.Id;
        destination.AgentName = source.AgentName;
        destination.ModelName = source.ModelName;
        destination.ApiKey = source.ApiKey;
        destination.Endpoint = source.Endpoint;
        destination.DefaultSystemPrompt = source.DefaultSystemPrompt;
        destination.Temperature = source.Temperature;
        destination.IsActive = source.IsActive;
        destination.CreatorId = source.CreatorId;
    }
}