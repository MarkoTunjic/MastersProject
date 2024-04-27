namespace GrpcGenerator.Domain.Mappers;

public static class MapperServiceRegistration
{
    public static void AddMappers(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MapperServiceRegistration));
    }
}