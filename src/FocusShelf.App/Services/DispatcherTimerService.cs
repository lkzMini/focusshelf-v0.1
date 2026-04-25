using FocusShelf.App.Contracts;
using Microsoft.UI.Dispatching;

namespace FocusShelf.App.Services;

public sealed class DispatcherTimerService : ITimerService
{
    private readonly DispatcherQueueTimer _timer;

    public DispatcherTimerService(DispatcherQueue dispatcherQueue)
    {
        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public bool IsRunning => _timer.IsRunning;

    public void Start()
    {
        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
    }

    public void Pause()
    {
        if (_timer.IsRunning)
        {
            _timer.Stop();
        }
    }

    public void Stop()
    {
        Pause();
    }
}
