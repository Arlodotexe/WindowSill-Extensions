using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for the placeholder view shown when clipboard history is empty or disabled.
/// </summary>
internal sealed partial class EmptyOrDisabledItemViewModel : ObservableObject
{
    internal EmptyOrDisabledItemViewModel()
    {
        IsClipboardHistoryEnabled = Clipboard.IsHistoryEnabled();
        Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;
    }

    /// <summary>
    /// Gets or sets whether Windows clipboard history is currently enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsClipboardHistoryEnabled { get; set; }

    [RelayCommand]
    private async Task OpenWindowsClipboardHistoryAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:clipboard"));
    }

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        IsClipboardHistoryEnabled = Clipboard.IsHistoryEnabled();
    }
}
