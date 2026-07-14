using DietitianApp.Agent.Poc.Application.Abstractions;
using DietitianApp.Agent.Poc.Models;

namespace DietitianApp.Agent.Poc.Application.UseCases;

public sealed class SendTestGroupMessageUseCase
{
    private readonly IWhatsAppSessionService _sessionService;
    private readonly IWhatsAppAutomationService _automationService;

    public SendTestGroupMessageUseCase(
        IWhatsAppSessionService sessionService,
        IWhatsAppAutomationService automationService)
    {
        _sessionService = sessionService;
        _automationService = automationService;
    }

    public async Task<AutomationResult> ExecuteAsync(
        SendMessageRequest request,
        bool userApproved,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return AutomationResult.Fail(validationError);
        }

        if (!userApproved)
        {
            return AutomationResult.Fail("Kullanici onay vermedi. Mesaj gonderilmedi.");
        }

        var sessionStatus = await _sessionService.EnsureSessionAsync(cancellationToken);
        if (sessionStatus == SessionStatus.CaptchaDetected)
        {
            return AutomationResult.Fail("CAPTCHA algilandi. Islem guvenli sekilde durduruldu.");
        }

        if (sessionStatus != SessionStatus.Authenticated)
        {
            return AutomationResult.Fail($"WhatsApp oturumu hazir degil. Durum: {sessionStatus}.");
        }

        return await _automationService.SendMessageAsync(request, cancellationToken);
    }

    private static string? Validate(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GroupName))
        {
            return "Grup adi bos olamaz.";
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return "Mesaj bos olamaz.";
        }

        if (request.GroupName.Contains('\n') || request.GroupName.Contains('\r'))
        {
            return "Grup adi tek satir olmalidir.";
        }

        return null;
    }
}
