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
	}


	void OnWindowTopPanelMouseDown(object sender, MouseButtonEventArgs e) {
		if (e.LeftButton == MouseButtonState.Pressed)
			DragMove();
	}

	void OnWindowCloseButtonClick(object sender, RoutedEventArgs e) {
		Close();
	}
}