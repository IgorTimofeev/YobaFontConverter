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
using ColorEx;
using ColorEx.Wpf;
using Newtonsoft.Json.Linq;

namespace YobaResourceConverter;

public partial class ImagePage : UserControl {
	public ImagePage() {
		InitializeComponent();

		RenderTimer = new(
			TimeSpan.FromMilliseconds(500),
			DispatcherPriority.ApplicationIdle,
			async (s, e) => {
				RenderTimer!.Stop();

				Render();
			},
			Dispatcher
		);

		RenderTimer.Stop();

		if (!DesignerProperties.GetIsInDesignMode(this)) {
			UpdateVisualsFromSettings();
			Render();
		}
	}

	readonly DispatcherTimer RenderTimer;

	int
		ExportWidth = 0,
		ExportHeight = 0;

	byte[] ExportBitmap = [];

	void UpdateVisualsFromSettings() {
		// Mode
		ModeComboBox.SelectedIndex = (byte) App.Settings.Image.Mode;

		// Path
		PathTextBox.Text = App.Settings.Image.Path;
		PathTextBox.TextChanged += (s, e) => EnqueueRender();

		// Palette
		PaletteTextBox.Text = string.Join(", ", App.Settings.Image.Palette.Select(o => $"{(o >= 0 ? "" : '-')}0x{Math.Abs(o):X6}"));
		PaletteTextBox.TextChanged += (s, e) => EnqueueRender();
	}

	void Render() {
		var paletteSettings =
			PaletteTextBox.Text
			.Replace("0x", "")
			.Split(
				[',', ' '],
				StringSplitOptions.RemoveEmptyEntries
			)
			.Select(
				stringColor => {
					var negative = false;

					// Hex numbers can't be parsed with trailing sign :(
					if (stringColor.StartsWith('-') && stringColor.Length > 1) {
						stringColor = stringColor[1..];
						negative = true;
					}

					int.TryParse(
						stringColor,
						NumberStyles.HexNumber,
						CultureInfo.CurrentUICulture,
						out var intColor
					);

					if (negative)
						intColor *= -1;

					return intColor;
				}
			).ToArray();

		if (paletteSettings.Length == 0)
			return;

		App.Settings.Image.Palette = paletteSettings;

		var paletteColors = paletteSettings.Select(o => o >= 0 ? (Color?) ((uint) o).ToColor().ChangeAlpha(0xFF) : null).ToArray();

		if (!File.Exists(PathTextBox.Text))
			return;

		App.Settings.Image.Path = PathTextBox.Text;

		BitmapImage originalImage = new(new Uri(PathTextBox.Text, UriKind.Absolute));
		PreviewImageOriginal.Source = originalImage;

		// Conversion itself
		ExportWidth = originalImage.PixelWidth;
		ExportHeight = originalImage.PixelHeight;

		WriteableBitmap convertedImage = new(
			ExportWidth,
			ExportHeight,
			96,
			96,
			PixelFormats.Pbgra32,
			null
		);

		var stride = ExportWidth * 4;
		var pixels = new byte[stride * ExportHeight];

		originalImage.CopyPixels(pixels, stride, 0);

		int
			exportBitmapIndex = 0,
			closestIndex,
			deltaR,
			deltaG,
			deltaB;

		double
			closestDelta,
			delta;

		Color? paletteColor;
		Color originalColor;

		ExportBitmap = new byte[ExportWidth * ExportHeight];

		for (int oc = 0; oc < pixels.Length; oc += 4) {
			originalColor = Color.FromArgb(
				// No alphas? :((
				0xFF,
				pixels[oc + 2],
				pixels[oc + 1],
				pixels[oc]
			);

			closestDelta = int.MaxValue;
			closestIndex = 0;

			for (int pi = 0; pi < paletteColors.Length; pi++) {
				paletteColor = paletteColors[pi];

				if (paletteColor is null)
					continue;

				deltaR = paletteColor.Value.R - originalColor.R;
				deltaG = paletteColor.Value.G - originalColor.G;
				deltaB = paletteColor.Value.B - originalColor.B;

				delta = Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);

				if (delta < closestDelta) {
					closestDelta = delta;
					closestIndex = pi;
				}
			}

			paletteColor = paletteColors[closestIndex]!;

			// Updating pixels with closest color data
			pixels[oc + 3] = 0xFF;
			pixels[oc + 2] = paletteColor.Value.R;
			pixels[oc + 1] = paletteColor.Value.G;
			pixels[oc] = paletteColor.Value.B;

			ExportBitmap[exportBitmapIndex] = (byte) closestIndex;
			exportBitmapIndex++;
		}

		convertedImage.WritePixels(
			new(0, 0, ExportWidth, ExportHeight),
			pixels,
			stride,
			0
		);

		PreviewImageConverted.Source = convertedImage;
	}

	void EnqueueRender() {
		RenderTimer.Stop();
		RenderTimer.Start();
	}

	private void OnPathButtonClick(object sender, RoutedEventArgs e) {
		OpenFileDialog dialog = new() {
			Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp"
		};

		if (dialog.ShowDialog() != true)
			return;

		PathTextBox.Text = dialog.FileName;
		EnqueueRender();
	}

	async void OnSaveButtonClick(object sender, RoutedEventArgs e) {
		var className = $"{App.GetHeaderNameRegex().Replace(Path.GetFileNameWithoutExtension(PathTextBox.Text), "")}Image";

		SaveFileDialog dialog = new() {
			Title = "Export image",
			FileName = $"{className}.h",
			Filter = "Header files|*.h"
		};

		if (dialog.ShowDialog() != true)
			return;

		// Maybe user had changed name via dialog
		className = Path.GetFileNameWithoutExtension(dialog.FileName);

		using FileStream fileStream = new(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
		using StreamWriter streamWriter = new(fileStream, Encoding.UTF8);

		StringBuilder sb = new("\t\t\t");

		int lineCounter = 0;

		for (int i = 0; i < ExportBitmap.Length; i++) {
			if (lineCounter > 0)
				sb.Append(' ');

			sb
				.Append("0x")
				.Append(ExportBitmap[i].ToString("X2"));

			if (i < ExportBitmap.Length - 1) {
				sb.Append(',');

				lineCounter++;

				if (lineCounter >= 16) {
					sb
						.AppendLine()
						.Append("\t\t\t");

					lineCounter = 0;
				}
			}
		}

		await streamWriter.WriteAsync($$"""
class {{className}} : public Image {
	public:
		{{className}}() : Image(
			Size(
				{{ExportWidth}},
				{{ExportHeight}}
			),
			_bitmap
		) {
			
		}

	private:
		PROGMEM const uint8_t _bitmap[{{ExportBitmap.Length}}] = {
{{sb}}
		};
};
""");
	}
}