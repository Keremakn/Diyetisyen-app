using System.Collections.ObjectModel;
using System.Windows;
using DietitianApp.Agent.Application.Abstractions;
using DietitianApp.Agent.Application.Common;
using DietitianApp.Agent.Application.Models;
using DietitianApp.Agent.Domain.Entities;
using DietitianApp.Agent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietitianApp.Agent.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IGroupService groups;
    private readonly IMessageTemplateService templates;
    private readonly IBatchSendService batches;
    private readonly IWhatsAppGateway gateway;
    private readonly AgentDbContext db;
    private CancellationTokenSource? sendCancellation;
    private string page = "Dashboard";
    private int selectedPageIndex;
    private string status = "Hazir";
    private string externalName = "";
    private string displayName = "";
    private string message = "";
    private MessageTemplate? selectedTemplate;
    private SelectableGroup? selectedGroup;
    private MessageTemplate? selectedTemplateForEdit;
    private SendBatch? selectedHistory;
    private string templateName = "";
    private string templateContent = "";
    private int progress;

    public MainViewModel(IGroupService groups, IMessageTemplateService templates, IBatchSendService batches, IWhatsAppGateway gateway, AgentDbContext db)
    {
        this.groups = groups;
        this.templates = templates;
        this.batches = batches;
        this.gateway = gateway;
        this.db = db;

        RefreshCommand = new(LoadAsync, onError: SetUnexpectedError);
        NewGroupCommand = new(NewGroupAsync, onError: SetUnexpectedError);
        SaveGroupCommand = new(SaveGroupAsync, onError: SetUnexpectedError);
        ToggleGroupActiveCommand = new(ToggleGroupActiveAsync, () => SelectedGroup is not null, SetUnexpectedError);
        VerifySelectedGroupCommand = new(VerifySelectedGroupAsync, () => SelectedGroup is not null, SetUnexpectedError);
        NewTemplateCommand = new(NewTemplateAsync, onError: SetUnexpectedError);
        SaveTemplateCommand = new(SaveTemplateAsync, onError: SetUnexpectedError);
        ToggleTemplateActiveCommand = new(ToggleTemplateActiveAsync, () => SelectedTemplateForEdit is not null, SetUnexpectedError);
        VerifyConnectionCommand = new(VerifyConnectionAsync, onError: SetUnexpectedError);
        SendCommand = new(SendAsync, onError: SetUnexpectedError);
        CancelCommand = new(() => { sendCancellation?.Cancel(); return Task.CompletedTask; }, onError: SetUnexpectedError);
        SelectAllCommand = new(SelectAllAsync, onError: SetUnexpectedError);
        ClearSelectionCommand = new(ClearSelectionAsync, onError: SetUnexpectedError);
        RetryFailuresCommand = new(RetryFailuresAsync, () => SelectedHistory?.FailureCount > 0, SetUnexpectedError);
        RetryUnsuccessfulCommand = new(RetryUnsuccessfulAsync, () => SelectedHistory is not null && (SelectedHistory.FailureCount > 0 || SelectedHistory.CancelledCount > 0), SetUnexpectedError);

        _ = LoadAsync();
    }

    public string CurrentPage { get => page; set => Set(ref page, value); }
    public string Status { get => status; set => Set(ref status, value); }
    public string ExternalName { get => externalName; set => Set(ref externalName, value); }
    public string DisplayName { get => displayName; set => Set(ref displayName, value); }
    public string TemplateName { get => templateName; set => Set(ref templateName, value); }
    public string TemplateContent { get => templateContent; set => Set(ref templateContent, value); }
    public string Message { get => message; set => Set(ref message, value); }
    public int ProgressValue { get => progress; set => Set(ref progress, value); }
    public int SelectedCount => Groups.Count(x => x.IsSelected);

    public MessageTemplate? SelectedTemplate
    {
        get => selectedTemplate;
        set
        {
            if (Set(ref selectedTemplate, value) && value is not null)
                Message = value.Content;
        }
    }

    public SelectableGroup? SelectedGroup
    {
        get => selectedGroup;
        set
        {
            if (!Set(ref selectedGroup, value))
                return;

            ExternalName = value?.Group.ExternalName ?? "";
            DisplayName = value?.Group.DisplayName ?? "";
            ToggleGroupActiveCommand.Refresh();
            VerifySelectedGroupCommand.Refresh();
        }
    }

    public MessageTemplate? SelectedTemplateForEdit
    {
        get => selectedTemplateForEdit;
        set
        {
            if (!Set(ref selectedTemplateForEdit, value))
                return;

            TemplateName = value?.Name ?? "";
            TemplateContent = value?.Content ?? "";
            ToggleTemplateActiveCommand.Refresh();
        }
    }

    public SendBatch? SelectedHistory
    {
        get => selectedHistory;
        set
        {
            if (!Set(ref selectedHistory, value))
                return;

            HistoryItems.Clear();
            if (value is not null)
            {
                foreach (var item in value.Items.OrderBy(x => x.GroupNameSnapshot))
                    HistoryItems.Add(item);
            }
            RetryFailuresCommand.Refresh();
            RetryUnsuccessfulCommand.Refresh();
        }
    }

    public int SelectedPageIndex
    {
        get => selectedPageIndex;
        set
        {
            if (Set(ref selectedPageIndex, value))
                CurrentPage = new[] { "Dashboard", "Gruplar", "Mesaj Sablonlari", "Yeni Gonderim", "Gonderim Gecmisi", "Ayarlar / WhatsApp" }[value];
        }
    }

    public ObservableCollection<SelectableGroup> Groups { get; } = [];
    public ObservableCollection<MessageTemplate> Templates { get; } = [];
    public ObservableCollection<SendBatch> History { get; } = [];
    public ObservableCollection<SendBatchItem> HistoryItems { get; } = [];
    public IEnumerable<MessageTemplate> ActiveTemplates => Templates.Where(x => x.IsActive);

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand NewGroupCommand { get; }
    public AsyncCommand SaveGroupCommand { get; }
    public AsyncCommand ToggleGroupActiveCommand { get; }
    public AsyncCommand VerifySelectedGroupCommand { get; }
    public AsyncCommand NewTemplateCommand { get; }
    public AsyncCommand SaveTemplateCommand { get; }
    public AsyncCommand ToggleTemplateActiveCommand { get; }
    public AsyncCommand VerifyConnectionCommand { get; }
    public AsyncCommand SendCommand { get; }
    public AsyncCommand CancelCommand { get; }
    public AsyncCommand SelectAllCommand { get; }
    public AsyncCommand ClearSelectionCommand { get; }
    public AsyncCommand RetryFailuresCommand { get; }
    public AsyncCommand RetryUnsuccessfulCommand { get; }

    private async Task LoadAsync()
    {
        Groups.Clear();
        foreach (var group in await groups.GetAllAsync(default))
        {
            var item = new SelectableGroup(group);
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SelectableGroup.IsSelected))
                {
                    Raise(nameof(SelectedCount));
                    SendCommand.Refresh();
                }
            };
            Groups.Add(item);
        }

        Templates.Clear();
        foreach (var template in await templates.GetAllAsync(default))
            Templates.Add(template);
        Raise(nameof(ActiveTemplates));

        History.Clear();
        var history = await db.SendBatches.Include(x => x.Items).ToListAsync();
        foreach (var batch in history.OrderByDescending(x => x.CreatedAtUtc).Take(100))
            History.Add(batch);

        SelectedHistory = History.FirstOrDefault();
        Status = "Veriler yuklendi.";
    }

    private Task NewGroupAsync()
    {
        SelectedGroup = null;
        ExternalName = "";
        DisplayName = "";
        Status = "Yeni grup girisi hazir.";
        return Task.CompletedTask;
    }

    private async Task SaveGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(ExternalName) || string.IsNullOrWhiteSpace(DisplayName))
        {
            Status = "Grup adlari bos olamaz.";
            return;
        }

        var verification = await gateway.VerifyGroupAsync(ExternalName, default);
        if (!verification.IsExactMatch)
        {
            Status = verification.ErrorMessage ?? "Grup dogrulanamadi; kaydedilmedi.";
            return;
        }

        if (MessageBox.Show($"'{ExternalName}' tam eslesti. Kaydedilsin mi?", "Grup dogrulama", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        var group = SelectedGroup?.Group ?? new WhatsAppGroup();
        group.ExternalName = ExternalName;
        group.DisplayName = DisplayName;
        group.IsActive = true;
        group.IsVerified = true;
        group.VerifiedAtUtc = DateTimeOffset.UtcNow;

        await groups.SaveAsync(group, default);
        await LoadAsync();
        Status = "Grup kaydedildi ve dogrulandi.";
    }

    private async Task ToggleGroupActiveAsync()
    {
        if (SelectedGroup is null)
            return;

        SelectedGroup.Group.IsActive = !SelectedGroup.Group.IsActive;
        await groups.SaveAsync(SelectedGroup.Group, default);
        await LoadAsync();
        Status = "Grup aktif/pasif durumu guncellendi.";
    }

    private async Task VerifySelectedGroupAsync()
    {
        if (SelectedGroup is null)
            return;

        var ok = await groups.VerifyAsync(SelectedGroup.Group.Id, default);
        await LoadAsync();
        Status = ok ? "Grup dogrulandi." : "Grup dogrulanamadi.";
    }

    private Task NewTemplateAsync()
    {
        SelectedTemplateForEdit = null;
        TemplateName = "";
        TemplateContent = "";
        Status = "Yeni sablon girisi hazir.";
        return Task.CompletedTask;
    }

    private async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            Status = "Sablon adi bos olamaz.";
            return;
        }
        if (string.IsNullOrWhiteSpace(TemplateContent))
        {
            Status = "Sablon icerigi bos olamaz.";
            return;
        }

        var item = SelectedTemplateForEdit ?? new MessageTemplate { IsActive = true };
        item.Name = TemplateName.Trim();
        item.Content = TemplateContent.Trim();
        await templates.SaveAsync(item, default);
        await LoadAsync();
        Status = "Sablon kaydedildi.";
    }

    private async Task ToggleTemplateActiveAsync()
    {
        if (SelectedTemplateForEdit is null)
            return;

        SelectedTemplateForEdit.IsActive = !SelectedTemplateForEdit.IsActive;
        await templates.SaveAsync(SelectedTemplateForEdit, default);
        await LoadAsync();
        Status = "Sablon aktif/pasif durumu guncellendi.";
    }

    private async Task VerifyConnectionAsync()
    {
        Status = "WhatsApp baglantisi kontrol ediliyor...";
        var result = await gateway.EnsureSessionAsync(default);
        Status = result.Success ? "WhatsApp baglantisi hazir." : result.ErrorMessage ?? "Baglanti kurulamadi.";
    }

    private Task SelectAllAsync()
    {
        foreach (var x in Groups.Where(x => x.Group.IsActive && x.Group.IsVerified))
            x.IsSelected = true;
        Raise(nameof(SelectedCount));
        SendCommand.Refresh();
        return Task.CompletedTask;
    }

    private Task ClearSelectionAsync()
    {
        foreach (var x in Groups)
            x.IsSelected = false;
        Raise(nameof(SelectedCount));
        SendCommand.Refresh();
        return Task.CompletedTask;
    }

    private async Task SendAsync()
    {
        var selected = Groups.Where(x => x.IsSelected).Select(x => x.Group).ToList();
        if (selected.Count == 0)
        {
            Status = "En az bir grup secilmelidir.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Message))
        {
            Status = "Mesaj bos olamaz.";
            return;
        }

        var preview = $"Grup sayisi: {selected.Count}\n\n{string.Join("\n", selected.Select(x => x.ExternalName))}\n\nMesaj:\n{Message}";
        if (MessageBox.Show(preview, "Gonderimi acikca onayliyor musunuz?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        {
            Status = "Gonderim onaylanmadi.";
            return;
        }

        await RunBatchAsync(async token => await batches.CreateAsync(new CreateBatchRequest(selected.Select(x => x.Id).ToArray(), Message), token));
    }

    private async Task RetryFailuresAsync()
    {
        if (SelectedHistory is null || SelectedHistory.FailureCount == 0)
        {
            Status = "Yeniden denenecek basarisiz kayit yok.";
            return;
        }

        if (MessageBox.Show("Yalnizca basarisiz gruplar yeniden denenecek. Devam edilsin mi?", "Basarisizlari yeniden dene", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        await RunBatchAsync(token => batches.RetryFailuresAsync(SelectedHistory.Id, CreateProgress(), token), createOnlyStartsBatch: false);
    }

    private async Task RetryUnsuccessfulAsync()
    {
        if (SelectedHistory is null || (SelectedHistory.FailureCount == 0 && SelectedHistory.CancelledCount == 0))
        {
            Status = "Yeniden denenecek hatali veya iptal edilmis kayit yok.";
            return;
        }

        if (MessageBox.Show("Basarili kayitlara dokunulmayacak. Hatali ve iptal edilen gruplar yeniden denenecek. Devam edilsin mi?", "Hatali ve iptal edilenleri yeniden dene", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        await RunBatchAsync(token => batches.RetryUnsuccessfulAsync(SelectedHistory.Id, CreateProgress(), token), createOnlyStartsBatch: false);
    }

    private async Task RunBatchAsync(Func<CancellationToken, Task<SendBatch>> createBatch, bool createOnlyStartsBatch = true)
    {
        try
        {
            sendCancellation = new();
            if (createOnlyStartsBatch)
            {
                var batch = await createBatch(sendCancellation.Token);
                await batches.StartAsync(batch.Id, CreateProgress(), sendCancellation.Token);
            }
            else
            {
                await createBatch(sendCancellation.Token);
            }
            await LoadAsync();
        }
        catch (ApplicationValidationException ex)
        {
            Status = ex.Message;
        }
        catch (OperationCanceledException)
        {
            Status = "Gonderim iptal edildi.";
        }
        finally
        {
            sendCancellation?.Dispose();
            sendCancellation = null;
        }
    }

    private IProgress<BatchProgress> CreateProgress() =>
        new Progress<BatchProgress>(x =>
        {
            ProgressValue = x.Total == 0 ? 0 : x.Completed * 100 / x.Total;
            Status = $"{x.GroupName}: {x.Status}";
        });

    private void SetUnexpectedError(Exception ex) => Status = $"Beklenmeyen hata: {ex.Message}";
}
