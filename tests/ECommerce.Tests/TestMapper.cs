using AutoMapper;
using ECommerce.Business.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Tests;

public static class TestMapper
{
    public static IMapper Create() =>
        new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance)
        .CreateMapper();
}
