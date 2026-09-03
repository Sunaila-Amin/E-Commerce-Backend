using AutoMapper;
using ECommerce.Business.Mapping;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.Tests.Unit.Mapping;

public class MappingProfileTests
{
    [Fact]
    public void AllMappings_AreValid()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }
}
