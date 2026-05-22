namespace FiapConnect.IntegrationTests.Fixtures;

// Collection Fixture: compartilha uma unica instancia da WebAppFixture entre testes
[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<WebAppFixture> { }