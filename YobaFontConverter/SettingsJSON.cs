using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YobaFontConverter;

public class FontSettingsJSON {
	public string Family = "Arial";
	public int Size = 16;
	public int GlyphsFrom = 32;
	public int GlyphsTo = 126;
}

public enum ImageSettingsMode : byte {
	Default16Bit,
	Palette8Bit
}

public class ImageSettingsJSON {
	public ImageSettingsMode Mode = ImageSettingsMode.Default16Bit;

	public uint[] Palette = [
		0x000000,
		0xFFFFFF
	];
}

public class SettingsJSON {
	public FontSettingsJSON Font = new();
	public ImageSettingsJSON Image = new();
}
