using System.Windows;
using System.IO;
using DietitianApp.Agent.Infrastructure;
using DietitianApp.Agent.Infrastructure.Paths;
using DietitianApp.Agent.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DietitianApp.Agent.Desktop;
public partial class App : System.Windows.Application
{
    private IHost? host;
    private IServiceScope? windowScope;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var builder=Host.CreateApplicationBuilder(e.Args);builder.Configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json",false).AddEnvironmentVariables();
            var paths=new AppPathProvider();paths.EnsureDirectories();Log.Logger=new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(Path.Combine(paths.LogsPath,"agent-.log"),rollingInterval:RollingInterval.Day,retainedFileCountLimit:14).CreateLogger();
            builder.Logging.ClearProviders();builder.Logging.AddSerilog(Log.Logger);builder.Services.AddAgentInfrastructure(builder.Configuration);builder.Services.AddTransient<ViewModels.MainViewModel>();builder.Services.AddTransient<MainWindow>();host=builder.Build();
            DispatcherUnhandledException+=(_,a)=>{Log.Error(a.Exception,"Unhandled UI error");MessageBox.Show("Beklenmeyen bir hata oluştu. Ayrıntılar log dosyasına yazıldı.");a.Handled=true;};
            using(var startupScope=host.Services.CreateScope())await startupScope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();windowScope=host.Services.CreateScope();var mainWindow=windowScope.ServiceProvider.GetRequiredService<MainWindow>();MainWindow=mainWindow;mainWindow.WindowStartupLocation=WindowStartupLocation.CenterScreen;mainWindow.ShowInTaskbar=true;mainWindow.Topmost=true;mainWindow.Show();mainWindow.Activate();mainWindow.Topmost=false;Log.Information("Desktop agent started");
        }
        catch(Exception ex){Log.Fatal(ex,"Startup failed");MessageBox.Show("Uygulama başlatılamadı. Log dosyasını kontrol edin.");Shutdown(1);}
    }
    protected override async void OnExit(ExitEventArgs e){windowScope?.Dispose();if(host is not null)await host.StopAsync();await Log.CloseAndFlushAsync();host?.Dispose();base.OnExit(e);}
}
