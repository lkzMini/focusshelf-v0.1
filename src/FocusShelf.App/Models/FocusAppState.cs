namespace FocusShelf.App.Models;

public sealed class FocusAppState
{
    public string CurrentFocus { get; set; } = string.Empty;

    public string NextStep { get; set; } = string.Empty;

    public string QuickNote { get; set; } = string.Empty;

    public FocusSessionState Timer { get; set; } = new();

    public DailyProgress DailyProgress { get; set; } = DailyProgress.CreateForToday();
}
