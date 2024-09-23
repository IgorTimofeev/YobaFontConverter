using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace YobaFontConverter;

public class UintToSolidColorBrushConverter : MarkupExtension, IValueConverter {
	public static UintToSolidColorBrushConverter Instance = new();

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		var uintColor = (uint) value;

		SolidColorBrush brush = new(Color.FromArgb(
			255,
			(byte) (uintColor >> 16 & 0xFF),
			(byte) (uintColor >> 8 & 0xFF),
			(byte) (uintColor & 0xFF)
		));

		brush.Freeze();

		return brush;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider) {
		return Instance;
	}
}

public class UintToHexStringConverter : MarkupExtension, IValueConverter {
	public static UintToHexStringConverter Instance = new();

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		return $"0x{(uint) value:X6}";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider) {
		return Instance;
	}
}

public partial class ImagePage : UserControl {
	public ImagePage() {
		InitializeComponent();

		if (!DesignerProperties.GetIsInDesignMode(this)) {
			UpdateVisualsFromSettings();
		}
	}

	void UpdateVisualsFromSettings() {
		// Palette
		PaletteComboBox.ItemsSource = App.Settings.Image.Palette;
		PaletteComboBox.SelectedIndex = App.Settings.Image.Palette.Length > 0 ? 0 : -1;
	}

	async void OnSaveButtonClick(object sender, RoutedEventArgs e) {

	}
}