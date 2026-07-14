using DietitianApp.Agent.Infrastructure.Paths;using FluentAssertions;
namespace DietitianApp.Agent.Infrastructure.Tests;public sealed class AppPathProviderTests{[Fact]public void Paths_are_below_local_app_data(){var p=new AppPathProvider();p.DatabasePath.Should().StartWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));p.DatabasePath.Should().EndWith("agent.db");}}
