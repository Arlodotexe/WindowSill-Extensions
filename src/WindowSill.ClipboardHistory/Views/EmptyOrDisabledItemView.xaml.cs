using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// View for the placeholder shown when clipboard history is empty or disabled.
/// </summary>
internal sealed partial class EmptyOrDisabledItemView : UserControl
{
    internal EmptyOrDisabledItemView()
    {
        ViewModel = new EmptyOrDisabledItemViewModel();
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal EmptyOrDisabledItemViewModel ViewModel { get; }
}
