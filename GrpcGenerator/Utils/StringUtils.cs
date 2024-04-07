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

        return result;
    }

    public static string GetSqlNameFromDotnetName(string dotnetName)
    {
        var result = "";
        var i = 0;
        foreach (var character in dotnetName.ToCharArray())
        {
            if (char.IsLetter(character) && char.IsLower(character))
            {
                result += character;
                continue;
            }

            if (i != 0) result += '_';

            result += char.ToLower(character);
            i++;
        }

        return result;
    }
}