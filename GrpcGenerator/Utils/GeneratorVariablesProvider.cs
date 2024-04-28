using GrpcGenerator.Domain;

namespace GrpcGenerator.Utils;

public static class GeneratorVariablesProvider
{
    private static readonly Dictionary<string, GeneratorVariables> GeneratorVariablesMap = new();

    public static void AddVariables(string uuid, GeneratorVariables databaseConnection)
    {
        GeneratorVariablesMap.Add(uuid, databaseConnection);
    }

    public static GeneratorVariables GetVariables(string uuid)
    {
        return GeneratorVariablesMap[uuid];
    }
    
    public static void RemoveVariables(string uuid)
    {
        GeneratorVariablesMap.Remove(uuid);
    }
}