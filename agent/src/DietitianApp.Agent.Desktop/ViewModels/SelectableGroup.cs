using DietitianApp.Agent.Domain.Entities;

namespace DietitianApp.Agent.Desktop.ViewModels;

public sealed class SelectableGroup(WhatsAppGroup group) : ObservableObject
{
    private bool selected;

    public WhatsAppGroup Group { get; } = group;
    public bool IsSelected { get => selected; set => Set(ref selected, value); }
    public bool CanSend => Group.IsActive && Group.IsVerified;
    public string State => !Group.IsVerified ? "Dogrulanmamis" : !Group.IsActive ? "Pasif" : "Hazir";
}
