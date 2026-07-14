using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Application.Services;
using DietitianApp.Agent.Infrastructure.Configuration;
using DietitianApp.Agent.Infrastructure.Paths;
using DietitianApp.Agent.Infrastructure.Persistence;
using DietitianApp.Agent.Infrastructure.Services;
using DietitianApp.Agent.Infrastructure.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DietitianApp.Agent.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddAgentInfrastructure(this IServiceCollection services,IConfiguration config)
    {
        services.Configure<WhatsAppOptions>(config.GetSection(WhatsAppOptions.Section));services.Configure<WhatsAppSelectors>(config.GetSection("WhatsAppSelectors"));
        services.AddSingleton<IAppPathProvider,AppPathProvider>();services.AddSingleton<IWhatsAppGateway,PlaywrightWhatsAppGateway>();services.AddSingleton<IArtifactService>(sp=>(PlaywrightWhatsAppGateway)sp.GetRequiredService<IWhatsAppGateway>());
        services.AddDbContext<AgentDbContext>((sp,o)=>o.UseSqlite($"Data Source={sp.GetRequiredService<IAppPathProvider>().DatabasePath}"));services.AddScoped<IApplicationDbContext>(sp=>sp.GetRequiredService<AgentDbContext>());
        services.AddScoped<IGroupService,GroupService>();services.AddScoped<IMessageTemplateService,MessageTemplateService>();services.AddScoped<IBatchSendService,BatchSendService>();services.AddSingleton<IClock,SystemClock>();services.AddScoped<DatabaseInitializer>();return services;
    }
}
