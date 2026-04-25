using System.Collections.ObjectModel;

namespace FocusShelf.App.Models;

public sealed class DailyProgress
{
    public DateOnly Date { get; set; }

    public int CompletedFocusSessions { get; set; }

    public ObservableCollection<string> DoneItems { get; set; } = [];

    public static DailyProgress CreateForToday()
    {
        return new DailyProgress
        {
            Date = DateOnly.FromDateTime(DateTime.Today)
        };
    }

    public bool IsForToday()
    {
        return Date == DateOnly.FromDateTime(DateTime.Today);
    }
}
