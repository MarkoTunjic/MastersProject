using AutoMapper;

namespace GrpcGenerator.Domain.Mappers;

public class MapperRegistration : Profile
{
    public MapperRegistration()
    {
        CreateMap<GenerationRequestMessage, GenerationRequest>();
        CreateMap<GrpcDatabaseConnectionData, DatabaseConnectionData>();
    }
}