using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YobaFontConverter;

public class FontSettingsJSON {
	public string Family = "Arial";
	public int Size = 16;
	public int From = 32;
	public int To = 126;
}

public enum ImageSettingsMode : byte {
	Default16Bit,
	Palette
}

public class ImageSettingsPaletteColorJSON {
	public ImageSettingsPaletteColorJSON() {
		
	}

	public ImageSettingsPaletteColorJSON(uint value, bool enabled = true) {
		Value = value;
		Enabled = enabled;
	}

	public ImageSettingsPaletteColorJSON(string stringValue) {
		if (stringValue.StartsWith('-') && stringValue.Length > 1) {
			Enabled = false;
			stringValue = stringValue[1..];
		}
		else {
			Enabled = true;
		}

		uint.TryParse(
			stringValue,
			NumberStyles.HexNumber,
			CultureInfo.CurrentUICulture,
			out Value
		);
	}

	public uint Value;
	public bool Enabled;

	public override string ToString() {
		return $"{(Enabled ? "" : '-')}0x{Value:X6}";
	}
}

public class ImageSettingsJSON {
	public ImageSettingsMode Mode = ImageSettingsMode.Default16Bit;

	public ImageSettingsPaletteColorJSON[] Palette = [
		new(0x000000),
		new(0xFF00FF, false),
		new(0xFFFFFF)
	];

	public string Path = string.Empty;
}

public class WindowSettingsJSON {
	public int
		X = 0,
		Y = 0,
		Width = 0,
		Height = 0;
}

public class SettingsJSON {
	public WindowSettingsJSON Window = new();
	public FontSettingsJSON Font = new();
	public ImageSettingsJSON Image = new();
	public byte TabIndex = 0;
}
