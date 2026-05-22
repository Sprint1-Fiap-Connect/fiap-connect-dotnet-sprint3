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

		builder.ConfigureAppConfiguration((context, config) =>
		{
			// Limpa providers padrao para nao herdar appsettings.Development
			config.Sources.Clear();
			config.AddJsonFile("appsettings.Test.json",
				optional: false, reloadOnChange: false);
		});
	}
}