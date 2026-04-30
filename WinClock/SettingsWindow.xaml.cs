using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Microsoft.UI.Xaml.Data;

namespace WinClock
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Windows.UI.Color color)
                return new SolidColorBrush(color);
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public sealed partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;
        private Alarm _editingAlarm = null;

        public SettingsWindow(MainWindow mainWindow)
        {
            this.InitializeComponent();
            _mainWindow = mainWindow;
            this.Title = "WinClock Settings";

            // Initialize window size
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(900, 1200));

            // Load current values
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (_mainWindow == null) return;

            // Set current language selection
            LanguageComboBox.SelectedIndex = _mainWindow.Language == "he" ? 1 : 0;
            
            // Set colors
            TimeColorPicker.Color = _mainWindow.TimeColor;
            DateColorPicker.Color = _mainWindow.DateColor;
            
            // Set Window Size
            WindowWidthSlider.Value = _mainWindow.WindowWidth;
            WindowHeightSlider.Value = _mainWindow.WindowHeight;
            AutoDismissTextBox.Text = _mainWindow.AlarmAutoDismissSeconds.ToString();
            HebrewDateCheckBox.IsChecked = _mainWindow.ShowHebrewDate;
            HebrewDateTransitionTimePicker.SelectedTime = _mainWindow.HebrewDateTransitionTime;
            HebrewDateSettingsPanel.Visibility = _mainWindow.ShowHebrewDate ? Visibility.Visible : Visibility.Collapsed;

            HebrewDateFontSizeSlider.Value = _mainWindow.HebrewDateFontSize;
            HebrewDateColorPicker.Color = _mainWindow.HebrewDateColor;
            AlertLabelFontSizeSlider.Value = _mainWindow.AlertLabelFontSize;
            AlertLabelColorPicker.Color = _mainWindow.AlertLabelColor;

            // Hebrew date font
            foreach (ComboBoxItem item in HebrewDateFontFamilyComboBox.Items)
            {
                if (item.Tag?.ToString() == _mainWindow.HebrewDateFontFamily)
                {
                    HebrewDateFontFamilyComboBox.SelectedItem = item;
                    break;
                }
            }
            if (HebrewDateFontFamilyComboBox.SelectedItem == null) HebrewDateFontFamilyComboBox.SelectedIndex = 0;

            // Alert label font
            foreach (ComboBoxItem item in AlertLabelFontFamilyComboBox.Items)
            {
                if (item.Tag?.ToString() == _mainWindow.AlertLabelFontFamily)
                {
                    AlertLabelFontFamilyComboBox.SelectedItem = item;
                    break;
                }
            }
            if (AlertLabelFontFamilyComboBox.SelectedItem == null) AlertLabelFontFamilyComboBox.SelectedIndex = 0;

            // Set current time font selection
            foreach (ComboBoxItem item in TimeFontFamilyComboBox.Items)
            {
                if (item.Tag?.ToString() == _mainWindow.TimeFontFamily)
                {
                    TimeFontFamilyComboBox.SelectedItem = item;
                    break;
                }
            }
            if (TimeFontFamilyComboBox.SelectedItem == null) TimeFontFamilyComboBox.SelectedIndex = 0;

            // Time format
            foreach (ComboBoxItem item in TimeFormatComboBox.Items)
            {
                if (item.Tag?.ToString() == _mainWindow.TimeFormat)
                {
                    TimeFormatComboBox.SelectedItem = item;
                    break;
                }
            }
            if (TimeFormatComboBox.SelectedItem == null) TimeFormatComboBox.SelectedIndex = 1; // Default to first option after None

            TimeFormatToggle.IsOn = _mainWindow.Use24Hour;
            TimeFontSizeSlider.Value = _mainWindow.TimeFontSize;
            
            // Date format
            foreach (ComboBoxItem item in DateFormatComboBox.Items)
            {
                if (item.Tag?.ToString() == _mainWindow.DateFormat)
                {
                    DateFormatComboBox.SelectedItem = item;
                    break;
                }
            }
            if (DateFormatComboBox.SelectedItem == null) DateFormatComboBox.SelectedIndex = 1; // Default to first option after None
            
            DateFontSizeSlider.Value = _mainWindow.DateFontSize;

            // Set current date font selection
            foreach (ComboBoxItem item in DateFontFamilyComboBox.Items)
            {
                if (item.Tag?.ToString() == _mainWindow.DateFontFamily)
                {
                    DateFontFamilyComboBox.SelectedItem = item;
                    break;
                }
            }
            if (DateFontFamilyComboBox.SelectedItem == null) DateFontFamilyComboBox.SelectedIndex = 0;
            
            ApplyLocalization();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                _mainWindow.Language = item.Tag.ToString();
                ApplyLocalization();
            }
        }

        private void RefreshAlarmsList()
        {
            if (_mainWindow == null || AlarmsListPanel == null) return;

            AlarmsListPanel.Children.Clear();
            foreach (var alarm in _mainWindow.Alarms)
            {
                var grid = new Grid { 
                    Padding = new Thickness(5), 
                    BorderBrush = Microsoft.UI.Xaml.Application.Current.Resources["SystemControlForegroundBaseLowBrush"] as Brush, 
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) // Make clickable
                };
                grid.Tapped += (s, e) => AlarmItem_Tapped(alarm);
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var infoStack = new StackPanel();
                infoStack.Tapped += (s, e) => AlarmItem_Tapped(alarm);
                var timeText = new TextBlock { Text = alarm.TargetTime.ToString(@"hh\:mm"), FontWeight = Microsoft.UI.Text.FontWeights.Bold };
                infoStack.Children.Add(timeText);

                if (!string.IsNullOrEmpty(alarm.Label))
                {
                    infoStack.Children.Add(new TextBlock { Text = alarm.Label, FontStyle = Windows.UI.Text.FontStyle.Italic, FontSize = 14 });
                }

                if (!alarm.IsDaily && alarm.TargetDate.HasValue)
                {
                    infoStack.Children.Add(new TextBlock { Text = alarm.TargetDate.Value.ToString("dd/MM/yyyy"), FontSize = 12, Opacity = 0.6 });
                }

                var deleteBtn = new Button { 
                    Content = new SymbolIcon(Symbol.Delete), 
                    Tag = alarm.Id,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                deleteBtn.Click += DeleteAlarmButton_Click;

                Grid.SetColumn(infoStack, 0);
                Grid.SetColumn(deleteBtn, 1);
                grid.Children.Add(infoStack);
                grid.Children.Add(deleteBtn);

                AlarmsListPanel.Children.Add(grid);
            }
        }

        private void AlarmItem_Tapped(Alarm alarm)
        {
            _editingAlarm = alarm;
            NewAlarmTimePicker.Time = alarm.TargetTime;
            NewAlarmLabelTextBox.Text = alarm.Label ?? "";
            DailyCheckBox.IsChecked = alarm.IsDaily;
            if (!alarm.IsDaily && alarm.TargetDate.HasValue)
            {
                NewAlarmDatePicker.Date = alarm.TargetDate.Value;
            }
            
            AddAlarmButton.Content = LocalizationManager.GetString("UpdateAlarmButton") ?? "Update Alarm";
            CancelEditButton.Visibility = Visibility.Visible;
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            ResetAlarmInputs();
        }

        private void ResetAlarmInputs()
        {
            _editingAlarm = null;
            NewAlarmLabelTextBox.Text = "";
            NewAlarmTimePicker.Time = DateTime.Now.TimeOfDay;
            DailyCheckBox.IsChecked = true;
            AddAlarmButton.Content = LocalizationManager.GetString("AddAlarmButton");
            CancelEditButton.Visibility = Visibility.Collapsed;
        }

        private void AddAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingAlarm != null)
            {
                // Update existing
                _editingAlarm.TargetTime = NewAlarmTimePicker.Time;
                _editingAlarm.IsDaily = DailyCheckBox.IsChecked ?? false;
                _editingAlarm.TargetDate = DailyCheckBox.IsChecked == true ? null : (DateTime?)NewAlarmDatePicker.Date.DateTime;
                _editingAlarm.Label = NewAlarmLabelTextBox.Text;
                
                ResetAlarmInputs();
            }
            else
            {
                // Add new
                if (_mainWindow.Alarms.Count >= 10) return;

                var newAlarm = new Alarm
                {
                    TargetTime = NewAlarmTimePicker.Time,
                    IsDaily = DailyCheckBox.IsChecked ?? false,
                    TargetDate = DailyCheckBox.IsChecked == true ? null : (DateTime?)NewAlarmDatePicker.Date.DateTime,
                    Label = NewAlarmLabelTextBox.Text
                };

                _mainWindow.Alarms.Add(newAlarm);
            }

            _mainWindow.Alarms = new System.Collections.Generic.List<Alarm>(_mainWindow.Alarms); // Trigger setter/save
            RefreshAlarmsList();
        }

        private void DailyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (SpecificDatePanel != null)
            {
                SpecificDatePanel.Visibility = DailyCheckBox.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void DeleteAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                _mainWindow.Alarms.RemoveAll(a => a.Id == id);
                _mainWindow.Alarms = new System.Collections.Generic.List<Alarm>(_mainWindow.Alarms); // Trigger setter/save
                RefreshAlarmsList();
            }
        }

        private void ApplyLocalization()
        {
            string langCode = _mainWindow.Language;
            LocalizationManager.SetLanguage(langCode);
            
            bool isHe = langCode == "he";
            SettingsPivot.FlowDirection = isHe ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            GeneralTab.Header = isHe ? "כללי" : "General";
            AlarmsTab.Header = isHe ? "התראות" : "Alarms";

            SettingsHeader.Text = LocalizationManager.GetString("SettingsHeader");
            LanguageLabel.Text = LocalizationManager.GetString("LanguageLabel");
            TimeFormatLabel.Text = LocalizationManager.GetString("TimeFormatLabel");
            TimeFormatToggle.Header = LocalizationManager.GetString("TimeFormatToggle");
            TimeFontSizeLabel.Text = LocalizationManager.GetString("TimeFontSizeLabel");
            TimeFontFamilyLabel.Text = LocalizationManager.GetString("TimeFontFamilyLabel");
            TimeColorLabel.Text = LocalizationManager.GetString("TimeColorLabel");
            DateFormatLabel.Text = LocalizationManager.GetString("DateFormatLabel");
            DateFontSizeLabel.Text = LocalizationManager.GetString("DateFontSizeLabel");
            DateFontFamilyLabel.Text = LocalizationManager.GetString("DateFontFamilyLabel");
            DateColorLabel.Text = LocalizationManager.GetString("DateColorLabel");
            BackgroundLabel.Text = LocalizationManager.GetString("BackgroundLabel");
            SelectBackgroundButton.Content = LocalizationManager.GetString("SelectBackgroundButton");
            ClearBackgroundButton.Content = LocalizationManager.GetString("ClearBackgroundButton");
            WindowWidthLabel.Text = LocalizationManager.GetString("WindowWidthLabel");
            WindowHeightLabel.Text = LocalizationManager.GetString("WindowHeightLabel");
            AutoDismissLabel.Text = LocalizationManager.GetString("AutoDismissLabel");
            HebrewDateCheckBox.Content = LocalizationManager.GetString("HebrewDateLabel");
            HebrewDateTransitionLabel.Text = LocalizationManager.GetString("HebrewDateTransitionLabel");
            HebrewDateFontSizeLabel.Text = LocalizationManager.GetString("HebrewDateFontSizeLabel");
            HebrewDateFontFamilyLabel.Text = LocalizationManager.GetString("HebrewDateFontFamilyLabel");
            HebrewDateColorLabel.Text = LocalizationManager.GetString("HebrewDateColorLabel");

            AlertLabelFontSizeLabel.Text = LocalizationManager.GetString("AlertLabelFontSizeLabel");
            AlertLabelFontFamilyLabel.Text = LocalizationManager.GetString("AlertLabelFontFamilyLabel");
            AlertLabelColorLabel.Text = LocalizationManager.GetString("AlertLabelColorLabel");
            
            ((ComboBoxItem)TimeFormatComboBox.Items[0]).Content = LocalizationManager.GetString("None");
            ((ComboBoxItem)DateFormatComboBox.Items[0]).Content = LocalizationManager.GetString("None");

            CloseButton.Content = LocalizationManager.GetString("CloseButton");

            // Alarms Localization
            AlarmsHeader.Text = LocalizationManager.GetString("AlarmsHeader");
            AlarmLabelText.Text = LocalizationManager.GetString("AlarmLabelText");
            TimeLabel.Text = LocalizationManager.GetString("TimeLabel");
            SpecificDateLabel.Text = LocalizationManager.GetString("SpecificDateLabel");
            DailyCheckBox.Content = LocalizationManager.GetString("DailyLabel");
            AddAlarmButton.Content = _editingAlarm != null ? LocalizationManager.GetString("UpdateAlarmButton") : LocalizationManager.GetString("AddAlarmButton");
            CancelEditButton.Content = LocalizationManager.GetString("CancelButton") ?? "Cancel";
            if (_editingAlarm != null) CancelEditButton.Visibility = Visibility.Visible;

            RefreshAlarmsList();
        }

        private void TimeFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (TimeFormatComboBox.SelectedItem is ComboBoxItem item)
                _mainWindow.TimeFormat = item.Tag.ToString();
        }

        private void TimeFormatToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_mainWindow == null) return;
            _mainWindow.Use24Hour = TimeFormatToggle.IsOn;
        }

        private void TimeFontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.TimeFontSize = e.NewValue;
        }

        private void TimeFontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (TimeFontFamilyComboBox.SelectedItem is ComboBoxItem item)
                _mainWindow.TimeFontFamily = item.Tag.ToString();
        }

        private void TimeColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_mainWindow != null) _mainWindow.TimeColor = args.NewColor;
        }

        private void DateFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (DateFormatComboBox.SelectedItem is ComboBoxItem item)
                _mainWindow.DateFormat = item.Tag.ToString();
        }

        private void DateFontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.DateFontSize = e.NewValue;
        }

        private void DateFontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (DateFontFamilyComboBox.SelectedItem is ComboBoxItem item)
                _mainWindow.DateFontFamily = item.Tag.ToString();
        }

        private void DateColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_mainWindow != null) _mainWindow.DateColor = args.NewColor;
        }

        private async void SelectBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            
            // Get window handle for the picker
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _mainWindow.SetBackgroundImage(file.Path);
            }
        }

        private void ClearBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.SetBackgroundImage(null);
        }

        private void WindowWidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.WindowWidth = (int)e.NewValue;
        }

        private void WindowHeightSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.WindowHeight = (int)e.NewValue;
        }

        private void AutoDismissTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && int.TryParse(AutoDismissTextBox.Text, out int result))
            {
                _mainWindow.AlarmAutoDismissSeconds = result;
            }
        }

        private void HebrewDateCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.ShowHebrewDate = HebrewDateCheckBox.IsChecked == true;
                HebrewDateSettingsPanel.Visibility = _mainWindow.ShowHebrewDate ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void HebrewDateFontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.HebrewDateFontSize = e.NewValue;
        }

        private void HebrewDateFontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (HebrewDateFontFamilyComboBox.SelectedItem is ComboBoxItem item)
                _mainWindow.HebrewDateFontFamily = item.Tag.ToString();
        }

        private void HebrewDateColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_mainWindow != null) _mainWindow.HebrewDateColor = args.NewColor;
        }

        private void AlertLabelFontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mainWindow != null) _mainWindow.AlertLabelFontSize = e.NewValue;
        }

        private void AlertLabelFontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow == null) return;
            if (AlertLabelFontFamilyComboBox.SelectedItem is ComboBoxItem item)
                _mainWindow.AlertLabelFontFamily = item.Tag.ToString();
        }

        private void AlertLabelColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_mainWindow != null) _mainWindow.AlertLabelColor = args.NewColor;
        }

        private void HebrewDateTransitionTimePicker_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
        {
            if (_mainWindow != null && args.NewTime.HasValue)
            {
                _mainWindow.HebrewDateTransitionTime = args.NewTime.Value;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
