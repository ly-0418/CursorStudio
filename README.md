# CursorStudio · 光标工作室

> 一个真正管用的 Windows 鼠标指针方案切换器 + 大小调节器。免安装、免管理员、纯原生 .NET，下载解压就能用。

![界面预览](assets/preview.png)

<p>
<img alt="platform" src="https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6">
<img alt="license" src="https://img.shields.io/badge/license-MIT-green">
<img alt="runtime" src="https://img.shields.io/badge/.NET%20Framework-4.x-blueviolet">
<img alt="size" src="https://img.shields.io/badge/exe-472%20KB-lightgrey">
</p>

---

## 为什么需要它

Windows 11 的「设置 → 辅助功能 → 鼠标指针和触控 → 大小」滑块，对**第三方光标包**经常完全不起作用：注册表里的 `CursorSize`、`CursorBaseSize` 都写进去了，读回来也对，但屏幕上那只指针还是 32×32 原尺寸。

原因不是系统"没收到"，而是它**根本不看这两个值来选帧**：`GetCursorInfo → GetIconInfo → GetObject` 读回来恒是 32×32，说明系统一直在用 `.cur` 文件里最小的那一帧渲染。想让它变大，只有一条路——**自己把每个角色的光标按目标像素重建出来，再用 `SetSystemCursor` 逐个顶上去**。

CursorStudio 就是把这套流程做成图形界面的工具：换方案、调大小、看效果，一个窗口搞定。

## 功能

| 功能 | 说明 |
|---|---|
| **方案切换** | 列出系统内置方案 + `custom/` 下的第三方包，选中即预览、点「应用此方案」才真正替换 |
| **大小调节** | 1–15 档，`px = 32 + (档位-1) × 8`（32 / 40 / 48 … 144 px），档位实时预览；滑条为细轨道 + 蓝色渐变进度 + 带投影的圆形滑块，悬停/拖动时滑块放大并透出光晕 |
| **形态预览** | 21 种指针形态，直接解析 `.cur`/`.ani` 内部**最大原始帧**（通常 128×128）重采样，不是把 32px 小图拉大 |
| **界面小手跟随** | 按钮、下拉框、滑块上的手形光标取自当前方案自己的 `Hand.cur`，尺寸跟着档位实时变 |
| **指针不跳** | 启动、切方案、应用大小全程不移动指针；窗口里卡片与按钮用同一档位的箭头/小手，进出按钮不会有 32↔40 的尺寸跳变 |
| **窗口自由缩放** | 无边框界面自己造缩放：四边 + 四角都能拖，卡片、按钮、下拉框、滑块与预览网格随窗口实时重排，最小 660×720 |
| **macOS 红绿灯** | 右上角三色圆点：黄＝最小化、绿＝最大化/还原、红＝关闭（红在最右）；悬停图标是矢量笔画手绘、按圆点比例居中：＋ / － / 双框 / ✕ |
| **窗口动画** | 最大化/还原由自驱帧循环 + smoothstep 驱动（0.2–0.28 s 自适应，帧间隔均匀、无位移帧不重绘），中途再点立刻反向；黄点最小化有「收进任务栏」的缩图动画 |
| **拖边缩放** | 鼠标只记录位置、按帧推进重排，不再每个鼠标消息排一次版，窗口紧跟指针不积压 |
| **全自绘界面** | 圆角卡片 + 自绘按钮/滑块/下拉，下拉是像素级平滑滚动，不是系统 ListBox 的一格一格跳 |
| **高 DPI** | 内置 DPI 感知清单，125% / 150% 缩放下按真实分辨率绘制，文字不失焦 |
| **安全可逆** | 只写 `HKCU\Control Panel\Cursors`，不碰 `C:\Windows`，不要管理员；一键「恢复系统默认」回 Aero |
| **绿色** | 单个 exe，无安装程序、无服务、无开机自启；删掉文件夹即卸载 |

## 下载与使用

1. 到 [Releases](https://github.com/ly-0418/CursorStudio/releases) 下载 `CursorStudio-1.0.3-portable.zip`，**解压到任意目录**（不要直接在压缩包里双击运行）。
2. 双击 `CursorStudio.exe`。
3. 下拉框里选一个方案 → 预览区立刻换成它的 21 种形态 → 满意就点 **「应用此方案」**。
4. 拖大小滑块选档位 → 点 **「应用大小」**。界面上的「系统当前实际尺寸」是实测值，和档位对得上才说明真的生效。

> 应用完大小后，如果又去拖动 Windows 设置里的自带滑块，系统会重载方案把指针打回 32 px —— 回到本工具再点一次「应用大小」即可。

> `docs/使用说明.txt` 是随程序分发的完整中文说明，里面的 `D:\CursorStudio\` 是作者的本地安装示例路径，按你自己解压的目录理解即可。

## 自定义光标包

把任意光标包**整个文件夹**丢进 `custom/`，点「刷新列表」（或重启程序）就会自动出现在下拉框里，文件夹名就是方案名（`Bibata-Modern-Classic` → 「Bibata Modern Classic（自定义）」）。

- 一个子文件夹 = 一个方案，支持递归查找，`.cur` / `.ani` 都认。
- 也可以点「导入光标包」直接选位于别处的包，工具会复制进来并立即识别。
- 推荐两个热门系列（均遵循 GPL-3.0，请从作者官方 Release 下载，本仓库不直接分发这些素材）：
  - [Bibata Cursor](https://github.com/ful1e5/Bibata_Cursor/releases) —— 尖角星形设计，黑 / 冰蓝 / 琥珀三种描边
  - [BreezeX Cursor](https://github.com/ful1e5/BreezeX_Cursor/releases) —— KDE Breeze 风格，黑 / 浅灰
  解压后把里面的文件夹整个复制到 `custom/` 即可。

### 21 个角色的识别规则

工具按角色名 / 常见文件名匹配包内文件，顺序如下（括号内为注册表值名）：

| # | 界面名称 | 注册表值 | 常见文件名 |
|---|---|---|---|
| 1 | 正常选择 | `Arrow` | `Arrow` / `Pointer` / `Normal Select` / `Default` |
| 2 | 帮助选择 | `Help` | `Help` / `Help Select` |
| 3 | 后台运行 | `AppStarting` | `AppStarting` / `Working in Background` / `Work` |
| 4 | 忙碌 | `Wait` | `Wait` / `Busy` |
| 5 | 精确选择 | `Crosshair` | `Crosshair` / `Precision` / `Cross` |
| 6 | 文本选择 | `IBeam` | `IBeam` / `Text Select` / `Text` |
| 7 | 手写笔 | `NWPen` | `NWPen` / `Handwriting` / `Pen` |
| 8 | 不可用 | `No` | `No` / `Unavailable` / `Forbidden` |
| 9 | 垂直缩放 | `SizeNS` | `SizeNS` / `Vertical Resize` / `Vert` |
| 10 | 水平缩放 | `SizeWE` | `SizeWE` / `Horizontal Resize` / `Horz` |
| 11 | 对角缩放 1 | `SizeNWSE` | `SizeNWSE` / `Diagonal Resize 1` / `Dgn1` |
| 12 | 对角缩放 2 | `SizeNESW` | `SizeNESW` / `Diagonal Resize 2` / `Dgn2` |
| 13 | 移动 | `SizeAll` | `SizeAll` / `Move` / `All Scroll` |
| 14 | 备用选择 | `UpArrow` | `UpArrow` / `Alternate Select` / `Link` |
| 15 | 链接选择 | `Hand` | `Hand` / `Link Select` / `Pointing` / `Hand2` |
| 16 | 位置选择 | `Pin` | `Pin` / `Location Select` |
| 17 | 人员选择 | `Person` | `Person` / `Person Select` |
| 18 | 平移 | `Pan` | `Pan` / `Hand Open` |
| 19 | 抓取 | `Grabbing` | `Grabbing` / `Hand Closed` |
| 20 | 放大 | `Zoom-in` | `ZoomIn` / `Zoom-in` / `Magnify` |
| 21 | 缩小 | `Zoom-out` | `ZoomOut` / `Zoom-out` / `Reduce` |

> 不同作者命名习惯差异很大（Bibata 用 `Pointer.cur`/`Wait.ani`，BreezeX 用 `Normal Select.cur`），工具做了多别名匹配并对不上时按角色顺序兜底，所以绝大多数包都能正确识别。

## 从源码构建

只需要 .NET Framework 4.x 自带的编译器，**不需要装 Visual Studio**：

```powershell
git clone https://github.com/ly-0418/CursorStudio.git
cd CursorStudio
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

产物在 `dist\CursorStudio.exe`。也可以手写一行：

```bat
csc /nologo /codepage:65001 /target:winexe /platform:anycpu ^
    /win32manifest:src\CursorStudio.manifest /win32icon:assets\icon.ico ^
    /out:dist\CursorStudio.exe ^
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
    src\CursorCore.cs src\CursorUI.cs
```

源码两个文件，约 2300 行，无第三方依赖：

| 文件 | 职责 |
|---|---|
| `src/CursorCore.cs` | 方案枚举与注册表读写、`.cur`/`.ani` 解码器、按目标像素重建光标、`SetSystemCursor` |
| `src/CursorUI.cs` | 全自绘界面：主题、按钮、滑块、下拉列表、DPI 缩放、`MainForm` |
| `src/CursorStudio.manifest` | DPI 感知与 requestedExecutionLevel=asInvoker |

## 工作原理

**1. 方案从哪来**
三个来源合并：`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes`（系统内置）、`HKCU\...\Schemes`（用户自建）、`custom\` 下的文件夹（第三方包），统一成 `Scheme{Name, Paths[21], IsPack}`。

**2. 为什么"写注册表大小"没用**
系统只在 `SPI_SETCURSORS` 之后按 `CursorBaseSize` 重新加载**它自己**的尺寸，但实际渲染路径始终取 `.cur` 的最小帧。实测：把 128×128 的单帧 `.cur` 写进 `Arrow`，系统照样压回 32×32。结论是必须逐角色硬替换句柄。

**3. 真正生效的施加流程**
```
读该角色的 .cur 路径
  → 自己解析文件，取出最大原始帧（Bibata 通常 128×128）
  → 按目标像素（32 / 40 / … / 144）高质量重采样
  → CreateIconIndirect 构造 HICON
  → CopyIcon + SetSystemCursor(ocrx, ...) 顶到系统光标槽位
```
关键坑：**`SetSystemCursor` 之后绝对不能广播 `WM_SETTINGCHANGE`**，否则系统会立刻重载方案、把尺寸打回 32。这一点让第一版"写完就提示成功"变成假象，也是很多同类工具"看起来生效了但重启就回去"的原因。

21 个角色里只有 14 个有独立的系统光标 ID（`OCR_NORMAL`/`OCR_IBEAM`/`OCR_HAND` …）；手写笔、位置选择、人员选择、平移、抓取、放大、缩小这 7 项 Windows 没有独立槽位，只能跟随默认，属正常现象。

**4. 预览为什么清楚**
不调用任何系统截图 API（它们只返回 32×32），而是自己按 `.cur`/`.ani` 的结构解析：`ICONDIR` → 每帧的 `BITMAPINFOHEADER` → 32bpp DIB 像素（含 XOR/AND 掩码还原），再 `ScaleCursor` 到显示尺寸。这样 46px 的格子画的是 128px 的原图，边缘干净。

**5. 界面为什么清晰**
`CursorStudio.manifest` 里声明 `dpiAware=true / dpiAwareness=system`，程序启动按 `GetDpiForSystem()` 得到缩放系数 `K`，所有字号用 `Theme.F(size*K)`、所有尺寸用 `Theme.Px(v*K)`，并手写 `ScaleTree()` 递归缩放控件树。
（WinForms 自带的 `AutoScaleMode.Dpi + PerformAutoScale()` 在这个全自绘场景里实测完全无效，客户区不变、子控件不变，只能手写。）

**6. 界面里的小手**
不使用 `Cursors.Hand`（它取的是进程启动时的系统句柄，换方案后不会变），而是读 `HKCU\Control Panel\Cursors\Hand` 的路径，按当前档位像素重建，再递归赋给所有自绘控件；缓存按 px 索引，拖动滑块的那一刻就重建，所以手形大小和桌面系统光标始终一致。

## 兼容性与已知限制

- **系统**：Windows 10 / 11（x64 与 ARM64 走 x64 模拟均可）。
- **需要** .NET Framework 4.x（Win10 1511 之后系统自带，无需另装）。
- **UWP 应用与全屏游戏**：部分程序使用私有光标或自己接管渲染，不会跟随系统光标，这是程序行为不是工具问题。
- **无代码签名**：exe 未签名，首次运行可能出现 SmartScreen「Windows 已保护你的电脑」，点"更多信息 → 仍要运行"即可。
- 方案与档位写入 `HKCU`，重启后由系统自行恢复；若重启后指针变小，打开工具点一次「应用大小」。

## 常见问题

<details>
<summary>点了「应用大小」但指针没变</summary>

先看状态栏下的「系统当前实际尺寸」。它是原地读取当前系统光标句柄（`GetCursorInfo + GetIconInfo + GetObject`）得到的真实像素，不是估算值：数字变了就是生效，没变才是失败。若没变，点「重启资源管理器」再看。
</details>

<details>
<summary>某些窗口里指针还是小的</summary>

那些窗口（尤其 Electron 应用、部分浏览器全屏、游戏）可能自己维护了一套光标资源。点「重启资源管理器」让系统重发一次光标消息通常能刷新。
</details>

<details>
<summary>想换回 Windows 默认</summary>

点「恢复系统默认」= 切回 Windows Aero 白色指针。也可以直接双击事先导出的 `.reg` 备份。
</details>

<details>
<summary>放进 custom 的包不显示</summary>

确认文件夹里确实有 `.cur` / `.ani`（不是只有 `.png`/`.svg`），且没有把文件夹套了两层以上；点「刷新列表」重新扫描。
</details>

## 更新记录

### v1.0.3 · 2026-11-09

- **红绿灯加大、图标收出边距**：圆点从 16 px 加到 20 px（125% 下约 25 px），间距 9 → 11 px。悬停图标改成按圆点直径 43% 手绘的矢量笔画，四周留约 28% 的空，不再贴着圆点外沿；叉号的对角线最占地方、双框的笔画落在包围盒最外沿，这两个单独再收一点，四个图标的视觉重量才一致。最大化状态下的绿点也从「－」换成双框还原图标，不再和黄色最小化撞脸。
- **指针大小滑块重做**：6 px 细轨道加上深下浅的浅槽感，已选段改成深→浅的蓝色渐变，圆形滑块 19 px 带两层柔和投影；悬停或拖动时放大到 23 px、透出淡蓝光晕、描边转主色。顺带修掉滑块背后那块多余的灰底——它继承的是页面底色而不是卡片白。
- **最大化/还原动画改自驱帧**：不再挂在 15 ms 定时器上（那样每帧都要「等定时器 + 干活」，实测有 60 ms 空档）。现在按真实时钟一帧接一帧，帧间隔均匀，尺寸没变化的帧直接跳过重绘；时长 0.24–0.34 s，动画没播完再点绿点会从当前尺寸立刻反向。
- **黄点收进任务栏**：最小化不再是「啪」一下消失——界面先拍成快照，窗口一边缩向任务栏一边把快照等比压小，收到位才真正最小化；从任务栏点回来尺寸位置不变。
- **拖边缩放按帧推进**：原来每个鼠标消息都要同步重排一次（一秒最多上百个），必然积压、越拖越滞后。现在鼠标只负责记录最新位置，重排每帧一次，窗口紧跟指针，松手那一帧也会补上。
- 便携包重新打包，`docs/使用说明.txt` 同步上述改动。

### v1.0.2 · 2026-11-09

- **窗口能自由改大小了**：无边框窗口既没有系统标题栏也没有可拖的边框，尺寸原本是锁死的。现在由界面自己接管缩放：指针移到窗口四边或四角约 7 px 内会变成双向箭头，按住拖动即可改大小，八个方向全支持，最小 660×720（按 DPI 换算）。缩放时布局实时重排 —— 下拉框与滑块自动伸缩，「应用此方案」「应用大小」「打开自定义文件夹」始终贴右，预览网格按宽度自动换列（宽屏 6–7 列、窄窗 4 列），不挤压、不重叠。
- **右上角换成 macOS 红绿灯**：左→右依次 黄 / 绿 / 红。黄＝最小化到任务栏；绿＝最大化、再点一次＝还原；红＝关闭程序，放在最右端。悬停时圆点上浮出 ＋ / － / ✕ 字形。
- `docs/使用说明.txt` 新增「窗口大小」「右上角按钮」两节，并修正「不生效怎么办」里关于测量方式的旧描述。

### v1.0.1 · 2026-11-09

- **修掉指针乱跳**：早期版本为了测「系统当前实际尺寸」，会把指针临时挪到窗口底部空白条，等 220 ms 再挪回原处，于是双击启动、点「应用此方案」、点「应用大小」时指针看起来在屏幕上跳来跳去。现在改成原地读取当前光标句柄的尺寸，全程不移动指针。
- **修掉进出按钮的尺寸闪烁**：窗口里卡片、标题、说明文字这些非按钮区域，原本用的是进程启动时那个 32 px 系统箭头，指针从空白区移进按钮会 32→40、移出又 40→32。现在整块界面（含下拉弹层）统一换成当前方案、当前档位的箭头，和按钮上的小手同尺寸，只变形状不变大小。
- 点「应用大小」不再多余重建光标缓存，切换瞬间少一次重绘。
- 便携包重新打包，`docs/使用说明.txt` 补充上述两条。

### v1.0.0 · 首次发布

- 方案切换（系统内置 + `custom/` 第三方包）、1–15 档大小调节、21 种形态高清预览、全自绘界面 + DPI 感知。

## 致谢

- [Bibata Cursor](https://github.com/ful1e5/Bibata_Cursor) 与 [BreezeX Cursor](https://github.com/ful1e5/BreezeX_Cursor)，作者 [ful1e5](https://github.com/ful1e5)，GPL-3.0。本项目只在其官方 Release 页面提供下载指引，不随仓库分发这些素材。
- Windows 光标格式（`ICONDIR` / `CUR` / `ANI`) 的结构参考了 MSDN 与开源社区的解析实现。

## License

[MIT](LICENSE) © 2026 ly-0418

---

### English summary

**CursorStudio** is a portable Windows 11 cursor-scheme switcher and size adjuster. Windows' built-in "cursor size" slider is silently ignored for most third-party cursor packs — the system keeps rendering the 32×32 frame stored inside the `.cur`. This tool parses the largest frame of each `.cur`/`.ani`, resamples it to the exact pixel size you pick (32–144 px), rebuilds the handle with `CreateIconIndirect`, and applies it role-by-role with `SetSystemCursor`. No admin rights, no files written under `C:\Windows`, everything lives in `HKCU\Control Panel\Cursors`. Drop any cursor pack into `custom/` and it shows up in the dropdown automatically.
