using DietitianApp.Agent.Poc.Application.UseCases;
using DietitianApp.Agent.Poc.Models;
using Microsoft.Extensions.Logging;

namespace DietitianApp.Agent.Poc.Presentation;

public sealed class ConsoleInteractionService
{
    private readonly SendTestGroupMessageUseCase _useCase;
    private readonly ILogger<ConsoleInteractionService> _logger;

    public ConsoleInteractionService(SendTestGroupMessageUseCase useCase, ILogger<ConsoleInteractionService> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Diyetisyen App - WhatsApp Web Faz 0 POC");
        Console.WriteLine("Yalnizca tek bir test grubuna, onayinizla mesaj gonderir.");
        Console.WriteLine();

        var groupName = PromptRequired("Test grup adi", cancellationToken);
        var message = PromptRequired("Gonderilecek mesaj", cancellationToken);

        Console.WriteLine();
        Console.WriteLine("Mesaj gonderim onayi");
        Console.WriteLine($"Grup : {groupName}");
        Console.WriteLine($"Mesaj: {message}");
        Console.Write("Devam etmek icin EVET yazin: ");

        var approval = Console.ReadLine();
        var approved = string.Equals(approval, "EVET", StringComparison.Ordinal);
        if (!approved)
        {
            Console.WriteLine("Onay verilmedi. Mesaj gonderilmeyecek.");
        }
        else
        {
            Console.WriteLine("WhatsApp Web aciliyor. Ilk calistirmada QR kodunu taramaniz gerekebilir.");
        }

        var result = await _useCase.ExecuteAsync(
            new SendMessageRequest(groupName, message),
            approved,
            cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Automation completed: {Message}", result.Message);
            Console.WriteLine(result.Message);
            return 0;
        }

        _logger.LogWarning(
            "Automation stopped: {Message}. Screenshot: {ScreenshotPath}. Trace: {TracePath}",
            result.Message,
            result.ScreenshotPath,
            result.TracePath);

        Console.WriteLine(result.Message);
        if (!string.IsNullOrWhiteSpace(result.ScreenshotPath))
        {
            Console.WriteLine($"Screenshot: {result.ScreenshotPath}");
        }

        if (!string.IsNullOrWhiteSpace(result.TracePath))
        {
            Console.WriteLine($"Trace: {result.TracePath}");
        }

        return 1;
    }

    private static string PromptRequired(string label, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.Write($"{label}: ");
            var value = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            Console.WriteLine($"{label} bos olamaz.");
        }
    }
}
