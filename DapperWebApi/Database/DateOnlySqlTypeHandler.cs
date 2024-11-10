using Dapper;
using System.Data;

namespace DapperWebApi.Database;

public sealed class DateOnlySqlTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value  = DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
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

public sealed class DateTimeSqlTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }

    public override DateTime Parse(object value)
    {
        return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
    }
}
