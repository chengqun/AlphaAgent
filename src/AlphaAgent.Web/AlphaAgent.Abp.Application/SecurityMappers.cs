using AlphaAgent.Abp.Application.Contracts.DTOs.Security;
using SecurityEntity = AlphaAgent.Abp.Domain.Entities.AppSecurity;
using Volo.Abp.Mapperly;

namespace AlphaAgent.Abp.Application.Mappings;

public class SecurityCreateDtoToSecurityMapper : MapperBase<SecurityCreateDto, SecurityEntity>
{
    public override SecurityEntity Map(SecurityCreateDto source)
    {
        return new SecurityEntity
        {
            Code = source.Code,
            Name = source.Name,
            Type = source.Type,
            Exchange = source.Exchange,
            BaseCode = source.BaseCode
        };
    }

    public override void Map(SecurityCreateDto source, SecurityEntity destination)
    {
        destination.Code = source.Code;
        destination.Name = source.Name;
        destination.Type = source.Type;
        destination.Exchange = source.Exchange;
        destination.BaseCode = source.BaseCode;
    }
}

public class SecurityUpdateDtoToSecurityMapper : MapperBase<SecurityUpdateDto, SecurityEntity>
{
    public override SecurityEntity Map(SecurityUpdateDto source)
    {
        return new SecurityEntity
        {
            Name = source.Name,
            Type = source.Type,
            Exchange = source.Exchange,
            BaseCode = source.BaseCode
        };
    }

    public override void Map(SecurityUpdateDto source, SecurityEntity destination)
    {
        destination.Name = source.Name;
        destination.Type = source.Type;
        destination.Exchange = source.Exchange;
        destination.BaseCode = source.BaseCode;
    }
}

public class SecurityToSecurityDtoMapper : MapperBase<SecurityEntity, SecurityDto>
{
    public override SecurityDto Map(SecurityEntity source)
    {
        return new SecurityDto
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            Type = source.Type,
            Exchange = source.Exchange,
            BaseCode = source.BaseCode
        };
    }

    public override void Map(SecurityEntity source, SecurityDto destination)
    {
        destination.Id = source.Id;
        destination.Code = source.Code;
        destination.Name = source.Name;
        destination.Type = source.Type;
        destination.Exchange = source.Exchange;
        destination.BaseCode = source.BaseCode;
    }
}