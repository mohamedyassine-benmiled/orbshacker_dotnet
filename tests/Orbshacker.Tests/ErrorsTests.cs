using Xunit;

namespace Orbshacker.Tests;

public class ErrorsTests
{
    [Theory]
    [InlineData(typeof(NetworkException))]
    [InlineData(typeof(SteamNotFoundException))]
    [InlineData(typeof(DatabaseLoadException))]
    public void SpecializedErrorsShareBaseType(Type type) => Assert.True(type.IsSubclassOf(typeof(OrbshackerException)));
}
