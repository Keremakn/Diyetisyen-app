namespace DietitianApp.Agent.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
