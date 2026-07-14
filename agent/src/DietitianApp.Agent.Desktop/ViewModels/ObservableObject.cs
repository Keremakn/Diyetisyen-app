using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace DietitianApp.Agent.Desktop.ViewModels;
public abstract class ObservableObject:INotifyPropertyChanged{public event PropertyChangedEventHandler? PropertyChanged;protected bool Set<T>(ref T field,T value,[CallerMemberName]string? name=null){if(EqualityComparer<T>.Default.Equals(field,value))return false;field=value;PropertyChanged?.Invoke(this,new(name));return true;}protected void Raise(string name)=>PropertyChanged?.Invoke(this,new(name));}
