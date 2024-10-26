using System.Data;
using Dapper;

namespace DapperWebApi.Database;

public sealed class DateOnlySqlTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        // Store as DateTime to the database
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            string stringValue when DateTime.TryParse(stringValue, out DateTime dateTime)
                              => DateOnly.FromDateTime(dateTime),
            _ => throw new ArgumentException("Invalid value for DateOnly type")
        };
    }
}