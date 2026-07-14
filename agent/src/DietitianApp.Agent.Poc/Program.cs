using DietitianApp.Agent.Poc.Application.Abstractions;
using DietitianApp.Agent.Poc.Application.UseCases;
using DietitianApp.Agent.Poc.Configuration;
using DietitianApp.Agent.Poc.Infrastructure.Artifacts;
using DietitianApp.Agent.Poc.Infrastructure.WhatsApp;
using DietitianApp.Agent.Poc.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text;

Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<WhatsAppSelectorsOptions>(builder.Configuration.GetSection("WhatsAppSelectors"));

builder.Services.AddSingleton<WhatsAppBrowserSession>();
builder.Services.AddSingleton<ScreenshotService>();
builder.Services.AddScoped<IWhatsAppSessionService, WhatsAppSessionService>();
builder.Services.AddScoped<IWhatsAppAutomationService, WhatsAppAutomationService>();
builder.Services.AddScoped<SendTestGroupMessageUseCase>();
builder.Services.AddScoped<ConsoleInteractionService>();

using var host = builder.Build();
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
    Console.WriteLine("Kapanis isteniyor. Devam eden islem guvenli sekilde durdurulacak.");
};

try
{
    Directory.CreateDirectory("logs");
    Directory.CreateDirectory("artifacts/screenshots");
    Directory.CreateDirectory("artifacts/traces");

    var console = host.Services.GetRequiredService<ConsoleInteractionService>();
    return await console.RunAsync(cancellation.Token);
}
catch (OperationCanceledException)
{
    Log.Information("Application cancelled by user.");
    return 130;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled application error.");
    Console.WriteLine("Beklenmeyen hata olustu. Ayrintilar log dosyasina yazildi.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
