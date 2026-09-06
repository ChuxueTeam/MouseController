using System.Text.Json;

namespace MouseController;

/// <summary>设置持久化：%APPDATA%\MouseController\settings.json</summary>
public sealed class AppSettings
{
    /// <summary>设置文件版本，用于旧版默认值迁移（v2：移动步长加大、新快捷键体系）。</summary>
    public int Version { get; set; } = 2;

    public int MoveLarge { get; set; } = 120;
    public int MoveMedium { get; set; } = 30;
    public int MoveSmall { get; set; } = 8;
    public int MoveTiny { get; set; } = 2;

    public int ClickCount { get; set; } = 10;
    public int ClickIntervalMs { get; set; } = 100;
    public int ClickButton { get; set; } = 0;

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
                    // v2 迁移：移动步长加大（旧默认 40/10/3/1 → 新默认 120/30/8/2）
                    if (s.Version < 2)
                    {
                        s.MoveLarge = 120;
                        s.MoveMedium = 30;
                        s.MoveSmall = 8;
                        s.MoveTiny = 2;
                        s.Version = 2;
                    }
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
