namespace DietitianApp.Agent.Application.Common;

public sealed class ApplicationValidationException(string message) : Exception(message);
