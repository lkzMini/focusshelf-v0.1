using FocusShelf.App.Models;

namespace FocusShelf.App.Contracts;

public interface IMotivationService
{
    MotivationalMessage GetMessageFor(DateOnly date);
}
