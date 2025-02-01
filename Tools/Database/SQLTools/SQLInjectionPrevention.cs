namespace FishyAPI.Tools.Database.SQLTools;

public class SQLInjectionPrevention
{
    public static string FormatSQLString(string input)
    {
        input = input.Replace("'", "''");
        input = input.Replace("[", "[[]");
        input = input.Replace("%", "[%]");
        input = input.Replace("_", "[_]");
        return input;
    }
}