using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace MouseController;

/// <summary>关于对话框：版本、开发者、网址、许可证。</summary>
public sealed class AboutForm : Form
{
    private const string HomeUrl = "https://github.com/ChuxueTeam";
    private const string RepoUrl = "https://github.com/ChuxueTeam/MouseController";
    private const string LicenseUrl = "https://github.com/ChuxueTeam/MouseController/blob/main/LICENSE";

    public AboutForm()
    {
        Text = "关于 MouseController";
        ClientSize = new Size(500, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9.5F);

        var pic = new PictureBox
        {
            Location = new Point(28, 26),
            Size = new Size(64, 64),
            Image = MakeIcon(),
            SizeMode = PictureBoxSizeMode.StretchImage,
        };
        Controls.Add(pic);

        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";
        Controls.Add(new Label
        {
            Text = "MouseController",
            Location = new Point(110, 26),
            Size = new Size(360, 38),
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
        });
        Controls.Add(new Label
        {
            Text = $"版本 v{version}",
            Location = new Point(113, 66),
            Size = new Size(340, 24),
            ForeColor = Color.DimGray,
        });

        Controls.Add(new Label
        {
            Text = "键盘驱动的全局鼠标控制与连点器\r\n单文件 · 免安装 · 全部使用 Windows 原生 API",
            Location = new Point(28, 112),
            Size = new Size(444, 52),
            ForeColor = Color.DimGray,
        });

        Controls.Add(Field("开发者", 180));
        Controls.Add(new Label
        {
            Text = "BottleMan-SR7",
            Location = new Point(122, 180),
            Size = new Size(340, 24),
        });

        Controls.Add(Field("网址", 216));
        Controls.Add(Link("https://github.com/ChuxueTeam", 216, HomeUrl));

        Controls.Add(Field("项目主页", 252));
        Controls.Add(Link("github.com/ChuxueTeam/MouseController", 252, RepoUrl));

        Controls.Add(Field("许可证", 288));
        Controls.Add(Link("MIT License", 288, LicenseUrl, 130));

        Controls.Add(new Label
        {
            Text = "本软件以 MIT 许可证开源，可自由使用、修改与分发。",
            Location = new Point(122, 322),
            Size = new Size(350, 24),
            ForeColor = Color.DimGray,
        });

        var btnOk = new Button
        {
            Text = "关闭",
            Location = new Point(394, 368),
            Size = new Size(84, 32),
            DialogResult = DialogResult.OK,
        };
        Controls.Add(btnOk);
        AcceptButton = btnOk;
        CancelButton = btnOk;
    }

    private static Label Field(string text, int y) => new()
    {
        Text = text,
        Location = new Point(28, y),
        Size = new Size(84, 24),
        ForeColor = Color.DimGray,
    };

    private static LinkLabel Link(string text, int y, string url, int width = 340)
    {
        var link = new LinkLabel
        {
            Text = text,
            Location = new Point(122, y),
            Size = new Size(width, 24),
        };
        link.LinkClicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // 打不开就算了
            }
        };
        return link;
    }

    private static Image MakeIcon()
    {
        var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, 63, 63);
        using (var path = RoundedRect(rect, 18))
        using (var brush = new LinearGradientBrush(rect, Color.FromArgb(76, 141, 255), Color.FromArgb(27, 95, 224), 45F))
        {
            g.FillPath(brush, path);
        }

        using var pen = new Pen(Color.White, 3.6F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        using (var body = RoundedRect(new Rectangle(22, 13, 20, 38), 10))
        {
            g.DrawPath(pen, body);
        }
        g.DrawLine(pen, 32, 16, 32, 26);
        return bmp;
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
