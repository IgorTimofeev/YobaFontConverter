using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();

		FontFamilyComboBox.ItemsSource = FontFamilies;
		FontFamilyComboBox.SelectedIndex = 0;

		FontFamilyComboBox.SelectionChanged += (s, e) => {
			if (FontFamilyComboBox.SelectedItem is not FontFamily fontFamily)
				return;

		};
	}

	public ObservableCollection<FontFamily> FontFamilies { get; set; } = new(Fonts.SystemFontFamilies.OrderBy(o => o.Source));

	void WindowTopPanel_MouseDown(object sender, MouseButtonEventArgs e) {
		if (e.LeftButton == MouseButtonState.Pressed)
			DragMove();
	}

	void WindowCloseButton_Click(object sender, RoutedEventArgs e) {
		Close();
	}


	async void OnSaveButtonClick(object sender, RoutedEventArgs e) {
		//FontSizeTextBox.FontFamily = new(new Uri("C:\\Users\\Igor\\AppData\\Local\\Microsoft\\Windows\\Fonts", UriKind.Absolute), "unscii 8 thin");

		//return;

		OpenFolderDialog dialog = new();

		if (dialog.ShowDialog() != true)
			return;

		if (!int.TryParse(FontSizeTextBox.Text, out var fontSize))
			fontSize = 16;

		if (!int.TryParse(GlyphsFromTextBox.Text, out var glyphsFrom))
			glyphsFrom = 32;

		if (!int.TryParse(GlyphsToTextBox.Text, out var glyphsTo))
			glyphsTo = 126;

		int glyphsTotal = glyphsTo - glyphsFrom + 1;

		if (glyphsTotal <= 0) {
			MessageBox.Show("Retarded glyphs range");
			return;
		}

		FormattedText[] formattedTexts = new FormattedText[glyphsTotal];
		FormattedText formattedText;

		Typeface typeface = new(
			(FontFamily) FontFamilyComboBox.SelectedItem,
			FontStyles.Normal,
			FontWeights.Normal,
			FontStretches.Normal
		);

		DrawingVisual drawingVisual = new();

		int x = 0;
		int width;
		int height;

		int widthTotal = 1;
		int heightTotal = 1;

		using (var drawingContext = drawingVisual.RenderOpen()) {
			for (int i = 0; i < glyphsTotal; i++) {
				formattedTexts[i] = formattedText = new(
					((char) (glyphsFrom + i)).ToString(),
					CultureInfo.CurrentUICulture,
					FlowDirection.LeftToRight,
					typeface,
					fontSize,
					Brushes.Black,
					new NumberSubstitution(),
					TextFormattingMode.Display,
					VisualTreeHelper.GetDpi(this).PixelsPerDip
				);

				width = (int) Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
				height = (int) Math.Ceiling(formattedText.Height);

				widthTotal += width;
				heightTotal = Math.Max(heightTotal, height);

				drawingContext.DrawText(formattedText, new(x, 0));

				x += width;
			}
		}

		if (heightTotal > 256) {
			MessageBox.Show($"Retarded font size, pixel height is {heightTotal}, decrease pls");
			return;
		}

		// Rendering
		RenderTargetBitmap bitmap = new(
			widthTotal,
			heightTotal,
			96,
			96,
			PixelFormats.Pbgra32
		);

		bitmap.Render(drawingVisual);

		// Bitmap
		x = 0;

		StringBuilder
			glyphsSB = new(),
			bitmapSB = new("\t\t\t");

		int bitmapGlyphBitIndex = 0;
		int bitmapByteIndex = 0;
		int bitmapByte = 0;
		int bitmapBytesTotal = 0;
		int bitmapByteBitIndex = 0;

		byte[] pixelBuffer;
		int pixelStride;

		void flushBitmapByte() {
			bitmapSB.Append($"0x{bitmapByte:X2},");
			bitmapByte = 0;
			bitmapBytesTotal++;

			// Bytes per line
			bitmapByteIndex++;

			if (bitmapByteIndex > 15) {
				bitmapByteIndex = 0;
				bitmapSB.Append($"{Environment.NewLine}\t\t\t");
			}
			else {
				bitmapSB.Append(' ');
			}
		}

		for (int i = 0; i < formattedTexts.Length; i++) {
			formattedText = formattedTexts[i];

			width = (int) Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
			pixelStride = width * 4;
			pixelBuffer = new byte[pixelStride * heightTotal];

			bitmap.CopyPixels(new(x, 0, width, heightTotal), pixelBuffer, pixelStride, 0);

			x += width;

			// Header glyph
			if (i > 0)
				glyphsSB.AppendLine();

			glyphsSB.Append($"\t\t\tGlyph({bitmapGlyphBitIndex}, {width}){(i < formattedTexts.Length - 1 ? "," : "")} // {(formattedText.Text == "\\" ? "backslash" : formattedText.Text)}");

			for (int j = 0; j < pixelBuffer.Length; j += 4) {
				// If alpha has value - there's definitely some pixel data
				if (pixelBuffer[j + 3] > 0)
					bitmapByte |= 1 << bitmapByteBitIndex;

				// Flushing byte if required
				bitmapByteBitIndex += 1;

				if (bitmapByteBitIndex > 7) {
					flushBitmapByte();
					bitmapByteBitIndex = 0;
				}
			}

			bitmapGlyphBitIndex += width * heightTotal;
		}

		// Last byte
		if (bitmapByteBitIndex > 0)
			flushBitmapByte();

		PreviewImage.Source = bitmap;
		PreviewImage.Height = heightTotal;

		// Saving
		Directory.CreateDirectory(dialog.FolderName);
		var name = $"{FontNameRegex().Replace(typeface.FontFamily.ToString(), "")}{fontSize}Font";
		var className = name[..1].ToUpper() + name[1..];
		var headerFilePath = Path.Combine(dialog.FolderName, $"{name}.h");

		using FileStream saveStream = new(headerFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
		using StreamWriter streamWriter = new(saveStream, Encoding.UTF8);

		await streamWriter.WriteAsync($$"""
class {{className}} : public Font {
	public:
		{{className}}() : Font(
			{{glyphsFrom}},
			{{glyphsTo}},
			{{heightTotal}},
			_glyphs,
			_bitmap
		) {
			
		}

	private:
		PROGMEM const Glyph _glyphs[{{glyphsTotal}}] = {
{{glyphsSB}}
		};

		PROGMEM const uint8_t _bitmap[{{bitmapBytesTotal}}] = {
{{bitmapSB}}
		};
};
""");
	}

	[GeneratedRegex(@"\W+")]
	private static partial Regex FontNameRegex();
}