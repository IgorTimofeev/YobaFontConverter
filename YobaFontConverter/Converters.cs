using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

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