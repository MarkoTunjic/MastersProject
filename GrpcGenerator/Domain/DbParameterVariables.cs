using System.Data;

namespace GrpcGenerator.Domain;

public class DbParameterVariables
{
    public DbParameterVariables(string paramName, DbType type, object? value, int size)
    {
        ParamName = paramName;
        Type = type;
        Value = value;
        Size = size;
    }

    public string ParamName { get; set; }
    public DbType Type { get; set; }
    public object? Value { get; set; }
    public int Size { get; set; }
}