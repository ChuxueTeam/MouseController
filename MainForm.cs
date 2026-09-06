namespace MouseController;

public sealed class MainForm : Form
{
    private readonly KeyboardHook _hook = new();
    private readonly AutoClicker _clicker = new();
    private readonly AppSettings _settings;

    private NotifyIcon? _trayIcon;
    private bool _exitRequested;

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
        _clicker.Progress += n => SafeUi(() => _lblClickStatus.Text = $"正在点击：{n} / {(int)_nudCount.Value}");
        _clicker.Finished += OnClickerFinished;
    }

    // ================= UI 构建 =================

    private void BuildUi()
    {
        Text = "MouseController · 鼠标控制器";
        ClientSize = new Size(560, 720);
        MinimumSize = new Size(584, 762);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

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
            Font = new Font("Consolas", 20F, FontStyle.Bold),
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
            Minimum = 0,   // 0 = 无间隔极速连点
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
            Size = new Size(532, 358),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        _lblKeys = new TextBox
        {
            Location = new Point(22, 26),
            Size = new Size(488, 318),
            Font = new Font("Consolas", 10F),
            Text = BuildKeyHelp(),
            Multiline = true,
            ReadOnly = true,
            WordWrap = true,            // 超宽自动折行，永不横向截断
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.None,
            TabStop = false,
            BackColor = SystemColors.Control,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        grpKeys.Controls.Add(_lblKeys);
        Controls.Add(grpKeys);

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

    private static string BuildKeyHelp() =>
        "方向键移动（按住连续移动，步长见上方设置）\r\n" +
        "  Alt+方向键               大幅\r\n" +
        "  Shift+Alt+方向键         中幅\r\n" +
        "  Ctrl+Alt+方向键          小幅\r\n" +
        "  Ctrl+Shift+Alt+方向键    微小\r\n" +
        "鼠标点击\r\n" +
        "  Ctrl+Alt+1 左键    Ctrl+Alt+2 中键    Ctrl+Alt+3 右键\r\n" +
        "连点器\r\n" +
        "  Ctrl+Alt+4 启动/停止（也可点上方按钮）\r\n" +
        "其他\r\n" +
        "  Ctrl+M 呼出主界面       Ctrl+Alt+Q 退出程序\r\n" +
        "  关闭窗口会最小化到托盘\r\n" +
        "注：Alt+左右键在浏览器里是前进/后退，运行期间会被本软件用作移动。\r\n" +
        "另：Ctrl+M 不与其它常用键冲突。";

    // ================= 生命周期 =================

    private void OnLoad(object? sender, EventArgs e)
    {
        // 创建托盘图标
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

        // 安装键盘钩子
        if (!_hook.Install())
        {
            MessageBox.Show(
                "键盘钩子安装失败，全局快捷键将不可用。\r\n" +
                "（可能是杀毒软件拦截，请将本程序加入白名单）",
                "MouseController", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        _hook.ComboDown += OnComboDown;
    }

    // ================= 窗口消息 =================

    /// <summary>再次运行本程序时系统发来的“显示主界面”消息（WM_SHOWMAIN）。</summary>
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
        if (h.Win) return false; // Win 系不处理（留给系统）

        switch (h.Vk)
        {
            case Native.VK_UP:
            case Native.VK_DOWN:
            case Native.VK_LEFT:
            case Native.VK_RIGHT:
                if (h.Alt && !h.Ctrl && !h.Shift)
                {
                    MoveCursorBy(h, StepLarge);
                    return true;
                }
                if (h.Alt && h.Shift && !h.Ctrl)
                {
                    MoveCursorBy(h, StepMedium);
                    return true;
                }
                if (h.Alt && h.Ctrl && !h.Shift)
                {
                    MoveCursorBy(h, StepSmall);
                    return true;
                }
                if (h.Alt && h.Ctrl && h.Shift)
                {
                    MoveCursorBy(h, StepTiny);
                    return true;
                }
                return false;

            case Native.VK_1:
                if (h.Ctrl && h.Alt && !h.Shift) { SimulateClick(ClickButton.Left); return true; }
                return false;
            case Native.VK_2:
                if (h.Ctrl && h.Alt && !h.Shift) { SimulateClick(ClickButton.Middle); return true; }
                return false;
            case Native.VK_3:
                if (h.Ctrl && h.Alt && !h.Shift) { SimulateClick(ClickButton.Right); return true; }
                return false;
            case Native.VK_4:
                if (h.Ctrl && h.Alt && !h.Shift) { ToggleClicker(); return true; }
                return false;

            case Native.VK_M:
                if (h.Ctrl && !h.Alt && !h.Shift) { ShowMain(); return true; }
                return false;

            case Native.VK_Q:
                if (h.Ctrl && h.Alt && !h.Shift) { ExitApp(); return true; }
                return false;
        }
        return false;
    }

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
            // 关窗口 = 藏到托盘
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
