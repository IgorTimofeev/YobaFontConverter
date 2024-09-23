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

namespace YobaFontConverter;

public partial class ImagePage : UserControl {
	public ImagePage() {
		InitializeComponent();

		RenderTimer = new(
			TimeSpan.FromMilliseconds(500),
			DispatcherPriority.ApplicationIdle,
			async (s, e) => {
				RenderTimer!.Stop();

				Render();

				await App.SaveSettingsAsync();
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

	void UpdateVisualsFromSettings() {
		// Mode
		ModeComboBox.SelectedIndex = (byte) App.Settings.Image.Mode;

		// Path
		PathTextBox.Text = App.Settings.Image.Path;
		PathTextBox.TextChanged += (s, e) => EnqueueRender();

		// Palette
		PaletteTextBox.Text = string.Join(", ", App.Settings.Image.Palette.Select(uintColor => $"0x{uintColor:X6}"));
		PaletteTextBox.TextChanged += (s, e) => EnqueueRender();
	}

	void Render() {
		var paletteUints =
			PaletteTextBox.Text
			.Replace("0x", "")
			.Split(
				[',', ' '],
				StringSplitOptions.RemoveEmptyEntries
			)
			.Select(
				stringColor => uint.TryParse(
					stringColor,
					NumberStyles.HexNumber,
					CultureInfo.CurrentUICulture,
					out var uintColor
				)
				? uintColor
				: 0
			).ToArray();

		if (paletteUints.Length == 0)
			return;

		var paletteColors = paletteUints.Select(o => o.ToColor().ChangeAlpha(0xFF)).ToArray();

		App.Settings.Image.Palette = paletteUints;

		if (!File.Exists(PathTextBox.Text))
			return;

		App.Settings.Image.Path = PathTextBox.Text;

		BitmapImage originalImage = new(new Uri(PathTextBox.Text, UriKind.Absolute));
		PreviewImageOriginal.Source = originalImage;

		WriteableBitmap convertedImage = new(
			originalImage.PixelWidth,
			originalImage.PixelHeight,
			96,
			96,
			PixelFormats.Pbgra32,
			null
		);

		var stride = originalImage.PixelWidth * 4;
		var pixels = new byte[stride * originalImage.PixelHeight];

		originalImage.CopyPixels(pixels, stride, 0);

		int
			closestIndex,
			deltaR,
			deltaG,
			deltaB;

		double
			closestDelta,
			delta;

		Color originalColor, paletteColor;

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

				deltaR = paletteColor.R - originalColor.R;
				deltaG = paletteColor.G - originalColor.G;
				deltaB = paletteColor.B - originalColor.B;

				delta = Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);

				if (delta < closestDelta) {
					closestDelta = delta;
					closestIndex = pi;
				}
			}

			paletteColor = paletteColors[closestIndex];

			// Updating pixels with closest color data
			pixels[oc + 3] = 0xFF;
			pixels[oc + 2] = paletteColor.R;
			pixels[oc + 1] = paletteColor.G;
			pixels[oc] = paletteColor.B;
		}

		convertedImage.WritePixels(
			new(0, 0, originalImage.PixelWidth, originalImage.PixelHeight),
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

	void OnSaveButtonClick(object sender, RoutedEventArgs e) {

	}
}