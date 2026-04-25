namespace FocusShelf.App.Models;

public sealed class FocusSessionState
{
    public int DurationSeconds { get; set; } = 25 * 60;

    public int RemainingSeconds { get; set; } = 25 * 60;

    public bool WasRunningOnLastSave { get; set; }

    public void EnsureValid()
    {
        if (DurationSeconds <= 0)
        {
            DurationSeconds = 25 * 60;
        }

        if (RemainingSeconds <= 0 || RemainingSeconds > DurationSeconds)
        {
            RemainingSeconds = DurationSeconds;
        }

        WasRunningOnLastSave = false;
    }
}
