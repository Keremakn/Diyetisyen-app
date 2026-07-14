using DietitianApp.Agent.Poc.Application.Abstractions;
using DietitianApp.Agent.Poc.Application.UseCases;
using DietitianApp.Agent.Poc.Models;

namespace DietitianApp.Agent.Poc.Tests;

public sealed class SendTestGroupMessageUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotApprove_DoesNotOpenSessionOrSend()
    {
        var session = new FakeSessionService(SessionStatus.Authenticated);
        var automation = new FakeAutomationService();
        var sut = new SendTestGroupMessageUseCase(session, automation);

        var result = await sut.ExecuteAsync(new SendMessageRequest("Test Grup", "Merhaba"), false, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, session.CallCount);
        Assert.Equal(0, automation.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGroupNameIsEmpty_DoesNotSend()
    {
        var session = new FakeSessionService(SessionStatus.Authenticated);
        var automation = new FakeAutomationService();
        var sut = new SendTestGroupMessageUseCase(session, automation);

        var result = await sut.ExecuteAsync(new SendMessageRequest("", "Merhaba"), true, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, session.CallCount);
        Assert.Equal(0, automation.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSessionIsAuthenticated_SendsSingleRequest()
    {
        var session = new FakeSessionService(SessionStatus.Authenticated);
        var automation = new FakeAutomationService();
        var sut = new SendTestGroupMessageUseCase(session, automation);

        var result = await sut.ExecuteAsync(new SendMessageRequest("Test Grup", "Merhaba"), true, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, session.CallCount);
        Assert.Equal(1, automation.CallCount);
        Assert.Equal("Test Grup", automation.LastRequest?.GroupName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptchaDetected_DoesNotSend()
    {
        var session = new FakeSessionService(SessionStatus.CaptchaDetected);
        var automation = new FakeAutomationService();
        var sut = new SendTestGroupMessageUseCase(session, automation);

        var result = await sut.ExecuteAsync(new SendMessageRequest("Test Grup", "Merhaba"), true, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(1, session.CallCount);
        Assert.Equal(0, automation.CallCount);
    }

    private sealed class FakeSessionService : IWhatsAppSessionService
    {
        private readonly SessionStatus _status;

        public FakeSessionService(SessionStatus status)
        {
            _status = status;
        }

        public int CallCount { get; private set; }

        public Task<SessionStatus> EnsureSessionAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_status);
        }
    }

    private sealed class FakeAutomationService : IWhatsAppAutomationService
    {
        public int CallCount { get; private set; }

        public SendMessageRequest? LastRequest { get; private set; }

        public Task<AutomationResult> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(AutomationResult.Ok("sent"));
        }
    }
}
