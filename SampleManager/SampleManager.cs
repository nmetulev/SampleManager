using GoogleAnalytics;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace SampleManager
{
    public class Sample
    {
        public Tracker GATracker { get; private set; }

        private SampleDescription _descriptionUI;
        private Grid _descriptionContainer;
        private Border _descriptionBackground;
        private DropShadowPanel _aboutSampleRoot;
        private string _localDescriptionFilename;
        private string _remoteDescriptionUri;

        private bool _descriptionVisible = false;

        private Grid _rootGrid;

        static string deviceFamily;

        private Frame _frame;

        private bool _init = false;

        public Frame Frame
        {
            get { return _frame; }
            set
            {
                if (_frame != null)
                {
                    _frame.Navigated -= Frame_Navigated;
                    _frame.Navigating -= Frame_Navigating;
                }

                _frame = value;

                if (_frame != null)
                {
                    _frame.Navigated += Frame_Navigated;
                    _frame.Navigating += Frame_Navigating;

                    var root = Frame.Content as Page;

                    if (root != null && GATracker != null)
                    {
                        var page = root.GetType().ToString();
                        GATracker.ScreenName = page;
                        GATracker?.Send(HitBuilder.CreateScreenView().Build());

                        GATracker.Send(HitBuilder.CreateCustomEvent("page_navigation", page, null, 0).Build());
                    }

                    if (_init)
                        InjectUI();
                }
            }
        }

        private static Sample _instance;

        public static Sample Instance => _instance ?? (_instance = new Sample());

        private Sample(){ }

        /// <summary>
        /// Initializes the manager
        /// </summary>
        /// <param name="descriptionFilename">filename of description in markdown format</param>
        public void Init(string localDescriptionFilename, string remoteDescriptionUri = null, string gaTrackerId = null)
        {
            if (gaTrackerId != null)
            {
                GATracker = AnalyticsManager.Current.CreateTracker(gaTrackerId);
                //AnalyticsManager.Current.IsDebug = true;
                AnalyticsManager.Current.ReportUncaughtExceptions = true;
                AnalyticsManager.Current.AutoAppLifetimeMonitoring = true;
            }

            if (Frame == null)
            {
                Frame = Window.Current.Content as Frame;
            }

            Window.Current.SizeChanged += Current_SizeChanged;
            SystemNavigationManager.GetForCurrentView().BackRequested += Sample_BackRequested;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            _localDescriptionFilename = localDescriptionFilename;
            _remoteDescriptionUri = remoteDescriptionUri;

            _init = true;

            InjectUI();
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            SetDescriptionSize();
        }

        private void SetDescriptionSize()
        {
            var height = Window.Current.Bounds.Height;
            var width = Window.Current.Bounds.Width;

            if (_descriptionVisible)
            {
                _descriptionUI.Height = height - (height < 500 ? 40 : 80);
                _descriptionUI.Width = width > 1080 ? 1000 : width - (width < 500 ? 40 : 80);
            }
        }

        private void Frame_Navigating(object sender, Windows.UI.Xaml.Navigation.NavigatingCancelEventArgs e)
        {
            CloseHelp();
            RemoveUI();
        }

        private void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            InjectUI();
            if (GATracker != null)
            {
                GATracker.ScreenName = e.SourcePageType.ToString();
                GATracker?.Send(HitBuilder.CreateScreenView().Build());

                string pageData = $"{e.SourcePageType.ToString()}|{e.NavigationMode.ToString()}|{e.Parameter?.ToString()}";
                GATracker.Send(HitBuilder.CreateCustomEvent("page_navigation", pageData, null, 0).Build());
            }
        }

        private async Task<string> GetCampaignIdAsync()
        {
            const string CampaignIdField = "customPolicyField1";

            Windows.Services.Store.StoreContext ctx = Windows.Services.Store.StoreContext.GetDefault();

            var storeProductResult = await ctx.GetStoreProductForCurrentAppAsync();
            if (storeProductResult.Product != null)
            {
                // Get first SKU that is in my collection
                var sku = storeProductResult.Product.Skus.FirstOrDefault(s => s.IsInUserCollection);

                if (sku != null)
                {
                    // Return this user's campaign id
                    return sku.CollectionData.CampaignId;
                }
            }

            // Can't get the campaign ID from collection data, try the license data
            var appLicense = await ctx.GetAppLicenseAsync();
            var json = Windows.Data.Json.JsonObject.Parse(appLicense.ExtendedJsonData);
            if (json.ContainsKey(CampaignIdField))
            {
                return json[CampaignIdField].GetString();
            }

            // No campaign ID was found
            return string.Empty;
        }


        private void InjectUI()
        {
            if (Frame == null)
            {
                return;
            }

            var root = Frame.Content as Page;

            if (root == null)
            {
                return;
            }

            // replace current page content with a grid 
            var oldRootContent = root.Content;
            _rootGrid = new Grid();
            _rootGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            _rootGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            root.Content = _rootGrid;
            _rootGrid.Children.Add(oldRootContent);

            var vsgOld = VisualStateManager.GetVisualStateGroups(oldRootContent as FrameworkElement);
            var vsgNew = VisualStateManager.GetVisualStateGroups(_rootGrid);

            for (var i = vsgOld.Count - 1; i >=0; --i)
            {
                var group = vsgOld.ElementAt(i);
                vsgOld.RemoveAt(i);
                vsgNew.Add(group);
            }

            // add the sample description in the new grid
            if (_aboutSampleRoot == null)
            {
                var text = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                if (IsXbox())
                {
                    text.Inlines.Add(new Run()
                    {
                        Text = "ABOUT THIS SAMPLE ("
                    });
                    text.Inlines.Add(new Run()
                    {
                        FontFamily = new FontFamily("Segoe Xbox Symbol"),
                        Text = "\xE414"
                    });
                    text.Inlines.Add(new Run()
                    {
                        Text = ")"
                    });
                }
                else
                {
                    text.Text = "ABOUT THIS SAMPLE";
                }

                var border = new Border();
                border.Height = 40;
                border.Background = new SolidColorBrush(Colors.Black);
                border.Tapped += Border_Tapped;
                border.Child = text;

                _aboutSampleRoot = new DropShadowPanel();
                _aboutSampleRoot.Content = border;
                _aboutSampleRoot.ShadowOpacity = 0.8;
                _aboutSampleRoot.Color = Colors.White;
                _aboutSampleRoot.BlurRadius = 20;
                Grid.SetRow(_aboutSampleRoot, 2);
            }
            _rootGrid.Children.Add(_aboutSampleRoot);

            // check if first run
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("sample-ran"))
            {
                ApplicationData.Current.LocalSettings.Values["sample-ran"] = true;
                var task = Task.Run(async () =>
                {
                    GATracker.Send(HitBuilder.CreateCustomEvent("campaign", await GetCampaignIdAsync(), null, 0).Build());
                });
                OpenHelp();
            }

        }

        private void RemoveUI()
        {
            _rootGrid.Children.Remove(_descriptionContainer);
            _rootGrid.Children.Remove(_aboutSampleRoot);
        }

        private void Border_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            OpenHelp();
        }

        private void _descriptionBackground_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseHelp();
        }

        private void Ui_CloseClicked(object sender, EventArgs e)
        {
            CloseHelp();
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.GamepadView)
            {
                OpenHelp();
            }
        }

        private void Sample_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (_descriptionVisible)
            {
                CloseHelp();
                e.Handled = true;
            }
        }

        private async void OpenHelp()
        {
            if (_descriptionVisible) return;

            if (_descriptionUI == null)
            {
                _descriptionUI = new SampleDescription(_localDescriptionFilename, _remoteDescriptionUri);
                _descriptionUI.CloseClicked += Ui_CloseClicked;
                _descriptionUI.Opacity = 0;

                _descriptionBackground = new Border();
                _descriptionBackground.Background = new SolidColorBrush(Colors.Transparent);
                _descriptionBackground.Tapped += _descriptionBackground_Tapped;

                _descriptionContainer = new Grid();

                _descriptionContainer.Children.Add(_descriptionBackground);
                _descriptionContainer.Children.Add(_descriptionUI);

                Grid.SetRowSpan(_descriptionContainer, 2);
            }

            GATracker?.Send(HitBuilder.CreateCustomEvent("sample-action", "about-opened", null, 0).Build());

            _descriptionVisible = true;
            _rootGrid.Children.Add(_descriptionContainer);
          
            SetDescriptionSize();
            float centerX = (float)_descriptionUI.Width / 2;
            float centerY = (float)_descriptionUI.Height / 2;

            _descriptionUI.Scale(1.05f, 1.05f, centerX, centerY, 0)
                        .Scale(1, 1, centerX, centerY)
                        .Fade(1).Start();

            await _descriptionBackground.Blur(4).StartAsync();

            if (IsXbox())
            {
                _descriptionUI.FocusWebView();
            }
        }

        private async void CloseHelp()
        { 
            if (!_descriptionVisible) return;

            _descriptionVisible = false;

            float centerX = (float)_descriptionUI.Width / 2;
            float centerY = (float)_descriptionUI.Height / 2;

            _descriptionBackground.Blur(0).Start();
            await _descriptionUI.Scale(1.05f, 1.05f, centerX, centerY)
                       .Fade(0).StartAsync();

            _rootGrid.Children.Remove(_descriptionContainer);
        }

        internal static bool IsXbox()
        {
            if (deviceFamily == null)
                deviceFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;

            return deviceFamily == "Windows.Xbox";
        }
    }
}
