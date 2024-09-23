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

public partial class ImagePage : UserControl {
	public ImagePage() {
		InitializeComponent();

		if (!DesignerProperties.GetIsInDesignMode(this)) {
			UpdateVisualsFromSettings();
		}
	}

	ObservableCollection<uint> PaletteComboBoxItems;

	bool PaletteEditMode {
		get => PaletteTextBox.Visibility == Visibility.Visible;
		set {
			PaletteTextBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed;

			PaletteComboBox.Visibility =
			PaletteAddButton.Visibility =
			PaletteRemoveButton.Visibility = value ? Visibility.Collapsed : Visibility.Visible;

			PaletteEditButton.Content = value ? "💾" : "✎";
		}
	}

	void UpdateVisualsFromSettings() {
		// Mode
		ModeComboBox.SelectedIndex = (byte) App.Settings.Image.Mode;

		// Palette
		PaletteComboBoxItems = new(App.Settings.Image.Palette);
		PaletteComboBox.ItemsSource = PaletteComboBoxItems;
		PaletteComboBox.SelectedIndex = PaletteComboBoxItems.Count > 0 ? 0 : -1;
	}

	private void PaletteRemoveButton_Click(object sender, RoutedEventArgs e) {
		if (PaletteComboBoxItems.Count <= 1)
			return;

		var index = PaletteComboBox.SelectedIndex;
		PaletteComboBoxItems.RemoveAt(index);
		PaletteComboBox.SelectedIndex = Math.Clamp(index, 0, PaletteComboBoxItems.Count - 1);
	}

	private void PaletteEditButton_Click(object sender, RoutedEventArgs e) {
		PaletteEditMode = !PaletteEditMode;

		if (PaletteEditMode) {
			PaletteTextBox.Focus();
		}
	}

	private void PaletteAddButton_Click(object sender, RoutedEventArgs e) {
		PaletteComboBoxItems.Add(0xFFFFFF);
		PaletteComboBox.SelectedIndex = PaletteComboBoxItems.Count - 1;
		PaletteEditMode = true;
	}

	private void PaletteTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (!uint.TryParse(PaletteTextBox.Text, out var value))
			return;

		PaletteComboBoxItems[PaletteComboBox.SelectedIndex] = value;
		PaletteComboBox.Items.Refresh();
	}


	private void PaletteTextBox_KeyDown(object sender, KeyEventArgs e) {
		if (e.Key is not Key.Enter)
			return;

		PaletteEditMode = false;
	}

	void OnSaveButtonClick(object sender, RoutedEventArgs e) {

	}
}