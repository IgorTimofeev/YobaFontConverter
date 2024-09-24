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

namespace YobaResourceConverter;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();

		if (App.Settings.Window.X > 0) {
			Left = App.Settings.Window.X;
			Top = App.Settings.Window.Y;

			Width = App.Settings.Window.Width;
			Height = App.Settings.Window.Height;

			WindowStartupLocation = WindowStartupLocation.Manual;
		}
		else {
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

		TabControl.SelectedIndex = App.Settings.TabIndex;
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
		base.OnRenderSizeChanged(sizeInfo);

		App.Settings.Window.X = (int) Left;
		App.Settings.Window.Y = (int) Top;

		App.Settings.Window.Width = (int) sizeInfo.NewSize.Width;
		App.Settings.Window.Height = (int) sizeInfo.NewSize.Height;
	}

	void OnWindowTopPanelMouseDown(object sender, MouseButtonEventArgs e) {
		if (e.LeftButton == MouseButtonState.Pressed)
			DragMove();
	}

	void OnWindowCloseButtonClick(object sender, RoutedEventArgs e) {
		Close();
	}

	void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e) {
		App.Settings.TabIndex = (byte) TabControl.SelectedIndex;
	}
}