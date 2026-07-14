using System.Windows;
using DietitianApp.Agent.Desktop.ViewModels;
namespace DietitianApp.Agent.Desktop;
public partial class MainWindow:Window{public MainWindow(MainViewModel vm){InitializeComponent();DataContext=vm;}}
