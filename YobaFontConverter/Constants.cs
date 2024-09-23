using System.IO;

namespace YobaFontConverter;

static class Constants {
	public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YobaResourceConverter");
	public static readonly string SettingsPath = Path.Combine(AppDataPath, "Settings.json");


}