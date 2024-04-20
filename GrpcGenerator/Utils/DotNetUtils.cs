using GrpcGenerator.Domain;

namespace GrpcGenerator.Utils;

public static class DotNetUtils
{
    public static void ConvertPrimaryKeysAndForeignKeysToDotnetNames(ref Dictionary<string, Type> primaryKeysAndTypes,
        ref Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        ConvertPrimaryKeysToDotnetNames(ref primaryKeysAndTypes);
        ConvertForeignKeysToDotnetNames(ref foreignKeys);
    }
    
    public static void ConvertStringListToDotNetNames(ref List<string> list)
    {
        list=list.Select(StringUtils.GetDotnetNameFromSqlName).ToList();
    }

    public static void ConvertPrimaryKeysToDotnetNames(ref Dictionary<string, Type> primaryKeysAndTypes)
    {
        primaryKeysAndTypes = primaryKeysAndTypes.ToDictionary(entry => StringUtils.GetDotnetNameFromSqlName(entry.Key),
            entry => entry.Value);
    }
    
    public static void ConvertForeignKeysToDotnetNames(ref Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        foreignKeys = foreignKeys.ToDictionary(entry =>
            {
                var modelName = StringUtils.GetDotnetNameFromSqlName(entry.Key);
                if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];
                return modelName;
            },
            entry => entry.Value.ToDictionary(
                entry1 => new ForeignKey(StringUtils.GetDotnetNameFromSqlName(entry1.Key.ColumnName),
                    StringUtils.GetDotnetNameFromSqlName(entry1.Key.ForeignColumnName)), entry1 => entry1.Value));
    }
}