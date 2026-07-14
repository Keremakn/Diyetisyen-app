using System.Windows.Input;

namespace DietitianApp.Agent.Desktop.ViewModels;

public sealed class AsyncCommand(Func<Task> execute, Func<bool>? can = null, Action<Exception>? onError = null) : ICommand
{
    private bool running;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !running && (can?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await execute();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
        finally
        {
            running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
