using Newtonsoft.Json;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace YobaFontConverter;

public partial class App : Application {
	public App() {
		LoadSettings();
	}

	public static SettingsJSON Settings;

	public static void LoadSettings() {
		if (File.Exists(Constants.SettingsPath)) {
			try {
				Settings = JsonConvert.DeserializeObject<SettingsJSON>(File.ReadAllText(Constants.SettingsPath)) ?? new();
			}
			catch (Exception ex) {
				Settings = new();
				MessageBox.Show(ex.Message);
			}
		}
		else {
			Settings = new();
		}
	}

	public static async Task SaveSettingsAsync() {
		Directory.CreateDirectory(Constants.AppDataPath);

		await File.WriteAllTextAsync(Constants.SettingsPath, JsonConvert.SerializeObject(Settings!, Formatting.Indented));
	}
}

