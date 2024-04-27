using GrpcGenerator.Domain;

namespace GrpcGenerator.Application.Services;

public interface IGeneratorService
{
    byte[] GetZipProject(GenerationRequest generationRequest);
}