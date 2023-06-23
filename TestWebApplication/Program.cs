var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var configuration = app.Configuration.GetSection("Logging").GetSection("LogLevel");
foreach (var keyValuePair in configuration.AsEnumerable(true))
{
    
}

var appConfigurator = ((IConfigurationBuilder) app.Configuration).ToAppConfigurator();

configuration = app.Configuration.GetSection("Logging").GetSection("LogLevel");
foreach (var keyValuePair in configuration.AsEnumerable(true))
{

}

app.MapGet("/", () => "Hello World!");

app.Run();
