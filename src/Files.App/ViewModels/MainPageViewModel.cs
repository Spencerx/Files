// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.Services.Store;
using Windows.System;
using WinRT.Interop;

namespace Files.App.ViewModels
{
	/// <summary>
	/// Represents ViewModel of <see cref="MainPage"/>.
	/// </summary>
	public sealed partial class MainPageViewModel : ObservableObject
	{
		// Dependency injections

		private IAppearanceSettingsService AppearanceSettingsService { get; } = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private INetworkService NetworkService { get; } = Ioc.Default.GetRequiredService<INetworkService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IResourcesService ResourcesService { get; } = Ioc.Default.GetRequiredService<IResourcesService>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();
		public ShelfViewModel ShelfViewModel { get; } = Ioc.Default.GetRequiredService<ShelfViewModel>();

		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		public static ObservableCollection<TabBarItem> AppInstances { get; private set; } = [];

		public List<ITabBar> MultitaskingControls { get; } = [];

		public ITabBar? MultitaskingControl { get; set; }

		private bool _IsSidebarPaneOpen;
		public bool IsSidebarPaneOpen
		{
			get => _IsSidebarPaneOpen;
			set => SetProperty(ref _IsSidebarPaneOpen, value);
		}

		private bool _IsSidebarPaneOpenToggleButtonVisible;
		public bool IsSidebarPaneOpenToggleButtonVisible
		{
			get => _IsSidebarPaneOpenToggleButtonVisible;
			set => SetProperty(ref _IsSidebarPaneOpenToggleButtonVisible, value);
		}

		private TabBarItem? selectedTabItem;
		public TabBarItem? SelectedTabItem
		{
			get => selectedTabItem;
			set => SetProperty(ref selectedTabItem, value);
		}

		private bool shouldViewControlBeDisplayed;
		public bool ShouldViewControlBeDisplayed
		{
			get => shouldViewControlBeDisplayed;
			set => SetProperty(ref shouldViewControlBeDisplayed, value);
		}

		private bool shouldPreviewPaneBeActive;
		public bool ShouldPreviewPaneBeActive
		{
			get => shouldPreviewPaneBeActive;
			set => SetProperty(ref shouldPreviewPaneBeActive, value);
		}

		private bool shouldPreviewPaneBeDisplayed;
		public bool ShouldPreviewPaneBeDisplayed
		{
			get => shouldPreviewPaneBeDisplayed;
			set => SetProperty(ref shouldPreviewPaneBeDisplayed, value);
		}

		public bool ShowShelfPane
			=> GeneralSettingsService.ShowShelfPane && AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;

		public Stretch AppThemeBackgroundImageFit
			=> AppearanceSettingsService.AppThemeBackgroundImageFit;

		public float AppThemeBackgroundImageOpacity
			=> AppearanceSettingsService.AppThemeBackgroundImageOpacity;

		public ImageSource? AppThemeBackgroundImageSource
		{
			get
			{
				if (string.IsNullOrWhiteSpace(AppearanceSettingsService.AppThemeBackgroundImageSource))
					return null;

				if (!Uri.TryCreate(AppearanceSettingsService.AppThemeBackgroundImageSource, UriKind.RelativeOrAbsolute, out Uri? validUri))
					return null;

				try
				{
					return new BitmapImage(validUri);
				}
				catch (Exception)
				{
					// Catch potential errors
					return null;
				}
			}
		}

		public VerticalAlignment AppThemeBackgroundImageVerticalAlignment
			=> AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment;

		public HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment
			=> AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment;

		public bool ShowToolbar =>
			AppearanceSettingsService.ShowToolbar &&
			context.PageType is not ContentPageTypes.Home &&
			context.PageType is not ContentPageTypes.ReleaseNotes &&
			context.PageType is not ContentPageTypes.Settings;

		public bool ShowStatusBar =>
			context.PageType is not ContentPageTypes.Home &&
			context.PageType is not ContentPageTypes.ReleaseNotes &&
			context.PageType is not ContentPageTypes.Settings;

		public bool ShowReviewPrompt
		{
			get
			{
				var isTargetEnvironment = AppLifecycleHelper.AppEnvironment is AppEnvironment.StoreStable or AppEnvironment.StorePreview;
				var hasClickedReviewPrompt = UserSettingsService.ApplicationSettingsService.HasClickedReviewPrompt;
				var launchCountReached = AppLifecycleHelper.TotalLaunchCount == 30;

				return isTargetEnvironment && !hasClickedReviewPrompt && launchCountReached;
			}
		}

		public bool ShowSponsorPrompt
		{
			get
			{
				var isTargetEnvironment = AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev or AppEnvironment.SideloadStable or AppEnvironment.SideloadPreview;
				var hasClickedSponsorPrompt = UserSettingsService.ApplicationSettingsService.HasClickedSponsorPrompt;
				var launchCountReached = AppLifecycleHelper.TotalLaunchCount == 30;

				return isTargetEnvironment && !hasClickedSponsorPrompt && launchCountReached;
			}
		}

		// Commands

		public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; }
		public ICommand ReviewAppCommand { get; }
		public ICommand DismissReviewPromptCommand { get; }
		public ICommand SponsorCommand { get; }
		public ICommand DismissSponsorPromptCommand { get; }

		// Constructor

		public MainPageViewModel()
		{
			NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand);
			ReviewAppCommand = new RelayCommand(ExecuteReviewAppCommand);
			DismissReviewPromptCommand = new RelayCommand(ExecuteDismissReviewPromptCommand);
			SponsorCommand = new RelayCommand(ExecuteSponsorCommand);
			DismissSponsorPromptCommand = new RelayCommand(ExecuteDismissSponsorPromptCommand);

			AppearanceSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageSource):
						OnPropertyChanged(nameof(AppThemeBackgroundImageSource));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageOpacity):
						OnPropertyChanged(nameof(AppThemeBackgroundImageOpacity));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageFit):
						OnPropertyChanged(nameof(AppThemeBackgroundImageFit));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment):
						OnPropertyChanged(nameof(AppThemeBackgroundImageVerticalAlignment));
						break;
					case nameof(AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment):
						OnPropertyChanged(nameof(AppThemeBackgroundImageHorizontalAlignment));
						break;
					case nameof(AppearanceSettingsService.ShowToolbar):
						OnPropertyChanged(nameof(ShowToolbar));
						break;
				}
			};

			context.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(context.PageType):
						OnPropertyChanged(nameof(ShowToolbar));
						OnPropertyChanged(nameof(ShowStatusBar));
						break;
				}
			};

			GeneralSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(GeneralSettingsService.ShowShelfPane):
						OnPropertyChanged(nameof(ShowShelfPane));
						break;
				}
			};
		}

		// Methods

		public async Task OnNavigatedToAsync(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
				return;

			var parameter = e.Parameter;
			var ignoreStartupSettings = false;
			if (parameter is MainPageNavigationArguments mainPageNavigationArguments)
			{
				parameter = mainPageNavigationArguments.Parameter;
				ignoreStartupSettings = mainPageNavigationArguments.IgnoreStartupSettings;
			}

			if (parameter is null || (parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
			{
				try
				{
					// add last session tabs to closed tabs stack if those tabs are not about to be opened
					if (!UserSettingsService.AppSettingsService.RestoreTabsOnStartup && !UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.GeneralSettingsService.LastSessionTabList != null)
					{
						var items = UserSettingsService.GeneralSettingsService.LastSessionTabList
							.Where(tab => !string.IsNullOrEmpty(tab))
							.Select(tab => TabBarItemParameter.Deserialize(tab)).ToArray();

						BaseTabBar.PushRecentTab(items);
					}

					if (UserSettingsService.AppSettingsService.RestoreTabsOnStartup)
					{
						UserSettingsService.AppSettingsService.RestoreTabsOnStartup = false;
						if (UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = TabBarItemParameter.Deserialize(tabArgsString);
								await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}

							if (!UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
								UserSettingsService.GeneralSettingsService.LastSessionTabList = null;
						}
					}
					else if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
						UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
					{
						foreach (string path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
							await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), path, true);
					}
					else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
						UserSettingsService.GeneralSettingsService.LastSessionTabList is not null)
					{
						if (AppInstances.Count == 0)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = TabBarItemParameter.Deserialize(tabArgsString);
								await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}
						}
					}
					else
					{
						await NavigationHelpers.AddNewTabAsync();
					}
				}
				catch
				{
					await NavigationHelpers.AddNewTabAsync();
				}
			}
			else
			{
				if (!ignoreStartupSettings)
				{
					try
					{
						if (UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup &&
								UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
						{
							foreach (string path in UserSettingsService.GeneralSettingsService.TabsOnStartupList)
								await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), path, true);
						}
						else if (UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp &&
							UserSettingsService.GeneralSettingsService.LastSessionTabList is not null &&
							AppInstances.Count == 0)
						{
							foreach (string tabArgsString in UserSettingsService.GeneralSettingsService.LastSessionTabList)
							{
								var tabArgs = TabBarItemParameter.Deserialize(tabArgsString);
								await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
							}
						}
					}
					catch { }
				}

				if (parameter is string navArgs)
					await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), navArgs, true);
				else if (parameter is PaneNavigationArguments paneArgs)
					await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), paneArgs);
				else if (parameter is TabBarItemParameter tabArgs)
					await NavigationHelpers.AddNewTabByParamAsync(tabArgs.InitialPageType, tabArgs.NavigationParameter);
			}

			// Load the app theme resources
			ResourcesService.LoadAppResources(AppearanceSettingsService);

			await Task.WhenAll(
				DrivesViewModel.UpdateDrivesAsync(),
				NetworkService.UpdateComputersAsync(),
				NetworkService.UpdateShortcutsAsync());
		}

		// Command methods

		private async void ExecuteReviewAppCommand()
		{
			UserSettingsService.ApplicationSettingsService.HasClickedReviewPrompt = true;
			OnPropertyChanged(nameof(ShowReviewPrompt));

			try
			{
				var storeContext = StoreContext.GetDefault();
				InitializeWithWindow.Initialize(storeContext, MainWindow.Instance.WindowHandle);
				await storeContext.RequestRateAndReviewAppAsync();
			}
			catch (Exception) { }
		}

		private void ExecuteDismissReviewPromptCommand()
		{
			UserSettingsService.ApplicationSettingsService.HasClickedReviewPrompt = true;
		}

		private async void ExecuteSponsorCommand()
		{
			UserSettingsService.ApplicationSettingsService.HasClickedSponsorPrompt = true;
			OnPropertyChanged(nameof(ShowSponsorPrompt));
			await Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.SupportUsUrl)).AsTask();
		}

		private void ExecuteDismissSponsorPromptCommand()
		{
			UserSettingsService.ApplicationSettingsService.HasClickedSponsorPrompt = true;
		}

		private async void ExecuteNavigateToNumberedTabKeyboardAcceleratorCommand(KeyboardAcceleratorInvokedEventArgs? e)
		{
			var indexToSelect = e!.KeyboardAccelerator.Key switch
			{
				VirtualKey.Number1 => 0,
				VirtualKey.Number2 => 1,
				VirtualKey.Number3 => 2,
				VirtualKey.Number4 => 3,
				VirtualKey.Number5 => 4,
				VirtualKey.Number6 => 5,
				VirtualKey.Number7 => 6,
				VirtualKey.Number8 => 7,
				VirtualKey.Number9 => AppInstances.Count - 1,
				_ => AppInstances.Count - 1,
			};

			// Only select the tab if it is in the list
			if (indexToSelect < AppInstances.Count)
			{
				App.AppModel.TabStripSelectedIndex = indexToSelect;

				// Small delay for the UI to load
				await Task.Delay(500);

				// Focus the content of the selected tab item (needed for keyboard navigation)
				(SelectedTabItem?.TabItemContent as Control)?.Focus(FocusState.Programmatic);
			}

			e.Handled = true;
		}

	}
}
