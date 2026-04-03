namespace PersonalProjects.Services;

public class DebounceService : IDisposable
{
    private CancellationTokenSource? _cts;

    public async Task DebounceAsync(Func<Task> action, int delayMs = 500)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        try
        {
            await Task.Delay(delayMs, token);
            if (!token.IsCancellationRequested)
                await action();
        }
        catch (TaskCanceledException) { /* debounced — expected */ }
    }

    public void Dispose() => _cts?.Cancel();
}
