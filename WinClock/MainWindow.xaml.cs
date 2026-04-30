using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Animation;

namespace WinClock
{
    public sealed partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private DispatcherTimer _timer;
        private string _language = "en";
        private bool _use24Hour = true;
        private string _dateFormat = "dd/MM/yyyy";
        private string _timeFormat = "HH:mm:ss";
        private double _timeFontSize = 60;
        private double _dateFontSize = 18;
        private string _timeFontFamily = "Segoe UI Variable";
        private string _dateFontFamily = "Segoe UI Variable";
        private Windows.UI.Color _timeColor = Microsoft.UI.Colors.Red;
        private Windows.UI.Color _dateColor = Microsoft.UI.Colors.White;
        private int _windowWidth = 550;
        private int _windowHeight = 300;
        private string _backgroundImagePath = "";
        private int _alarmAutoDismissSeconds = 60;
        private DateTime? _alertStartTime = null;
        private bool _showHebrewDate = false;
        private TimeSpan _hebrewDateTransitionTime = new TimeSpan(18, 30, 0);
        private double _hebrewDateFontSize = 16;
        private string _hebrewDateFontFamily = "Segoe UI Variable";
        private Windows.UI.Color _hebrewDateColor = Microsoft.UI.Colors.LightGray;
        private double _alertLabelFontSize = 48;
        private string _alertLabelFontFamily = "Segoe UI Variable";
        private Windows.UI.Color _alertLabelColor = Microsoft.UI.Colors.Red;
        private bool _alwaysOnTop = true;
        private System.Collections.Generic.List<Alarm> _alarms = new System.Collections.Generic.List<Alarm>();

        // Public properties for SettingsWindow to access
        public System.Collections.Generic.List<Alarm> Alarms { get => _alarms; set { _alarms = value; SaveAlarms(); } }
        public int AlarmAutoDismissSeconds { get => _alarmAutoDismissSeconds; set { _alarmAutoDismissSeconds = value; SaveSettings(); } }
        public bool ShowHebrewDate { get => _showHebrewDate; set { _showHebrewDate = value; UpdateDisplay(); SaveSettings(); } }
        public TimeSpan HebrewDateTransitionTime { get => _hebrewDateTransitionTime; set { _hebrewDateTransitionTime = value; UpdateDisplay(); SaveSettings(); } }
        public string Language { get => _language; set { _language = value; LocalizationManager.LoadLanguage(value); UpdateDisplay(); SaveSettings(); } }
        public bool Use24Hour { get => _use24Hour; set { _use24Hour = value; UpdateDisplay(); SaveSettings(); } }
        public string DateFormat { get => _dateFormat; set { _dateFormat = value; UpdateDisplay(); SaveSettings(); } }
        public string TimeFormat { get => _timeFormat; set { _timeFormat = value; UpdateDisplay(); SaveSettings(); } }
        public double TimeFontSize { get => _timeFontSize; set { _timeFontSize = value; if (TimeTextBlock != null) TimeTextBlock.FontSize = value; SaveSettings(); } }
        public double DateFontSize { get => _dateFontSize; set { _dateFontSize = value; if (DateTextBlock != null) DateTextBlock.FontSize = value; SaveSettings(); } }
        public string TimeFontFamily { get => _timeFontFamily; set { _timeFontFamily = value; if (TimeTextBlock != null) TimeTextBlock.FontFamily = new FontFamily(value); SaveSettings(); } }
        public string DateFontFamily { get => _dateFontFamily; set { _dateFontFamily = value; if (DateTextBlock != null) DateTextBlock.FontFamily = new FontFamily(value); SaveSettings(); } }
        public Windows.UI.Color TimeColor { get => _timeColor; set { _timeColor = value; if (TimeTextBlock != null) TimeTextBlock.Foreground = new SolidColorBrush(value); SaveSettings(); } }
        public Windows.UI.Color DateColor { get => _dateColor; set { _dateColor = value; if (DateTextBlock != null) DateTextBlock.Foreground = new SolidColorBrush(value); SaveSettings(); } }
        public double HebrewDateFontSize { get => _hebrewDateFontSize; set { _hebrewDateFontSize = value; if (HebrewDateTextBlock != null) HebrewDateTextBlock.FontSize = value; SaveSettings(); } }
        public string HebrewDateFontFamily { get => _hebrewDateFontFamily; set { _hebrewDateFontFamily = value; if (HebrewDateTextBlock != null) HebrewDateTextBlock.FontFamily = new FontFamily(value); SaveSettings(); } }
        public Windows.UI.Color HebrewDateColor { get => _hebrewDateColor; set { _hebrewDateColor = value; if (HebrewDateTextBlock != null) HebrewDateTextBlock.Foreground = new SolidColorBrush(value); SaveSettings(); } }
        public double AlertLabelFontSize { get => _alertLabelFontSize; set { _alertLabelFontSize = value; if (AlarmLabelTextBlock != null) AlarmLabelTextBlock.FontSize = value; SaveSettings(); } }
        public string AlertLabelFontFamily { get => _alertLabelFontFamily; set { _alertLabelFontFamily = value; if (AlarmLabelTextBlock != null) AlarmLabelTextBlock.FontFamily = new FontFamily(value); SaveSettings(); } }
        public Windows.UI.Color AlertLabelColor { get => _alertLabelColor; set { _alertLabelColor = value; if (AlarmLabelTextBlock != null) AlarmLabelTextBlock.Foreground = new SolidColorBrush(value); SaveSettings(); } }
        public int WindowWidth { get => _windowWidth; set { _windowWidth = value; ResizeWindow(); SaveSettings(); } }
        public int WindowHeight { get => _windowHeight; set { _windowHeight = value; ResizeWindow(); SaveSettings(); } }
        public bool AlwaysOnTop { get => _alwaysOnTop; set { _alwaysOnTop = value; UpdateAlwaysOnTop(); SaveSettings(); } }

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "WinClock Widget";
            
            LoadSettings();
            LocalizationManager.LoadLanguage(_language);
            SetupWidgetBehavior();
            ResizeWindow();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateDisplay();
            ApplyAllSettings();
        }

        private void UpdateAlwaysOnTop()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = _alwaysOnTop;
                }
            }
        }

        private void SetupWidgetBehavior()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.IsShownInSwitchers = false;
                var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = _alwaysOnTop;
                    presenter.IsResizable = false;
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                    presenter.SetBorderAndTitleBar(false, false);
                }
            }
        }

        private void SaveSettings()
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values["Language"] = _language;
            settings.Values["Use24Hour"] = _use24Hour;
            settings.Values["DateFormat"] = _dateFormat;
            settings.Values["TimeFormat"] = _timeFormat;
            settings.Values["TimeFontSize"] = _timeFontSize;
            settings.Values["DateFontSize"] = _dateFontSize;
            settings.Values["TimeFontFamily"] = _timeFontFamily;
            settings.Values["DateFontFamily"] = _dateFontFamily;
            settings.Values["TimeColor"] = ColorToHex(_timeColor);
            settings.Values["DateColor"] = ColorToHex(_dateColor);
            settings.Values["WindowWidth"] = _windowWidth;
            settings.Values["WindowHeight"] = _windowHeight;
            settings.Values["BackgroundImagePath"] = _backgroundImagePath;
            settings.Values["AlarmAutoDismissSeconds"] = _alarmAutoDismissSeconds;
            settings.Values["ShowHebrewDate"] = _showHebrewDate;
            settings.Values["HebrewDateTransitionTicks"] = _hebrewDateTransitionTime.Ticks;
            settings.Values["HebrewDateFontSize"] = _hebrewDateFontSize;
            settings.Values["HebrewDateFontFamily"] = _hebrewDateFontFamily;
            settings.Values["HebrewDateColor"] = ColorToHex(_hebrewDateColor);
            settings.Values["AlertLabelFontSize"] = _alertLabelFontSize;
            settings.Values["AlertLabelFontFamily"] = _alertLabelFontFamily;
            settings.Values["AlertLabelColor"] = ColorToHex(_alertLabelColor);
            settings.Values["AlwaysOnTop"] = _alwaysOnTop;
            SaveAlarms();
        }

        private void SaveAlarms()
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var json = System.Text.Json.JsonSerializer.Serialize(_alarms);
            settings.Values["AlarmsJson"] = json;
        }

        private void LoadSettings()
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey("Language"))
            {
                var sysLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                _language = sysLang == "he" ? "he" : "en";
            }
            else _language = (string)settings.Values["Language"];

            if (settings.Values.ContainsKey("Use24Hour")) _use24Hour = (bool)settings.Values["Use24Hour"];
            if (settings.Values.ContainsKey("DateFormat")) _dateFormat = (string)settings.Values["DateFormat"];
            if (settings.Values.ContainsKey("TimeFormat")) _timeFormat = (string)settings.Values["TimeFormat"];
            if (settings.Values.ContainsKey("TimeFontSize")) _timeFontSize = (double)settings.Values["TimeFontSize"];
            if (settings.Values.ContainsKey("DateFontSize")) _dateFontSize = (double)settings.Values["DateFontSize"];
            if (settings.Values.ContainsKey("TimeFontFamily")) _timeFontFamily = (string)settings.Values["TimeFontFamily"];
            if (settings.Values.ContainsKey("DateFontFamily")) _dateFontFamily = (string)settings.Values["DateFontFamily"];
            if (settings.Values.ContainsKey("TimeColor")) _timeColor = HexToColor((string)settings.Values["TimeColor"]);
            if (settings.Values.ContainsKey("DateColor")) _dateColor = HexToColor((string)settings.Values["DateColor"]);
            if (settings.Values.ContainsKey("WindowWidth")) _windowWidth = (int)settings.Values["WindowWidth"];
            if (settings.Values.ContainsKey("WindowHeight")) _windowHeight = (int)settings.Values["WindowHeight"];
            if (settings.Values.ContainsKey("BackgroundImagePath")) _backgroundImagePath = (string)settings.Values["BackgroundImagePath"];
            if (settings.Values.ContainsKey("AlarmAutoDismissSeconds")) _alarmAutoDismissSeconds = (int)settings.Values["AlarmAutoDismissSeconds"];
            if (settings.Values.ContainsKey("ShowHebrewDate")) _showHebrewDate = (bool)settings.Values["ShowHebrewDate"];
            if (settings.Values.ContainsKey("HebrewDateTransitionTicks")) _hebrewDateTransitionTime = TimeSpan.FromTicks((long)settings.Values["HebrewDateTransitionTicks"]);
            if (settings.Values.ContainsKey("HebrewDateFontSize")) _hebrewDateFontSize = (double)settings.Values["HebrewDateFontSize"];
            if (settings.Values.ContainsKey("HebrewDateFontFamily")) _hebrewDateFontFamily = (string)settings.Values["HebrewDateFontFamily"];
            if (settings.Values.ContainsKey("HebrewDateColor")) _hebrewDateColor = HexToColor((string)settings.Values["HebrewDateColor"]);
            if (settings.Values.ContainsKey("AlertLabelFontSize")) _alertLabelFontSize = (double)settings.Values["AlertLabelFontSize"];
            if (settings.Values.ContainsKey("AlertLabelFontFamily")) _alertLabelFontFamily = (string)settings.Values["AlertLabelFontFamily"];
            if (settings.Values.ContainsKey("AlertLabelColor")) _alertLabelColor = HexToColor((string)settings.Values["AlertLabelColor"]);
            if (settings.Values.ContainsKey("AlwaysOnTop")) _alwaysOnTop = (bool)settings.Values["AlwaysOnTop"];
            
            if (settings.Values.ContainsKey("AlarmsJson"))
            {
                try {
                    _alarms = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Alarm>>((string)settings.Values["AlarmsJson"]) ?? new System.Collections.Generic.List<Alarm>();
                } catch { _alarms = new System.Collections.Generic.List<Alarm>(); }
            }
        }

        private void TriggerAlert(Alarm alarm)
        {
            _alertStartTime = DateTime.Now;
            AlertOverlay.Opacity = 1;
            AlertStoryboard.Begin();
            AlertIcon.Visibility = Visibility.Visible;
            DismissAlertButton.Visibility = Visibility.Visible;
            if (!string.IsNullOrEmpty(alarm.Label))
            {
                AlarmLabelTextBlock.Text = alarm.Label;
            }
            else AlarmLabelTextBlock.Text = "ALARM!";
            
            AlarmLabelTextBlock.FontSize = _alertLabelFontSize;
            AlarmLabelTextBlock.FontFamily = new FontFamily(_alertLabelFontFamily);
            AlarmLabelTextBlock.Foreground = new SolidColorBrush(_alertLabelColor);
            AlertIcon.Foreground = AlarmLabelTextBlock.Foreground;

            DimmingOverlay.Opacity = 0.8;
            AlertContent.Visibility = Visibility.Visible;
            ClockContent.Opacity = 0.2;
            
            AlertPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri("ms-winsoundevent:Notification.Looping.Alarm"));
            AlertPlayer.MediaPlayer.IsLoopingEnabled = true;
            AlertPlayer.MediaPlayer.Play();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();

        private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Only drag if we're not clicking on an interactive element
            if (e.OriginalSource is FrameworkElement fe)
            {
                // Traverse up to see if we're inside a button
                DependencyObject current = fe;
                while (current != null && current != RootGrid)
                {
                    if (current is Button || current is HyperlinkButton || current is MenuFlyoutItem)
                        return;
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SendMessage(hWnd, 0x00A1, (IntPtr)2, IntPtr.Zero);
        }

        private void RootGrid_Tapped(object sender, TappedRoutedEventArgs e) { if (_alertStartTime != null) DismissAlert(); }

        private void DismissAlertButton_Click(object sender, RoutedEventArgs e) => DismissAlert();

        private void DismissAlert()
        {
            _alertStartTime = null;
            AlertStoryboard.Stop();
            AlertOverlay.Opacity = 0;
            DimmingOverlay.Opacity = 0;
            AlertContent.Visibility = Visibility.Collapsed;
            ClockContent.Opacity = 1;
            AlertPlayer.MediaPlayer.Pause();
        }

        private void ApplyAllSettings()
        {
            if (TimeTextBlock != null)
            {
                TimeTextBlock.FontSize = _timeFontSize;
                TimeTextBlock.FontFamily = new FontFamily(_timeFontFamily);
                TimeTextBlock.Foreground = new SolidColorBrush(_timeColor);
            }
            if (DateTextBlock != null)
            {
                DateTextBlock.FontSize = _dateFontSize;
                DateTextBlock.FontFamily = new FontFamily(_dateFontFamily);
                DateTextBlock.Foreground = new SolidColorBrush(_dateColor);
            }
            if (HebrewDateTextBlock != null)
            {
                HebrewDateTextBlock.FontSize = _hebrewDateFontSize;
                HebrewDateTextBlock.FontFamily = new FontFamily(_hebrewDateFontFamily);
                HebrewDateTextBlock.Foreground = new SolidColorBrush(_hebrewDateColor);
            }
            if (AlarmLabelTextBlock != null)
            {
                AlarmLabelTextBlock.FontSize = _alertLabelFontSize;
                AlarmLabelTextBlock.FontFamily = new FontFamily(_alertLabelFontFamily);
                AlarmLabelTextBlock.Foreground = new SolidColorBrush(_alertLabelColor);
                AlertIcon.Foreground = AlarmLabelTextBlock.Foreground;
            }
            SetBackgroundImage(_backgroundImagePath);
        }

        private string ColorToHex(Windows.UI.Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        private Windows.UI.Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");
            byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return Windows.UI.Color.FromArgb(a, r, g, b);
        }

        private void ResizeWindow()
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(_windowWidth, _windowHeight));
        }

        public void SetBackgroundImage(string path)
        {
            _backgroundImagePath = path ?? "";
            if (string.IsNullOrEmpty(_backgroundImagePath)) BackgroundImage.Source = null;
            else
            {
                try { BackgroundImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(_backgroundImagePath)); }
                catch { }
            }
            SaveSettings();
        }

        private void Timer_Tick(object sender, object e)
        {
            DateTime now = DateTime.Now;
            UpdateDisplay();
            foreach (var alarm in _alarms) { if (alarm.ShouldTrigger(now)) TriggerAlert(alarm); }
            if (_alertStartTime.HasValue && _alarmAutoDismissSeconds > 0)
            {
                if ((now - _alertStartTime.Value).TotalSeconds >= _alarmAutoDismissSeconds) DismissAlert();
            }
        }

        private void UpdateDisplay()
        {
            DateTime now = DateTime.Now;
            
            // Map simple codes to full culture strings
            string cultureCode = _language switch
            {
                "he" => "he-IL",
                "en" => "en-US",
                "zh" => "zh-CN",
                "ar" => "ar-SA",
                "hi" => "hi-IN",
                _ => _language
            };

            var culture = new System.Globalization.CultureInfo(cultureCode);
            
            // Handle Right-to-Left languages
            bool isRTL = _language == "he" || _language == "ar";
            RootGrid.FlowDirection = isRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            if (_timeFormat == "None") TimeTextBlock.Visibility = Visibility.Collapsed;
            else
            {
                TimeTextBlock.Visibility = Visibility.Visible;
                string format = _timeFormat;
                if (!_use24Hour) format = format.Replace("HH", "hh") + " tt";
                TimeTextBlock.Text = now.ToString(format, culture);
            }

            if (_dateFormat == "None") DateTextBlock.Visibility = Visibility.Collapsed;
            else
            {
                DateTextBlock.Visibility = Visibility.Visible;
                DateTextBlock.Text = now.ToString(_dateFormat, culture);
            }

            if (_showHebrewDate)
            {
                HebrewDateTextBlock.Visibility = Visibility.Visible;
                DateTime hebrewDateContext = now.TimeOfDay >= _hebrewDateTransitionTime ? now.AddDays(1) : now;
                HebrewDateTextBlock.Text = GetHebrewDateString(hebrewDateContext);
                HebrewDateTextBlock.FontSize = _hebrewDateFontSize;
                HebrewDateTextBlock.FontFamily = new FontFamily(_hebrewDateFontFamily);
                HebrewDateTextBlock.Foreground = new SolidColorBrush(_hebrewDateColor);
            }
            else HebrewDateTextBlock.Visibility = Visibility.Collapsed;

            MenuSettings.Text = LocalizationManager.GetString("MenuSettings");
            MenuExit.Text = LocalizationManager.GetString("MenuExit");
            DismissAlertButton.Content = LocalizationManager.GetString("DismissButton");
        }

        private string GetHebrewDateString(DateTime date)
        {
            try
            {
                var formatter = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("day month year", new[] { "he-IL-u-ca-hebrew" });
                return formatter.Format(new DateTimeOffset(date));
            }
            catch { return ""; }
        }

        private async void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var textBlock = new TextBlock 
            { 
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            };
            
            textBlock.Inlines.Add(new Run { Text = "All rights reserved to " });
            
            var hyperlink = new Hyperlink
            {
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
            };
            hyperlink.Inlines.Add(new Run { Text = "cpo7-corp" } );
            hyperlink.Click += async (s, e2) =>
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/cpo7-corp/WinClock"));
            };
            
            textBlock.Inlines.Add(hyperlink);

            var dialog = new ContentDialog
            {
                Title = "About WinClock",
                Content = textBlock,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(this);
            var mainAppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)));
            var settingsAppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(WinRT.Interop.WindowNative.GetWindowHandle(settingsWindow)));
            var newPos = mainAppWindow.Position;
            newPos.X += mainAppWindow.Size.Width + 10;
            settingsAppWindow.Move(newPos);
            settingsWindow.Activate();
        }
    }
}
