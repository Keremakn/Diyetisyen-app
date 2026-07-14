namespace DietitianApp.Agent.Infrastructure.Configuration;
public sealed class WhatsAppSelectors
{
    public string[] SearchBoxes { get; init; } = ["input[role='textbox'][data-tab='3']", "#side div[role='textbox'][contenteditable='true']"];
    public string[] MessageBoxes { get; init; } = ["footer div[role='textbox'][contenteditable='true'][aria-label*='message']", "footer div[role='textbox'][contenteditable='true'][aria-label*='mesaj']", "footer div[contenteditable='true']"];
    public string[] Ready { get; init; } = ["#pane-side", "[data-testid='chat-list']", "[aria-label='Sohbet listesi']", "[aria-label='Chat list']"];
    public string[] Qr { get; init; } = ["canvas[aria-label*='Scan']", "canvas"];
    public string[] Captcha { get; init; } = ["text=captcha", "text=CAPTCHA", "text=robot"];
}
