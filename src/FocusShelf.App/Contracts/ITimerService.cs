namespace FocusShelf.App.Contracts;

public interface ITimerService
{
    event EventHandler? Tick;

    bool IsRunning { get; }

    void Start();

    void Pause();

    void Stop();
}
