using Xunit;

namespace GOP.API.Tests.Fixtures;

[CollectionDefinition("ApiIntegrationTests")]
public sealed class ApiTestCollection : ICollectionFixture<GopTestWebApplicationFactory>;
