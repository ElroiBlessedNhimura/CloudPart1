using CoffeeNChill.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<MenuTableService>();
        services.AddSingleton<StaffDocumentService>();
    })
    .Build();

host.Run();