using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.ClipboardHistory.Factories;
using WindowSill.ClipboardHistory.FirstTimeSetup;
using WindowSill.ClipboardHistory.Services;
using WindowSill.ClipboardHistory.Settings;
using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory;

[Export(typeof(ISill))]
[Name("Clipboard History")]
[Priority(Priority.Low)]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
public sealed class ClipboardHistorySill : ISillActivatedByDefault, ISillFirstTimeSetup, ISillListView
{
    private readonly ILogger _logger;
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ClipboardHistoryDataService _clipboardDataService;
    private readonly ClipboardItemViewFactory _viewFactory;

    [ImportingConstructor]
    internal ClipboardHistorySill(
        IProcessInteractionService processInteractionService,
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo,
        ClipboardHistoryDataService clipboardDataService)
    {
        _logger = this.Log();
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _clipboardDataService = clipboardDataService;
        _viewFactory = new ClipboardItemViewFactory(settingsProvider, processInteractionService);
    }

    public string DisplayName => "/WindowSill.ClipboardHistory/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "clipboard.svg")))
        };

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView { get; } = ClipboardItemViewFactory.CreatePlaceholderView();

    public IFirstTimeSetupContributor[] GetFirstTimeSetupContributors()
    {
        if (_clipboardDataService.IsHistoryEnabled)
        {
            return [];
        }

        return [new ClipboardHistoryFirstTimeSetupContributor()];
    }

    public async ValueTask OnActivatedAsync()
    {
        _settingsProvider.SettingChanged += SettingsProvider_SettingChanged;
        _clipboardDataService.DataUpdated += ClipboardDataService_DataUpdated;
        _clipboardDataService.Subscribe();

        await RefreshClipboardDataAsync();
    }

    public ValueTask OnDeactivatedAsync()
    {
        _settingsProvider.SettingChanged -= SettingsProvider_SettingChanged;
        _clipboardDataService.DataUpdated -= ClipboardDataService_DataUpdated;
        _clipboardDataService.Unsubscribe();
        return ValueTask.CompletedTask;
    }

    private void SettingsProvider_SettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.MaximumHistoryCount.Name)
        {
            RefreshClipboardDataAsync().Forget();
        }
        else if (args.SettingName == Settings.Settings.HidePasswords.Name)
        {
            ViewList.Clear();
            _clipboardDataService.ClearCache();
            RefreshClipboardDataAsync().Forget();
        }
    }

    private void ClipboardDataService_DataUpdated(object? sender, EventArgs e)
    {
        RefreshClipboardDataAsync().Forget();
    }

    private async Task RefreshClipboardDataAsync()
    {
        int maxItems = _settingsProvider.GetSetting(Settings.Settings.MaximumHistoryCount);
        await _clipboardDataService.RefreshAsync(maxItems);
        await UpdateViewListAsync();
    }

    private async Task UpdateViewListAsync()
    {
        IReadOnlyList<ClipboardItemData> cachedItems = _clipboardDataService.GetCachedItems();

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.SynchronizeWith(
                cachedItems,
                (oldItem, newItem) =>
                {
                    if (oldItem.DataContext is ClipboardHistoryItemViewModelBase oldItemViewModel)
                    {
                        return oldItemViewModel.Equals(newItem.Item);
                    }
                    throw new Exception($"Unexpected item type in ViewList: {oldItem.DataContext?.GetType().FullName ?? "null"}");
                },
                (itemData) =>
                {
                    ClipboardHistoryItemViewModelBase viewModel;
                    SillListViewItem view;

                    try
                    {
                        (viewModel, view) = _viewFactory.Create(itemData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create a view and viewmodel for a clipboard item.");
                        (viewModel, view) = _viewFactory.Create(new ClipboardItemData(itemData.Item, DetectedClipboardDataType.Unknown));
                    }

                    CreateContextMenu(viewModel, view);

                    return view;
                });
        });
    }

    private static void CreateContextMenu(ClipboardHistoryItemViewModelBase viewModel, SillListViewItem view)
    {
        var menuFlyout = new MenuFlyout();
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = "/WindowSill.ClipboardHistory/Misc/ClearHistory".GetLocalizedString(),
            Icon = new SymbolIcon(Symbol.Clear),
            Command = viewModel.ClearCommand
        });
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = "/WindowSill.ClipboardHistory/Misc/Delete".GetLocalizedString(),
            Icon = new SymbolIcon(Symbol.Delete),
            Command = viewModel.DeleteCommand
        });

        view.ContextFlyout = menuFlyout;
    }
}
