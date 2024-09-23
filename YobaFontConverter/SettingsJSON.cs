using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YobaFontConverter;

public class SettingsJSON {
	public string FontFamily = "Arial";
	public int FontSize = 16;
	public int GlyphsFrom = 32;
	public int GlyphsTo = 126;
}
