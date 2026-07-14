namespace DietitianApp.Agent.Poc.Configuration;

public sealed class WhatsAppSelectorsOptions
{
    public string[] CaptchaIndicators { get; init; } =
    [
        "text=captcha",
        "text=CAPTCHA",
        "text=robot"
    ];

    public string[] SearchBoxSelectors { get; init; } =
    [
        "input[role='textbox'][data-tab='3']",
        "input[role='textbox'][aria-label*='Arat']",
        "input[role='textbox'][aria-label*='Search']",
        "div[role='textbox'][contenteditable='true'][aria-label*='Search']",
        "div[role='textbox'][contenteditable='true'][aria-label*='Ara']",
        "div[contenteditable='true'][data-tab='3']"
    ];

    public string[] SearchActivationSelectors { get; init; } =
    [
        "#side button[aria-label*='Search']",
        "#side button[aria-label*='Ara']",
        "#side div[role='button'][aria-label*='Search']",
        "#side div[role='button'][aria-label*='Ara']"
    ];

    public string ChatTitleSelector { get; init; } = "header span[title]";

    public string[] MessageBoxSelectors { get; init; } =
    [
        "footer div[role='textbox'][contenteditable='true'][aria-label*='message']",
        "footer div[role='textbox'][contenteditable='true'][aria-label*='mesaj']",
        "footer div[contenteditable='true']"
    ];

    public string[] LoginReadySelectors { get; init; } =
    [
        "div[role='textbox'][contenteditable='true'][aria-label*='Search']",
        "div[role='textbox'][contenteditable='true'][aria-label*='Ara']",
        "div[contenteditable='true'][data-tab='3']"
    ];

    public string[] QrLoginSelectors { get; init; } =
    [
        "canvas[aria-label*='Scan']",
        "canvas",
        "text=Use WhatsApp on your computer",
        "text=WhatsApp Web"
    ];
}
