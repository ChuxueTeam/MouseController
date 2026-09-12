using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace MouseController;

/// <summary>关于对话框：版本、开发者、网址、许可证。</summary>
public sealed class AboutForm : Form
{
    private const string HomeUrl = "https://github.com/BottleMan-SR7";
    private const string RepoUrl = "https://github.com/ChuxueTeam/MouseController";
    private const string LicenseUrl = "https://github.com/ChuxueTeam/MouseController/blob/main/LICENSE";

    public AboutForm()
    {
        Text = "关于 MouseController";
        ClientSize = new Size(432, 336);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9F);

        var pic = new PictureBox
        {
            Location = new Point(24, 22),
            Size = new Size(56, 56),
            Image = MakeIcon(),
            SizeMode = PictureBoxSizeMode.StretchImage,
        };
        Controls.Add(pic);

        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";
        Controls.Add(new Label
        {
            Text = "MouseController",
            Location = new Point(94, 20),
            Size = new Size(320, 32),
            Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
        });
        Controls.Add(new Label
        {
            Text = $"版本 v{version}",
            Location = new Point(97, 54),
            Size = new Size(300, 22),
            ForeColor = Color.DimGray,
        });

        Controls.Add(new Label
        {
            Text = "键盘驱动的全局鼠标控制与连点器\r\n单文件 · 免安装 · 全部使用 Windows 原生 API",
            Location = new Point(24, 94),
            Size = new Size(384, 44),
            ForeColor = Color.DimGray,
        });

        Controls.Add(Field("开发者", 150));
        Controls.Add(new Label
        {
            Text = "BottleMan-SR7",
            Location = new Point(104, 150),
            Size = new Size(300, 22),
        });

        Controls.Add(Field("网址", 180));
        Controls.Add(Link("https://github.com/BottleMan-SR7", 180, HomeUrl));

        Controls.Add(Field("项目主页", 210));
        Controls.Add(Link("github.com/ChuxueTeam/MouseController", 210, RepoUrl));

        Controls.Add(Field("许可证", 240));
        Controls.Add(Link("MIT License", 240, LicenseUrl, 120));

        Controls.Add(new Label
        {
            Text = "本软件以 MIT 许可证开源，可自由使用、修改与分发。",
            Location = new Point(224, 240),
            Size = new Size(184, 22),
            ForeColor = Color.DimGray,
        });

        var btnOk = new Button
        {
            Text = "关闭",
            Location = new Point(332, 288),
            Size = new Size(76, 30),
            DialogResult = DialogResult.OK,
        };
        Controls.Add(btnOk);
        AcceptButton = btnOk;
        CancelButton = btnOk;
    }

    private static Label Field(string text, int y) => new()
    {
        Text = text,
        Location = new Point(24, y),
        Size = new Size(74, 22),
        ForeColor = Color.DimGray,
    };

    private static LinkLabel Link(string text, int y, string url, int width = 300)
    {
        var link = new LinkLabel
        {
            Text = text,
            Location = new Point(104, y),
            Size = new Size(width, 22),
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
        var bmp = new Bitmap(56, 56);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, 55, 55);
        using (var path = RoundedRect(rect, 16))
        using (var brush = new LinearGradientBrush(rect, Color.FromArgb(76, 141, 255), Color.FromArgb(27, 95, 224), 45F))
        {
            g.FillPath(brush, path);
        }

        using var pen = new Pen(Color.White, 3.2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        using (var body = RoundedRect(new Rectangle(19, 11, 18, 34), 9))
        {
            g.DrawPath(pen, body);
        }
        g.DrawLine(pen, 28, 14, 28, 23);
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
