# MouseController

键盘驱动的全局鼠标控制与连点器小工具。单文件发布，无第三方库依赖（键盘钩子 + SendInput 全走 Windows 原生 API）。

当前版本：**v1.0.1**

## 功能

- 全局快捷键移动鼠标，四档步长可调（按住连续移动）
- 全局快捷键模拟点击（左 / 中 / 右键）
- 连点器：次数、间隔、键位可设，一键启停
- 鼠标坐标实时显示、移动到指定坐标、取当前坐标
- **设置**：全部快捷键可自定义（带冲突检测），界面字体与字号可更换
- **关于**：版本、开发者、项目链接与许可证
- 托盘常驻，`Ctrl+M` 呼出主界面，关窗口即收进托盘
- 设置持久化（`%APPDATA%\MouseController\settings.json`）
- 单实例运行（重复启动会唤出已有实例主界面）

## 默认快捷键

| 功能 | 快捷键 |
| --- | --- |
| 大幅移动 | `Alt` + 方向键 |
| 中幅移动 | `Shift+Alt` + 方向键 |
| 小幅移动 | `Ctrl+Alt` + 方向键 |
| 微小移动 | `Ctrl+Shift+Alt` + 方向键 |
| 左键点击 | `Ctrl+Alt+1` |
| 中键点击 | `Ctrl+Alt+2` |
| 右键点击 | `Ctrl+Alt+3` |
| 连点器启动/停止 | `Ctrl+Alt+4` |
| 呼出主界面 | `Ctrl+M` |
| 退出程序 | `Ctrl+Alt+Q` |

以上均可在软件内的「设置」中修改：点「修改」后按下新组合键即可（要求至少含一个修饰键），重复或冲突的组合会被拒绝。移动类只改修饰键，方向键固定。

默认选择 Alt 系组合键是为了避开系统与输入法常用键（`Win+方向` 是窗口贴靠、`Ctrl+Space` 是输入法中英切换、`Shift+方向` 是选中文字等），冲突最小。

## 构建

需要 .NET 9 SDK：

```powershell
# 精简版（需系统装有 .NET 9 桌面运行时，约 190KB）
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 自包含版（塞入运行时，免依赖，约 46MB，适合 PE / 无运行时的机器）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

测试自检：`MouseController.exe --selftest`（验证键盘钩子是否可安装，不启动界面）。

## 使用注意

- 运行期间会按当前快捷键设置拦截对应组合键；默认会占用 `Alt+方向键`、`Ctrl+Alt+1~4`、`Ctrl+M` 等
- 浏览器前进/后退（`Alt+左右`）默认会被用作鼠标移动，可在设置中改掉
- 连点器打在鼠标当前所在位置，间隔 0 为无间隔极速连点
- .NET 9 要求 Windows 10 1607 及以上；自包含版可用于 WinPE 等无运行时的环境

## 许可

MIT License · © 2026 BottleMan-SR7
