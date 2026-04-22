using Xunit;

namespace PlaywrightTests.Tests.Setup;

[CollectionDefinition("Admin Auth Collection")]
public class AdminAuthCollection : ICollectionFixture<AdminAuthFixture>
{
}
