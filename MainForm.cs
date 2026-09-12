namespace MouseController;

public sealed class MainForm : Form
{
    private readonly KeyboardHook _hook = new();
    private readonly AutoClicker _clicker = new();
    private readonly AppSettings _settings;

    private NotifyIcon? _trayIcon;
    private bool _exitRequested;

    /// <summary>设置对话框请求捕获新快捷键时的回调（非空即处于捕获模式）。</summary>
    private Action<HotkeyInfo>? _captureHandler;

    // ---- 控件 ----
    private Label _lblCoords = null!;
    private NumericUpDown _nudLarge = null!, _nudMedium = null!, _nudSmall = null!, _nudTiny = null!;
    private NumericUpDown _nudJumpX = null!, _nudJumpY = null!;
    private ComboBox _cmbButton = null!;
    private NumericUpDown _nudCount = null!, _nudInterval = null!;
    private Button _btnClick = null!;
    private Label _lblClickStatus = null!;
    private TextBox _lblKeys = null!;
    private System.Windows.Forms.Timer _posTimer = null!;

    // ---- 移动步长 ----
    private int StepLarge => (int)_nudLarge.Value;
    private int StepMedium => (int)_nudMedium.Value;
    private int StepSmall => (int)_nudSmall.Value;
    private int StepTiny => (int)_nudTiny.Value;

    public MainForm(AppSettings settings)
    {
        _settings = settings;
        BuildUi();
        ApplyFonts();
        _clicker.Progress += n => SafeUi(() => _lblClickStatus.Text = $"正在点击：{n} / {(int)_nudCount.Value}");
        _clicker.Finished += OnClickerFinished;
    }

    // ================= UI 构建 =================

    private void BuildUi()
    {
        Text = "MouseController · 鼠标控制器";
        ClientSize = new Size(560, 740);
        MinimumSize = new Size(584, 782);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // ---- 鼠标坐标 ----
        var lblTitle = new Label
        {
            Text = "鼠标坐标",
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            Location = new Point(15, 10),
            AutoSize = true,
        };
        _lblCoords = new Label
        {
            Text = "X: 0    Y: 0",
            Location = new Point(15, 30),
            Size = new Size(526, 42),
        };
        Controls.Add(lblTitle);
        Controls.Add(_lblCoords);

        // ---- 移动步长 ----
        var grpMove = new GroupBox
        {
            Text = "移动步长（像素）",
            Location = new Point(12, 84),
            Size = new Size(532, 76),
        };
        _nudLarge = AddStepRow(grpMove, 0, "大幅", _settings.MoveLarge);
        _nudMedium = AddStepRow(grpMove, 1, "中幅", _settings.MoveMedium);
        _nudSmall = AddStepRow(grpMove, 2, "小幅", _settings.MoveSmall);
        _nudTiny = AddStepRow(grpMove, 3, "微小", _settings.MoveTiny);
        Controls.Add(grpMove);

        // ---- 移动到指定坐标 ----
        var grpJump = new GroupBox
        {
            Text = "移动到指定坐标",
            Location = new Point(12, 166),
            Size = new Size(532, 72),
        };
        grpJump.Controls.Add(new Label { Text = "X", Location = new Point(24, 22), AutoSize = true });
        _nudJumpX = new NumericUpDown
        {
            Location = new Point(46, 18),
            Size = new Size(100, 23),
            Minimum = -32768,
            Maximum = 65535,
        };
        grpJump.Controls.Add(_nudJumpX);

        grpJump.Controls.Add(new Label { Text = "Y", Location = new Point(174, 22), AutoSize = true });
        _nudJumpY = new NumericUpDown
        {
            Location = new Point(196, 18),
            Size = new Size(100, 23),
            Minimum = -32768,
            Maximum = 65535,
        };
        grpJump.Controls.Add(_nudJumpY);

        var btnJumpPick = new Button
        {
            Text = "取当前坐标",
            Location = new Point(310, 16),
            Size = new Size(95, 28),
        };
        btnJumpPick.Click += (_, _) =>
        {
            Native.GetCursorPos(out var p);
            _nudJumpX.Value = Math.Clamp(p.X, _nudJumpX.Minimum, _nudJumpX.Maximum);
            _nudJumpY.Value = Math.Clamp(p.Y, _nudJumpY.Minimum, _nudJumpY.Maximum);
        };
        grpJump.Controls.Add(btnJumpPick);

        var btnJump = new Button
        {
            Text = "移动到这里",
            Location = new Point(415, 16),
            Size = new Size(100, 28),
        };
        btnJump.Click += (_, _) => Native.SetCursorPos((int)_nudJumpX.Value, (int)_nudJumpY.Value);
        grpJump.Controls.Add(btnJump);
        Controls.Add(grpJump);

        // ---- 连点器 ----
        var grpClick = new GroupBox
        {
            Text = "连点器",
            Location = new Point(12, 244),
            Size = new Size(532, 92),
        };
        grpClick.Controls.Add(new Label { Text = "按键", Location = new Point(24, 22), AutoSize = true });
        _cmbButton = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(66, 18),
            Size = new Size(88, 25),
        };
        _cmbButton.Items.AddRange(new object[] { "左键", "中键", "右键" });
        _cmbButton.SelectedIndex = _settings.ClickButton is >= 0 and <= 2 ? _settings.ClickButton : 0;
        grpClick.Controls.Add(_cmbButton);

        grpClick.Controls.Add(new Label { Text = "次数", Location = new Point(182, 22), AutoSize = true });
        _nudCount = new NumericUpDown
        {
            Location = new Point(224, 18),
            Size = new Size(70, 23),
            Minimum = 1,
            Maximum = 99999,
            Value = Math.Clamp(_settings.ClickCount, 1, 99999),
        };
        grpClick.Controls.Add(_nudCount);

        grpClick.Controls.Add(new Label { Text = "间隔(ms)", Location = new Point(316, 22), AutoSize = true });
        _nudInterval = new NumericUpDown
        {
            Location = new Point(380, 18),
            Size = new Size(56, 23),
            Minimum = 0,
            Maximum = 5000,
            Value = Math.Clamp(_settings.ClickIntervalMs, 0, 5000),
        };
        grpClick.Controls.Add(_nudInterval);

        _btnClick = new Button
        {
            Text = "开始连点",
            Location = new Point(24, 54),
            Size = new Size(110, 28),
        };
        _btnClick.Click += (_, _) => ToggleClicker();
        grpClick.Controls.Add(_btnClick);

        _lblClickStatus = new Label
        {
            Text = "就绪（间隔 0 = 极速连点）",
            Location = new Point(150, 60),
            Size = new Size(300, 22),
            ForeColor = Color.DimGray,
        };
        grpClick.Controls.Add(_lblClickStatus);
        Controls.Add(grpClick);

        // ---- 快捷键说明 ----
        var grpKeys = new GroupBox
        {
            Text = "快捷键",
            Location = new Point(12, 342),
            Size = new Size(532, 346),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        _lblKeys = new TextBox
        {
            Location = new Point(22, 26),
            Size = new Size(488, 306),
            Text = BuildKeyHelp(),
            Multiline = true,
            ReadOnly = true,
            WordWrap = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical,
            TabStop = false,
            BackColor = SystemColors.Control,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        grpKeys.Controls.Add(_lblKeys);
        Controls.Add(grpKeys);

        // ---- 底部按钮 ----
        var btnSettings = new Button
        {
            Text = "设置",
            Location = new Point(12, 700),
            Size = new Size(96, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };
        btnSettings.Click += (_, _) => OpenSettings();
        Controls.Add(btnSettings);

        var btnAbout = new Button
        {
            Text = "关于",
            Location = new Point(116, 700),
            Size = new Size(96, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };
        btnAbout.Click += (_, _) => OpenAbout();
        Controls.Add(btnAbout);

        // ---- 坐标刷新定时器 ----
        _posTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _posTimer.Tick += (_, _) => UpdateCoords();
        _posTimer.Start();

        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private static NumericUpDown AddStepRow(GroupBox parent, int col, string label, int value)
    {
        int x = 28 + col * 130;
        parent.Controls.Add(new Label { Text = label, Location = new Point(x, 18), AutoSize = true });
        var nud = new NumericUpDown
        {
            Location = new Point(x, 40),
            Size = new Size(95, 25),
            Minimum = 1,
            Maximum = 400,
            Value = Math.Clamp(value, 1, 400),
        };
        parent.Controls.Add(nud);
        return nud;
    }

    private string BuildKeyHelp()
    {
        var hk = _settings.Hotkeys;
        return
            "方向键移动（按住连续移动，步长见上方设置）\r\n" +
            Fmt(hk.MoveLarge.Display(true), "大幅") +
            Fmt(hk.MoveMedium.Display(true), "中幅") +
            Fmt(hk.MoveSmall.Display(true), "小幅") +
            Fmt(hk.MoveTiny.Display(true), "微小") +
            "鼠标点击\r\n" +
            Fmt(hk.ClickLeft.Display(false), "左键") +
            Fmt(hk.ClickMiddle.Display(false), "中键") +
            Fmt(hk.ClickRight.Display(false), "右键") +
            "连点器\r\n" +
            Fmt(hk.ClickerToggle.Display(false), "启动/停止（也可点上方按钮）") +
            "其他\r\n" +
            Fmt(hk.ShowMain.Display(false), "呼出主界面") +
            Fmt(hk.Quit.Display(false), "退出程序") +
            Fmt("", "关闭窗口会最小化到托盘") +
            "\r\n以上快捷键均可在「设置」中修改，界面字体也可更换。\r\n" +
            "注：Alt+左右键在浏览器里是前进/后退，运行期间会被本软件用作移动。";
    }

    private static string Fmt(string key, string desc) => "  " + key.PadRight(24) + desc + "\r\n";

    // ================= 字体 =================

    private void ApplyFonts()
    {
        float size = Math.Clamp(_settings.FontSize, 8F, 14F);
        Font uiFont;
        try
        {
            uiFont = new Font(_settings.FontFamily, size);
            if (!uiFont.Name.Equals(_settings.FontFamily, StringComparison.OrdinalIgnoreCase))
                uiFont = new Font("Microsoft YaHei UI", size);
        }
        catch
        {
            uiFont = new Font("Microsoft YaHei UI", size);
        }

        SuspendLayout();
        Font = uiFont;
        foreach (Control c in Controls)
            ApplyFontRecursive(c, uiFont);
        _lblCoords.Font = new Font("Consolas", size + 11F, FontStyle.Bold);
        _lblKeys.Font = new Font("Consolas", size + 1F);
        ResumeLayout();
    }

    private void ApplyFontRecursive(Control c, Font f)
    {
        if (c != _lblCoords && c != _lblKeys)
            c.Font = f;
        foreach (Control child in c.Controls)
            ApplyFontRecursive(child, f);
    }

    // ================= 生命周期 =================

    private void OnLoad(object? sender, EventArgs e)
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "MouseController 鼠标控制器",
            Visible = true,
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (_, _) => ShowMain());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMain();

        if (!_hook.Install())
        {
            MessageBox.Show(
                "键盘钩子安装失败，全局快捷键将不可用。\r\n" +
                "（可能是杀毒软件拦截，请将本程序加入白名单）",
                "MouseController", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        _hook.ComboDown += OnComboDown;
    }

    // ================= 设置 / 关于 =================

    private void OpenSettings()
    {
        using var dlg = new SettingsForm(_settings, this);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _lblKeys.Text = BuildKeyHelp();
            ApplyFonts();
            SaveSettings();
        }
    }

    private void OpenAbout()
    {
        using var dlg = new AboutForm();
        dlg.ShowDialog(this);
    }

    /// <summary>进入快捷键捕获模式：所有组合键交给回调，不再触发鼠标动作。</summary>
    public void BeginCapture(Action<HotkeyInfo> handler) => _captureHandler = handler;

    public void EndCapture() => _captureHandler = null;

    // ================= 窗口消息 =================

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_SHOWMAIN)
        {
            ShowMain();
            return;
        }
        base.WndProc(ref m);
    }

    // ================= 组合键分发（键盘钩子回调，UI 线程） =================

    private bool OnComboDown(HotkeyInfo h)
    {
        // 设置界面正在捕获新快捷键
        if (_captureHandler != null)
        {
            if (IsModifierVk(h.Vk)) return true; // 纯修饰键，继续等待
            var cb = _captureHandler;
            cb(h);
            return true;
        }

        var hk = _settings.Hotkeys;

        // 方向键移动（四档，仅比较修饰键）
        if (h.Vk is Native.VK_UP or Native.VK_DOWN or Native.VK_LEFT or Native.VK_RIGHT)
        {
            if (hk.MoveLarge.Matches(h)) { MoveCursorBy(h, StepLarge); return true; }
            if (hk.MoveMedium.Matches(h)) { MoveCursorBy(h, StepMedium); return true; }
            if (hk.MoveSmall.Matches(h)) { MoveCursorBy(h, StepSmall); return true; }
            if (hk.MoveTiny.Matches(h)) { MoveCursorBy(h, StepTiny); return true; }
            return false;
        }

        if (hk.ClickLeft.Matches(h)) { SimulateClick(ClickButton.Left); return true; }
        if (hk.ClickMiddle.Matches(h)) { SimulateClick(ClickButton.Middle); return true; }
        if (hk.ClickRight.Matches(h)) { SimulateClick(ClickButton.Right); return true; }
        if (hk.ClickerToggle.Matches(h)) { ToggleClicker(); return true; }
        if (hk.ShowMain.Matches(h)) { ShowMain(); return true; }
        if (hk.Quit.Matches(h)) { ExitApp(); return true; }

        return false;
    }

    private static bool IsModifierVk(uint vk) => vk switch
    {
        0x10 or 0x11 or 0x12 or 0x5B or 0x5C          // Shift / Ctrl / Alt / Win
        or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 => true,
        _ => false,
    };

    private void MoveCursorBy(HotkeyInfo h, int step)
    {
        int dx = 0, dy = 0;
        switch (h.Vk)
        {
            case Native.VK_UP: dy = -step; break;
            case Native.VK_DOWN: dy = step; break;
            case Native.VK_LEFT: dx = -step; break;
            case Native.VK_RIGHT: dx = step; break;
        }
        Native.GetCursorPos(out var p);
        Native.SetCursorPos(p.X + dx, p.Y + dy);
    }

    private void SimulateClick(ClickButton button)
    {
        (uint down, uint up) = button switch
        {
            ClickButton.Left => (Native.MOUSEEVENTF_LEFTDOWN, Native.MOUSEEVENTF_LEFTUP),
            ClickButton.Middle => (Native.MOUSEEVENTF_MIDDLEDOWN, Native.MOUSEEVENTF_MIDDLEUP),
            _ => (Native.MOUSEEVENTF_RIGHTDOWN, Native.MOUSEEVENTF_RIGHTUP),
        };
        Native.ClickMouse(down, up);
    }

    private void ShowMain()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void ExitApp()
    {
        _exitRequested = true;
        _clicker.Stop();
        _hook.Dispose();
        _trayIcon?.Dispose();
        Close();
    }

    // ================= 连点器 =================

    private void ToggleClicker()
    {
        if (_clicker.IsRunning)
        {
            _clicker.Stop();
            _lblClickStatus.Text = "已停止";
            return;
        }

        var button = (ClickButton)_cmbButton.SelectedIndex;
        int count = (int)_nudCount.Value;
        int interval = (int)_nudInterval.Value;

        _btnClick.Text = "停止";
        SetClickControlsEnabled(false);
        _lblClickStatus.Text = $"正在点击：0 / {count}";
        _ = _clicker.RunAsync(button, count, interval);
    }

    private void OnClickerFinished()
    {
        SafeUi(() =>
        {
            _btnClick.Text = "开始连点";
            SetClickControlsEnabled(true);
            if (_lblClickStatus.Text.StartsWith("正在点击"))
                _lblClickStatus.Text = $"完成 {_nudCount.Value} 次";
        });
    }

    private void SetClickControlsEnabled(bool enabled)
    {
        _cmbButton.Enabled = enabled;
        _nudCount.Enabled = enabled;
        _nudInterval.Enabled = enabled;
    }

    // ================= 窗口生命周期 =================

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settings.MoveLarge = StepLarge;
        _settings.MoveMedium = StepMedium;
        _settings.MoveSmall = StepSmall;
        _settings.MoveTiny = StepTiny;
        _settings.ClickCount = (int)_nudCount.Value;
        _settings.ClickIntervalMs = (int)_nudInterval.Value;
        _settings.ClickButton = _cmbButton.SelectedIndex;
        _settings.Save();
    }

    private void UpdateCoords()
    {
        Native.GetCursorPos(out var p);
        _lblCoords.Text = $"X: {p.X}    Y: {p.Y}";
    }

    private void SafeUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // 窗口已关闭
        }
    }
}
