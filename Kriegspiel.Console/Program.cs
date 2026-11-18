using Kriegsspiel.Console.Startup;
using Microsoft.Extensions.DependencyInjection;

var services = ServiceRegistration.Build(args);
services.GetRequiredService<GameLoop>().Run();