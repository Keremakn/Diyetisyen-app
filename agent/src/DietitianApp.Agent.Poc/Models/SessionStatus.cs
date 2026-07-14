namespace DietitianApp.Agent.Poc.Models;

public enum SessionStatus
{
    Unknown = 0,
    RequiresQrLogin = 1,
    Authenticated = 2,
    CaptchaDetected = 3,
    Failed = 4
}
