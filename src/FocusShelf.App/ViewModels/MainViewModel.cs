using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using FocusShelf.App.Contracts;
using FocusShelf.App.Helpers;
using FocusShelf.App.Models;
using FocusShelf.App.Settings;
using Microsoft.UI.Dispatching;

namespace FocusShelf.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IFocusRepository _repository;
    private readonly ITimerService _timerService;
    private readonly IMotivationService _motivationService;
    private readonly DispatcherQueueTimer _saveDebounceTimer;

    private string _currentFocus = string.Empty;
    private string _nextStep = string.Empty;
    private string _quickNote = string.Empty;
    private string _addDoneText = string.Empty;
    private int _remainingSeconds = FocusShelfDefaults.TimerDurationSeconds;
    private int _durationSeconds = FocusShelfDefaults.TimerDurationSeconds;
    private int _completedFocusSessions;
    private bool _isTimerRunning;
    private bool _isLoaded;
    private string _saveStatusText = "Saved locally";

    public MainViewModel(
        IFocusRepository repository,
        ITimerService timerService,
        IMotivationService motivationService,
        DispatcherQueue dispatcherQueue)
    {
        _repository = repository;
        _timerService = timerService;
        _motivationService = motivationService;

        DailyMessage = _motivationService.GetMessageFor(DateOnly.FromDateTime(DateTime.Today)).Text;

        StartTimerCommand = new RelayCommand(StartTimer, () => !IsTimerRunning && RemainingSeconds > 0);
        PauseTimerCommand = new RelayCommand(PauseTimer, () => IsTimerRunning);
        ResetTimerCommand = new RelayCommand(ResetTimer);
        AddDoneItemCommand = new RelayCommand(AddDoneItem, () => !string.IsNullOrWhiteSpace(AddDoneText));
        RemoveDoneItemCommand = new RelayCommand<string>(RemoveDoneItem, item => !string.IsNullOrWhiteSpace(item));
        MarkCurrentFocusDoneCommand = new RelayCommand(MarkCurrentFocusDone, () => !string.IsNullOrWhiteSpace(CurrentFocus));
        ClearTodayProgressCommand = new RelayCommand(ClearTodayProgress, () => CompletedFocusSessions > 0 || DoneItems.Count > 0);
        SaveNowCommand = new AsyncRelayCommand(SaveAsync);

        _timerService.Tick += OnTimerTick;

        _saveDebounceTimer = dispatcherQueue.CreateTimer();
        _saveDebounceTimer.Interval = TimeSpan.FromMilliseconds(700);
        _saveDebounceTimer.Tick += async (_, _) =>
        {
            _saveDebounceTimer.Stop();
            await SaveAsync();
        };

        DoneItems.CollectionChanged += OnDoneItemsChanged;
    }

    public string CurrentFocus
    {
        get => _currentFocus;
        set
        {
            if (SetProperty(ref _currentFocus, value))
            {
                RaiseCommandStates();
                QueueSave();
            }
        }
    }

    public string NextStep
    {
        get => _nextStep;
        set
        {
            if (SetProperty(ref _nextStep, value))
            {
                QueueSave();
            }
        }
    }

    public string QuickNote
    {
        get => _quickNote;
        set
        {
            if (SetProperty(ref _quickNote, value))
            {
                QueueSave();
            }
        }
    }

    public string AddDoneText
    {
        get => _addDoneText;
        set
        {
            if (SetProperty(ref _addDoneText, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        private set
        {
            if (SetProperty(ref _remainingSeconds, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(TimerDisplay));
                OnPropertyChanged(nameof(TimerStatusText));
                RaiseCommandStates();
                QueueSave();
            }
        }
    }

    public int CompletedFocusSessions
    {
        get => _completedFocusSessions;
        private set
        {
            if (SetProperty(ref _completedFocusSessions, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(ProgressSummary));
                RaiseCommandStates();
                QueueSave();
            }
        }
    }

    public bool IsTimerRunning
    {
        get => _isTimerRunning;
        private set
        {
            if (SetProperty(ref _isTimerRunning, value))
            {
                OnPropertyChanged(nameof(TimerStatusText));
                RaiseCommandStates();
                QueueSave();
            }
        }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        private set => SetProperty(ref _isLoaded, value);
    }

    public string DailyMessage { get; }

    public ObservableCollection<string> DoneItems { get; } = [];

    public string TimerDisplay => TimeSpan.FromSeconds(RemainingSeconds).ToString(@"mm\:ss");

    public string TimerStatusText
    {
        get
        {
            if (IsTimerRunning)
            {
                return "A quiet focus window is running.";
            }

            return RemainingSeconds == 0
                ? "Nice. That focus window is complete."
                : "Ready when you are.";
        }
    }

    public string ProgressSummary
    {
        get
        {
            var sessionLabel = CompletedFocusSessions == 1 ? "focus window" : "focus windows";
            var itemLabel = DoneItems.Count == 1 ? "small win" : "small wins";
            return $"{CompletedFocusSessions} {sessionLabel} · {DoneItems.Count} {itemLabel}";
        }
    }

    public string SaveStatusText
    {
        get => _saveStatusText;
        private set => SetProperty(ref _saveStatusText, value);
    }

    public ICommand StartTimerCommand { get; }

    public ICommand PauseTimerCommand { get; }

    public ICommand ResetTimerCommand { get; }

    public ICommand AddDoneItemCommand { get; }

    public ICommand RemoveDoneItemCommand { get; }

    public ICommand MarkCurrentFocusDoneCommand { get; }

    public ICommand ClearTodayProgressCommand { get; }

    public ICommand SaveNowCommand { get; }

    public async Task LoadAsync()
    {
        var state = await _repository.LoadAsync();

        CurrentFocus = state.CurrentFocus;
        NextStep = state.NextStep;
        QuickNote = state.QuickNote;

        _durationSeconds = state.Timer.DurationSeconds <= 0
            ? FocusShelfDefaults.TimerDurationSeconds
            : state.Timer.DurationSeconds;

        RemainingSeconds = state.Timer.RemainingSeconds <= 0
            ? _durationSeconds
            : Math.Min(state.Timer.RemainingSeconds, _durationSeconds);

        CompletedFocusSessions = state.DailyProgress.CompletedFocusSessions;

        DoneItems.CollectionChanged -= OnDoneItemsChanged;
        DoneItems.Clear();
        foreach (var item in state.DailyProgress.DoneItems.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            DoneItems.Add(item.Trim());
        }
        DoneItems.CollectionChanged += OnDoneItemsChanged;

        IsTimerRunning = false;
        IsLoaded = true;
        SaveStatusText = "Saved locally";
        RaiseCommandStates();
        await SaveAsync();
    }

    public async Task SaveAsync()
    {
        if (!IsLoaded)
        {
            return;
        }

        var state = new FocusAppState
        {
            CurrentFocus = CurrentFocus,
            NextStep = NextStep,
            QuickNote = QuickNote,
            Timer = new FocusSessionState
            {
                DurationSeconds = _durationSeconds,
                RemainingSeconds = RemainingSeconds == 0 ? _durationSeconds : RemainingSeconds,
                WasRunningOnLastSave = IsTimerRunning
            },
            DailyProgress = new DailyProgress
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                CompletedFocusSessions = CompletedFocusSessions,
                DoneItems = new ObservableCollection<string>(DoneItems)
            }
        };

        try
        {
            await _repository.SaveAsync(state);
            SaveStatusText = "Saved locally";
        }
        catch
        {
            SaveStatusText = "Could not save locally";
        }
    }

    private void StartTimer()
    {
        if (RemainingSeconds <= 0)
        {
            return;
        }

        _timerService.Start();
        IsTimerRunning = true;
    }

    private void PauseTimer()
    {
        _timerService.Pause();
        IsTimerRunning = false;
    }

    private void ResetTimer()
    {
        _timerService.Stop();
        IsTimerRunning = false;
        RemainingSeconds = _durationSeconds;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (RemainingSeconds <= 1)
        {
            RemainingSeconds = 0;
            _timerService.Stop();
            IsTimerRunning = false;
            CompletedFocusSessions++;
            return;
        }

        RemainingSeconds--;
    }

    private void AddDoneItem()
    {
        var item = AddDoneText.Trim();
        if (string.IsNullOrWhiteSpace(item))
        {
            return;
        }

        DoneItems.Add(item);
        AddDoneText = string.Empty;
    }

    private void MarkCurrentFocusDone()
    {
        var item = CurrentFocus.Trim();
        if (string.IsNullOrWhiteSpace(item))
        {
            return;
        }

        DoneItems.Add(item);
    }

    private void RemoveDoneItem(string? item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            return;
        }

        DoneItems.Remove(item);
    }

    private void ClearTodayProgress()
    {
        CompletedFocusSessions = 0;
        DoneItems.Clear();
    }

    private void OnDoneItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ProgressSummary));
        RaiseCommandStates();
        QueueSave();
    }

    private void QueueSave()
    {
        if (!IsLoaded)
        {
            return;
        }

        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    private void RaiseCommandStates()
    {
        Raise(StartTimerCommand);
        Raise(PauseTimerCommand);
        Raise(AddDoneItemCommand);
        Raise(RemoveDoneItemCommand);
        Raise(MarkCurrentFocusDoneCommand);
        Raise(ClearTodayProgressCommand);
    }

    private static void Raise(ICommand command)
    {
        switch (command)
        {
            case RelayCommand relayCommand:
                relayCommand.RaiseCanExecuteChanged();
                break;
            case AsyncRelayCommand asyncRelayCommand:
                asyncRelayCommand.RaiseCanExecuteChanged();
                break;
            case RelayCommand<string> relayCommandOfString:
                relayCommandOfString.RaiseCanExecuteChanged();
                break;
        }
    }
}
