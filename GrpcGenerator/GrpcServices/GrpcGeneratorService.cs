using AutoMapper;
using Grpc.Core;
using GrpcGenerator.Application.Services;
using GrpcGenerator.Domain;

namespace GrpcGenerator.GrpcServices;

public class GrpcGeneratorService : Generator.GeneratorBase
{
    private readonly IGeneratorService _generatorService;
    private readonly IMapper _mapper;
    
    public GrpcGeneratorService(IGeneratorService generatorService, IMapper mapper)
    {
        _generatorService = generatorService;
        _mapper = mapper;
    }

    public override Task<GenerationReply> GenerateProject(GenerationRequestMessage request, ServerCallContext context)
    {
        var mappedRequest = _mapper.Map<GenerationRequestMessage, GenerationRequest>(request);
        var result = _generatorService.GetZipProject(mappedRequest);
        return Task.FromResult(new GenerationReply
        {
            Zip = Google.Protobuf.ByteString.CopyFrom(result)
        });
    }

    public override Task<AvailableTablesReply> GetAvailableTables(GrpcDatabaseConnectionData request, ServerCallContext context)
    {
        var result = _generatorService.GetAvailableTables(_mapper.Map<GrpcDatabaseConnectionData, DatabaseConnectionData>(request));
        return Task.FromResult(new AvailableTablesReply
        {
            AvailableTables = { result }
        });
    }
}