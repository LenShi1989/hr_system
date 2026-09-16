using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrSystem.Api.Data;

public class DateTimeOffsetUtcConverter()
    : ValueConverter<DateTimeOffset, DateTimeOffset>(
        v => v.ToUniversalTime(),
        v => v);