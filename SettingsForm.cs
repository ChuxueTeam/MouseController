using System.Drawing.Text;

namespace MouseController;

/// <summary>设置对话框：自定义快捷键 + 界面字体。</summary>
public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly MainForm _main;
    private readonly HotkeySettings _hotkeys;
    private readonly List<HotkeyRow> _rows = new();

    private ComboBox _cmbFont = null!;
    private NumericUpDown _nudSize = null!;
    private Label _lblPreview = null!;

    private HotkeyRow? _capturing;

    private sealed class HotkeyRow
    {
        public required string Title;
        public required bool Directional;
        public required Func<KeyBinding> Get;
        public required Action<KeyBinding> Set;
        public required Func<HotkeySettings, KeyBinding> Pick;
        public Label ValueLabel = null!;
        public Button Button = null!;
    }

    public SettingsForm(AppSettings settings, MainForm main)
    {
        _settings = settings;
        _main = main;
        _hotkeys = settings.Hotkeys.Clone(); // 操作副本，取消不影响原设置
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "设置";
        ClientSize = new Size(470, 524);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font(_settings.FontFamily, _settings.FontSize);

        // ---------- 快捷键 ----------
        var grpKeys = new GroupBox
        {
            Text = "快捷键",
            Location = new Point(12, 12),
            Size = new Size(446, 344),
        };
        var hint = new Label
        {
            Text = "点「修改」后按下新组合键（必须含至少一个修饰键）；移动类只改修饰键，方向键固定。",
            Location = new Point(18, 20),
            Size = new Size(410, 20),
            ForeColor = Color.DimGray,
        };
        grpKeys.Controls.Add(hint);

        int i = 0;
        AddRow(grpKeys, i++, "大幅移动", true, () => _hotkeys.MoveLarge, b => _hotkeys.MoveLarge = b, h => h.MoveLarge);
        AddRow(grpKeys, i++, "中幅移动", true, () => _hotkeys.MoveMedium, b => _hotkeys.MoveMedium = b, h => h.MoveMedium);
        AddRow(grpKeys, i++, "小幅移动", true, () => _hotkeys.MoveSmall, b => _hotkeys.MoveSmall = b, h => h.MoveSmall);
        AddRow(grpKeys, i++, "微小移动", true, () => _hotkeys.MoveTiny, b => _hotkeys.MoveTiny = b, h => h.MoveTiny);
        AddRow(grpKeys, i++, "左键点击", false, () => _hotkeys.ClickLeft, b => _hotkeys.ClickLeft = b, h => h.ClickLeft);
        AddRow(grpKeys, i++, "中键点击", false, () => _hotkeys.ClickMiddle, b => _hotkeys.ClickMiddle = b, h => h.ClickMiddle);
        AddRow(grpKeys, i++, "右键点击", false, () => _hotkeys.ClickRight, b => _hotkeys.ClickRight = b, h => h.ClickRight);
        AddRow(grpKeys, i++, "连点器启停", false, () => _hotkeys.ClickerToggle, b => _hotkeys.ClickerToggle = b, h => h.ClickerToggle);
        AddRow(grpKeys, i++, "呼出主界面", false, () => _hotkeys.ShowMain, b => _hotkeys.ShowMain = b, h => h.ShowMain);
        AddRow(grpKeys, i++, "退出程序", false, () => _hotkeys.Quit, b => _hotkeys.Quit = b, h => h.Quit);
        Controls.Add(grpKeys);

        // ---------- 字体 ----------
        var grpFont = new GroupBox
        {
            Text = "字体",
            Location = new Point(12, 364),
            Size = new Size(446, 112),
        };
        grpFont.Controls.Add(new Label { Text = "字体", Location = new Point(18, 32), AutoSize = true });
        _cmbFont = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(60, 28),
            Size = new Size(236, 25),
        };
        PopulateFonts();
        grpFont.Controls.Add(_cmbFont);

        grpFont.Controls.Add(new Label { Text = "字号", Location = new Point(314, 32), AutoSize = true });
        _nudSize = new NumericUpDown
        {
            Location = new Point(356, 28),
            Size = new Size(62, 23),
            Minimum = 8,
            Maximum = 14,
            Value = (decimal)Math.Clamp(_settings.FontSize, 8F, 14F),
        };
        grpFont.Controls.Add(_nudSize);

        _lblPreview = new Label
        {
            Text = "预览：MouseController 鼠标控制器 0123",
            Location = new Point(18, 68),
            Size = new Size(410, 30),
        };
        grpFont.Controls.Add(_lblPreview);
        Controls.Add(grpFont);

        _cmbFont.SelectedIndexChanged += (_, _) => UpdatePreview();
        _nudSize.ValueChanged += (_, _) => UpdatePreview();
        UpdatePreview();

        // ---------- 底部按钮 ----------
        var btnDefaults = new Button
        {
            Text = "恢复默认快捷键",
            Location = new Point(12, 486),
            Size = new Size(150, 30),
        };
        btnDefaults.Click += (_, _) => RestoreDefaults();
        Controls.Add(btnDefaults);

        var btnOk = new Button
        {
            Text = "确定",
            Location = new Point(292, 486),
            Size = new Size(80, 30),
        };
        btnOk.Click += (_, _) =>
        {
            Apply();
            DialogResult = DialogResult.OK;
        };
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(378, 486),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel,
        };
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void AddRow(GroupBox parent, int index, string title, bool directional,
        Func<KeyBinding> get, Action<KeyBinding> set, Func<HotkeySettings, KeyBinding> pick)
    {
        int y = 48 + index * 29;
        var row = new HotkeyRow
        {
            Title = title,
            Directional = directional,
            Get = get,
            Set = set,
            Pick = pick,
        };
        parent.Controls.Add(new Label { Text = title, Location = new Point(20, y + 3), Size = new Size(90, 22) });
        row.ValueLabel = new Label
        {
            Location = new Point(114, y + 3),
            Size = new Size(196, 22),
            Text = get().Display(directional),
        };
        parent.Controls.Add(row.ValueLabel);
        row.Button = new Button
        {
            Text = "修改",
            Location = new Point(318, y),
            Size = new Size(64, 26),
        };
        row.Button.Click += (_, _) => StartCapture(row);
        parent.Controls.Add(row.Button);
        _rows.Add(row);
    }

    // ---------- 快捷键捕获 ----------

    private void StartCapture(HotkeyRow row)
    {
        if (_capturing != null)
        {
            // 结束上一次捕获
            _main.EndCapture();
            _capturing.ValueLabel.ForeColor = SystemColors.ControlText;
            _capturing.ValueLabel.Text = _capturing.Get().Display(_capturing.Directional);
        }
        _capturing = row;
        row.ValueLabel.ForeColor = Color.FromArgb(27, 110, 243);
        row.ValueLabel.Text = "请按新快捷键…";
        _main.BeginCapture(h => OnCaptured(row, h));
    }

    private void OnCaptured(HotkeyRow row, HotkeyInfo h)
    {
        _main.EndCapture();
        _capturing = null;

        if (h.ModCount == 0)
        {
            row.ValueLabel.ForeColor = Color.Firebrick;
            row.ValueLabel.Text = "需要至少一个修饰键";
            RestoreLater(row);
            return;
        }

        var candidate = new KeyBinding
        {
            Ctrl = h.Ctrl,
            Shift = h.Shift,
            Alt = h.Alt,
            Win = h.Win,
            Vk = row.Directional ? 0u : h.Vk,
        };

        foreach (var other in _rows)
        {
            if (ReferenceEquals(other, row)) continue;
            if (other.Get().ConflictsWith(candidate))
            {
                row.ValueLabel.ForeColor = Color.Firebrick;
                row.ValueLabel.Text = $"与「{other.Title}」冲突";
                RestoreLater(row);
                return;
            }
        }

        row.Set(candidate);
        row.ValueLabel.ForeColor = SystemColors.ControlText;
        row.ValueLabel.Text = candidate.Display(row.Directional);
    }

    private void RestoreLater(HotkeyRow row)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 1600 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            if (_capturing == null)
            {
                row.ValueLabel.ForeColor = SystemColors.ControlText;
                row.ValueLabel.Text = row.Get().Display(row.Directional);
            }
        };
        timer.Start();
    }

    private void RestoreDefaults()
    {
        if (_capturing != null)
        {
            _main.EndCapture();
            _capturing = null;
        }
        var defaults = new HotkeySettings();
        foreach (var row in _rows)
        {
            row.Set(row.Pick(defaults));
            row.ValueLabel.ForeColor = SystemColors.ControlText;
            row.ValueLabel.Text = row.Get().Display(row.Directional);
        }
    }

    // ---------- 字体 ----------

    private void PopulateFonts()
    {
        string[] prefer =
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "等线", "宋体", "黑体", "楷体", "仿宋",
            "Segoe UI", "Consolas",
        };

        var installed = new List<string>();
        try
        {
            using var ifc = new InstalledFontCollection();
            foreach (var f in ifc.Families) installed.Add(f.Name);
        }
        catch
        {
            // 忽略枚举失败
        }

        var set = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);
        foreach (var p in prefer)
            if (set.Contains(p)) _cmbFont.Items.Add(p);
        foreach (var n in installed.OrderBy(x => x, StringComparer.CurrentCulture))
            if (!_cmbFont.Items.Contains(n)) _cmbFont.Items.Add(n);

        int idx = _cmbFont.Items.IndexOf(_settings.FontFamily);
        if (idx < 0)
        {
            _cmbFont.Items.Insert(0, _settings.FontFamily);
            idx = 0;
        }
        _cmbFont.SelectedIndex = idx;
    }

    private void UpdatePreview()
    {
        string family = _cmbFont.SelectedItem?.ToString() ?? _settings.FontFamily;
        float size = (float)_nudSize.Value;
        try
        {
            _lblPreview.Font = new Font(family, size + 1F);
        }
        catch
        {
            _lblPreview.Font = new Font("Microsoft YaHei UI", size + 1F);
        }
    }

    // ---------- 应用 ----------

    private void Apply()
    {
        _settings.Hotkeys = _hotkeys;
        if (_cmbFont.SelectedItem != null) _settings.FontFamily = _cmbFont.SelectedItem.ToString()!;
        _settings.FontSize = (float)_nudSize.Value;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _main.EndCapture();
        base.OnFormClosing(e);
    }
}
