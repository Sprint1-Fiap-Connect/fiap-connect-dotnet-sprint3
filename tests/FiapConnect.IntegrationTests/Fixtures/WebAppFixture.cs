using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FiapConnect.IntegrationTests.Fixtures;

// Sobe a API em memoria carregando appsettings.Test.json (base separada do prod)
public class WebAppFixture : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Test");

		// ContentRoot padrao do WebApplicationFactory aponta pra src/FiapConnect.API,
		// entao resolvemos o caminho absoluto pro output dos testes onde o
		// CopyToOutputDirectory deixou o appsettings.Test.json
		var caminhoAppsettings = Path.Combine(
			AppContext.BaseDirectory, "appsettings.Test.json");

		builder.ConfigureAppConfiguration((context, config) =>
		{
			config.Sources.Clear();
			config.AddJsonFile(caminhoAppsettings,
				optional: false, reloadOnChange: false);
		});
	}
}