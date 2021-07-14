namespace FluentValidation.Tests {
	using System;
	using System.IO;
	using System.Net.Http;
	using AspNetCore;
	using FluentValidation.AspNetCore;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Mvc.Testing;
	using Microsoft.Extensions.DependencyInjection;

	public class WebAppFixture : WebApplicationFactory<Startup> {
		public WebAppFixture() {
			// using builder.UseContentRoot inside ConfigureWebHost doesn't work
			// in .net6 anymore as it explicitly checks the testing manifest file instead.
			// Delete the manifest file to revert to the net5 behaviour.
			if (File.Exists("MvcTestingAppManifest.json")) {
				File.Delete("MvcTestingAppManifest.json");
			}
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder) {
			builder.UseContentRoot(".");
		}

		protected override IWebHostBuilder CreateWebHostBuilder() {
			return new WebHostBuilder()
				.UseDefaultServiceProvider((context, options) => options.ValidateScopes = true)
				.UseStartup<Startup>();
		}

		public HttpClient CreateClientWithServices(Action<IServiceCollection> configurator) {
			return WithWebHostBuilder(builder => builder.ConfigureServices(configurator)).CreateClient();
		}
	}
}
