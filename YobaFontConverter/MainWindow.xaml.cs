using Microsoft.Win32;
using Newtonsoft.Json;
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

		LoadSettings();
		FillFontFamilies();

		// Rendering callbacks
		RenderTimer = new(
			TimeSpan.FromMilliseconds(500),
			DispatcherPriority.ApplicationIdle,
			async (s, e) => {
				RenderTimer!.Stop();
				
				Render();

				await SaveSettingsAsync();
			},
			Dispatcher
		);

		RenderTimer.Stop();

		FontFamilyComboBox.SelectionChanged += (s, e) => {
			if (FontFamilyComboBox.SelectedItem is not FontFamily fontFamily)
				return;

			Settings!.FontFamily = fontFamily.Source;
			EnqueueRender();
		};

		void addTextBoxRenderCallback(TextBox textBox) {
			textBox.TextChanged += (s, e) => {
				if (textBox.Text.Length > 0)
					EnqueueRender();
			};
		}

		addTextBoxRenderCallback(FontSizeTextBox);
		addTextBoxRenderCallback(GlyphsFromTextBox);
		addTextBoxRenderCallback(GlyphsToTextBox);

		// Rendering glyphs once on startup
		Render();
	}

	static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YobaFontConverter");
	static readonly string SettingsPath = Path.Combine(AppDataPath, "Settings.json");

	SettingsJSON? Settings = null;
	FormattedText[]? GlyphsFormattedTexts = null;
	Typeface? GlyphsTypeface = null;
	RenderTargetBitmap? GlyphsBitmap = null;

	int
		GlyphsTotal = 94,
		GlyphsWidthTotal = 1,
		GlyphsHeightTotal = 1;

	DispatcherTimer RenderTimer;

	public ObservableCollection<FontFamily> FontFamilies { get; set; } = [];

	void LoadSettings() {
		if (!File.Exists(SettingsPath)) {
			Settings = new();
			return;
		}

		try {
			Settings = JsonConvert.DeserializeObject<SettingsJSON>(File.ReadAllText(SettingsPath)) ?? new();
		}
		catch (Exception ex) {
			Settings = new();
			MessageBox.Show(ex.Message);
			return;
		}
	}

	void FillFontFamilies() {
		int settingsFontFamilyCounter = 0;
		int settingsFontFamilyIndex = 0;

		foreach (var fontFamily in Fonts.SystemFontFamilies.OrderBy(o => o.Source)) {
			FontFamilies.Add(fontFamily);

			if (fontFamily.Source == Settings!.FontFamily)
				settingsFontFamilyIndex = settingsFontFamilyCounter;

			settingsFontFamilyCounter++;
		}

		FontFamilyComboBox.ItemsSource = FontFamilies;
		FontFamilyComboBox.SelectedIndex = settingsFontFamilyIndex;
	}

	async Task SaveSettingsAsync() {
		Directory.CreateDirectory(AppDataPath);

		await File.WriteAllTextAsync(SettingsPath, JsonConvert.SerializeObject(Settings!));
	}

	void Render() {
		if (!int.TryParse(FontSizeTextBox.Text, out Settings!.FontSize))
			Settings.FontSize = 16;

		if (!int.TryParse(GlyphsFromTextBox.Text, out Settings.GlyphsFrom))
			Settings.GlyphsFrom = 32;

		if (!int.TryParse(GlyphsToTextBox.Text, out Settings.GlyphsTo))
			Settings.GlyphsTo = 126;

		GlyphsTotal = Settings.GlyphsTo - Settings.GlyphsFrom + 1;

		if (GlyphsTotal <= 0) {
			MessageBox.Show("Retarded glyphs range");
			return;
		}

		GlyphsFormattedTexts = new FormattedText[GlyphsTotal];
		FormattedText formattedText;

		GlyphsTypeface = new(
			(FontFamily) FontFamilyComboBox.SelectedItem,
			FontStyles.Normal,
			FontWeights.Normal,
			FontStretches.Normal
		);

		DrawingVisual drawingVisual = new();

		int x = 0;
		int width;
		int height;

		GlyphsWidthTotal = 1;
		GlyphsHeightTotal = 1;

		using (var drawingContext = drawingVisual.RenderOpen()) {
			for (int i = 0; i < GlyphsTotal; i++) {
				GlyphsFormattedTexts[i] = formattedText = new(
					((char) (Settings.GlyphsFrom + i)).ToString(),
					CultureInfo.CurrentUICulture,
					FlowDirection.LeftToRight,
					GlyphsTypeface,
					Settings.FontSize,
					(SolidColorBrush) Application.Current.FindResource("ThemeFg1"),
					new NumberSubstitution(),
					TextFormattingMode.Display,
					VisualTreeHelper.GetDpi(this).PixelsPerDip
				);

				width = (int) Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
				height = (int) Math.Ceiling(formattedText.Height);

				GlyphsWidthTotal += width;
				GlyphsHeightTotal = Math.Max(GlyphsHeightTotal, height);

				drawingContext.DrawText(formattedText, new(x, 0));

				x += width;
			}
		}

		if (GlyphsHeightTotal > 256) {
			MessageBox.Show($"Retarded font size, pixel height is {GlyphsHeightTotal}, decrease pls");
			return;
		}

		// Rendering
		GlyphsBitmap = new(
			GlyphsWidthTotal,
			GlyphsHeightTotal,
			96,
			96,
			PixelFormats.Pbgra32
		);

		GlyphsBitmap.Render(drawingVisual);

		PreviewImage.Source = GlyphsBitmap;
		PreviewImage.Height = GlyphsHeightTotal;
	}

	void EnqueueRender() {
		RenderTimer.Stop();
		RenderTimer.Start();
	}

	void OnWindowTopPanelMouseDown(object sender, MouseButtonEventArgs e) {
		if (e.LeftButton == MouseButtonState.Pressed)
			DragMove();
	}

	void OnWindowCloseButtonClick(object sender, RoutedEventArgs e) {
		Close();
	}

	async void OnSaveButtonClick(object sender, RoutedEventArgs e) {
		if (GlyphsBitmap is null)
			return;

		var className = $"{FontNameRegex().Replace(GlyphsTypeface!.FontFamily.ToString(), "")}{Settings!.FontSize}Font";

		SaveFileDialog dialog = new() {
			FileName = $"{className}.h",
			Filter = "Header files|*.h"
		};

		if (dialog.ShowDialog() != true)
			return;

		// Bitmap
		int x = 0;

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

		FormattedText formattedText;
		int width;

		for (int i = 0; i < GlyphsFormattedTexts!.Length; i++) {
			formattedText = GlyphsFormattedTexts[i];

			width = (int) Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace);
			pixelStride = width * 4;
			pixelBuffer = new byte[pixelStride * GlyphsHeightTotal];

			GlyphsBitmap.CopyPixels(new(x, 0, width, GlyphsHeightTotal), pixelBuffer, pixelStride, 0);

			x += width;

			// Header glyph
			if (i > 0)
				glyphsSB.AppendLine();

			glyphsSB.Append($"\t\t\tGlyph({bitmapGlyphBitIndex}, {width}){(i < GlyphsFormattedTexts.Length - 1 ? "," : "")} // {(formattedText.Text == "\\" ? "backslash" : formattedText.Text)}");

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

			bitmapGlyphBitIndex += width * GlyphsHeightTotal;
		}

		// Last byte
		if (bitmapByteBitIndex > 0)
			flushBitmapByte();

		// Saving
		Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName) ?? "");

		using FileStream fileStream = new(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
		using StreamWriter streamWriter = new(fileStream, Encoding.UTF8);

		await streamWriter.WriteAsync($$"""
class {{className}} : public Font {
	public:
		{{className}}() : Font(
			{{Settings.GlyphsFrom}},
			{{Settings.GlyphsTo}},
			{{GlyphsHeightTotal}},
			_glyphs,
			_bitmap
		) {
			
		}

	private:
		PROGMEM const Glyph _glyphs[{{GlyphsTotal}}] = {
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