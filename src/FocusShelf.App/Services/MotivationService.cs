using FocusShelf.App.Contracts;
using FocusShelf.App.Models;

namespace FocusShelf.App.Services;

public sealed class MotivationService : IMotivationService
{
    private static readonly string[] Messages =
    [
        "Start small. Small still counts.",
        "You do not need to solve everything today.",
        "One clear step is enough for now.",
        "A quiet start is still a start.",
        "Put one thing gently in front of you.",
        "Less noise. One next step.",
        "Your pace is allowed to be human.",
        "Do the kind part first.",
        "Clarity can be small.",
        "You can return to this moment.",
        "A focused minute is not wasted.",
        "Make it simple enough to begin.",
        "There is no need to rush your attention.",
        "Today can be lighter than your list.",
        "Stay with the next honest step."
    ];

    public MotivationalMessage GetMessageFor(DateOnly date)
    {
        var dayNumber = date.DayNumber;
        var index = Math.Abs(dayNumber) % Messages.Length;
        return new MotivationalMessage(Messages[index]);
    }
}
