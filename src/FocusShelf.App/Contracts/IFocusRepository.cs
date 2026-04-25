using FocusShelf.App.Models;

namespace FocusShelf.App.Contracts;

public interface IFocusRepository
{
    Task<FocusAppState> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(FocusAppState state, CancellationToken cancellationToken = default);
}
