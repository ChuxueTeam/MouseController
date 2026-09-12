using System.Text.Json;

namespace MouseController;

/// <summary>设置持久化：%APPDATA%\MouseController\settings.json</summary>
public sealed class AppSettings
{
    /// <summary>设置文件版本（v3：新热键体系 + 字体设置）。</summary>
    public int Version { get; set; } = 3;

    public int MoveLarge { get; set; } = 120;
    public int MoveMedium { get; set; } = 30;
    public int MoveSmall { get; set; } = 8;
    public int MoveTiny { get; set; } = 2;

    public int ClickCount { get; set; } = 10;
    public int ClickIntervalMs { get; set; } = 100;
    public int ClickButton { get; set; } = 0;

    /// <summary>界面字体族。</summary>
    public string FontFamily { get; set; } = "Microsoft YaHei UI";

    /// <summary>界面字号。</summary>
    public float FontSize { get; set; } = 9F;

    /// <summary>可自定义快捷键。</summary>
    public HotkeySettings Hotkeys { get; set; } = new();

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MouseController", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (s != null)
                {
                    // 旧版本迁移：移动步长默认值加大
                    if (s.Version < 2)
                    {
                        s.MoveLarge = 120;
                        s.MoveMedium = 30;
                        s.MoveSmall = 8;
                        s.MoveTiny = 2;
                    }
                    // 缺失的字段由属性初始化器补默认值（字体、热键等）
                    if (string.IsNullOrWhiteSpace(s.FontFamily)) s.FontFamily = "Microsoft YaHei UI";
                    if (s.FontSize < 7F || s.FontSize > 16F) s.FontSize = 9F;
                    s.Hotkeys ??= new HotkeySettings();
                    s.Version = 3;
                    return s;
                }
            }
        }
        catch
        {
            // 损坏则用默认值
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // 保存失败不影响使用
        }
    }
}
