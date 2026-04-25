using System.Text.Json;
using System.Text.Json.Serialization;
using FocusShelf.App.Contracts;
using FocusShelf.App.Models;

namespace FocusShelf.App.Repositories;

public sealed class JsonFocusRepository : IFocusRepository
{
    private const string AppFolderName = "FocusShelf";
    private const string StateFileName = "state.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _stateFilePath;

    public JsonFocusRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, AppFolderName);
        _stateFilePath = Path.Combine(appFolder, StateFileName);
    }

    public async Task<FocusAppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_stateFilePath))
        {
            return CreateDefaultState();
        }

        try
        {
            await using var stream = File.OpenRead(_stateFilePath);
            var state = await JsonSerializer.DeserializeAsync<FocusAppState>(
                stream,
                SerializerOptions,
                cancellationToken);

            return Normalize(state);
        }
        catch (JsonException)
        {
            return CreateDefaultState();
        }
        catch (IOException)
        {
            return CreateDefaultState();
        }
        catch (UnauthorizedAccessException)
        {
            return CreateDefaultState();
        }
    }

    public async Task SaveAsync(FocusAppState state, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFilePath = $"{_stateFilePath}.tmp";

        try
        {
            await using (var stream = File.Create(tempFilePath))
            {
                await JsonSerializer.SerializeAsync(stream, Normalize(state), SerializerOptions, cancellationToken);
            }

            File.Move(tempFilePath, _stateFilePath, true);
        }
        catch (IOException)
        {
            TryDeleteTempFile(tempFilePath);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            TryDeleteTempFile(tempFilePath);
            throw;
        }
    }

    private static FocusAppState Normalize(FocusAppState? state)
    {
        state ??= CreateDefaultState();
        state.Timer ??= new FocusSessionState();
        state.Timer.EnsureValid();

        state.DailyProgress ??= DailyProgress.CreateForToday();
        state.DailyProgress.DoneItems ??= [];

        if (!state.DailyProgress.IsForToday())
        {
            state.DailyProgress = DailyProgress.CreateForToday();
        }

        state.CurrentFocus ??= string.Empty;
        state.NextStep ??= string.Empty;
        state.QuickNote ??= string.Empty;

        return state;
    }

    private static FocusAppState CreateDefaultState()
    {
        return new FocusAppState
        {
            CurrentFocus = string.Empty,
            NextStep = string.Empty,
            QuickNote = string.Empty,
            Timer = new FocusSessionState(),
            DailyProgress = DailyProgress.CreateForToday()
        };
    }

    private static void TryDeleteTempFile(string tempFilePath)
    {
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
