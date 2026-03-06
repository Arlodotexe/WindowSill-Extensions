using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WindowSill.API;
using WindowSill.ShortTermReminder.Core;

namespace WindowSill.ShortTermReminder.ViewModels;

/// <summary>
/// ViewModel for the reminder detail popup, displaying remaining time
/// and providing a delete action.
/// </summary>
internal sealed partial class ReminderPopupViewModel : ObservableObject
{
    private readonly IReminderService _reminderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReminderPopupViewModel"/> class.
    /// </summary>
    /// <param name="reminder">The reminder to display.</param>
    /// <param name="reminderService">The reminder service for deleting reminders.</param>
    public ReminderPopupViewModel(Reminder reminder, IReminderService reminderService)
    {
        Reminder = reminder;
        _reminderService = reminderService;
    }

    /// <summary>
    /// Gets the reminder being displayed.
    /// </summary>
    internal Reminder Reminder { get; }

    /// <summary>
    /// Gets the formatted text showing the remaining time until the reminder fires.
    /// </summary>
    [ObservableProperty]
    internal partial string RemainingTimeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action to close the popup.
    /// </summary>
    internal Action? CloseAction { get; set; }

    /// <summary>
    /// Updates the remaining time display when the popup opens.
    /// </summary>
    internal void OnOpening()
    {
        TimeSpan remainingTime = Reminder.ReminderTime - DateTime.Now;
        RemainingTimeText = string.Format(
            "/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/ReminderRemainingTime".GetLocalizedString(),
            (remainingTime.TotalMinutes + 1).ToString("0"),
            Reminder.ReminderTime.ToString("h:mm tt"));
    }

    /// <summary>
    /// Deletes this reminder and closes the popup.
    /// </summary>
    [RelayCommand]
    private void Delete()
    {
        _reminderService.DeleteReminder(Reminder.Id);
        CloseAction?.Invoke();
    }
}
