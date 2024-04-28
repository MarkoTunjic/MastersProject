using GrpcGenerator.Application.Services;
using GrpcGenerator.Application.Services.Impl;
using GrpcGenerator.Domain.Mappers;
using GrpcGenerator.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddTransient<IGeneratorService, GeneratorServiceImpl>();
builder.Services.AddMappers();

var app = builder.Build();

app.MapGrpcService<GrpcGeneratorService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();