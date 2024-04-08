using GrpcGenerator.Domain;

namespace GrpcGenerator.Utils;

public static class DotNetUtils
{
    public static void CovertPrimaryKeysAndForeignKeysToDotnetNames(ref Dictionary<string, Type> primaryKeysAndTypes,
        ref Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        primaryKeysAndTypes = primaryKeysAndTypes.ToDictionary(entry => StringUtils.GetDotnetNameFromSqlName(entry.Key),
            entry => entry.Value);

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