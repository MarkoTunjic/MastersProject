namespace GrpcGenerator.Utils;

public static class StringUtils
{
    public static string GetDotnetNameFromSqlName(string sqlName)
    {
        var result = "";
        foreach (var part in sqlName.Split("_"))
        {
            var firstLetter = char.ToUpper(part[0]);
            result += firstLetter + part[1..];
        }

        if (char.ToLower(result[^1]) == 's') result = result[..^1];
        return result;
    }
}