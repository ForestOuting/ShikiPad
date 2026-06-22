# ShikiPad

ShikiPad 是一款 Windows 原生手柄键鼠映射工具。它把 DualSense、DualShock 4、Xbox 360、Xbox Series X|S 手柄转换成键盘、鼠标、滚轮和多重快捷键，让手柄也能承担桌面操作和高频文字输入。

English documentation: [README.md](README.md)

## 适合谁

- 想用手柄完成鼠标移动、点击、滚轮和快捷键操作的人。
- 想在沙发、床边、掌机、电视或远程桌面场景中用手柄输入文字的人。
- 想把手柄当作稳定的键鼠层，而不是临时玩具映射的人。

## 发布包内容

正式发布压缩包只应包含这些文件：

| 文件 | 用途 |
|---|---|
| `ShikiPad.exe` | 主程序 |
| `install_driver.bat` | Interception 驱动安装脚本，需要管理员权限 |
| `interception.dll` | ShikiPad 使用的 Interception 运行库 |
| `driver/install-interception.exe` | Interception 驱动安装器 |
| `README.md` / `README.zh-CN.md` | 英文和中文说明文档 |

运行后，ShikiPad 可能会在同目录创建 `shikipad.default`。它只记录默认启动手柄型号，不保存映射参数，不属于发布包内容。

## 安装与首次运行

1. 将发布压缩包解压到普通文件夹。
2. 右键 `install_driver.bat`，选择“以管理员身份运行”。
3. 等待出现 `Installation complete!`，然后重启 Windows。驱动必须重启后才会生效。
4. 运行 `ShikiPad.exe`。由于底层输入注入需要权限，程序会请求管理员权限。
5. 在控制台选择手柄型号。选择后可保存为默认启动，之后直接运行即可自动进入。

## 界面说明

ShikiPad 的控制台界面分为五类页面。

| 页面 | 何时出现 | 你需要做什么 |
|---|---|---|
| 启动页 | 每次打开程序后 | 查看提示，等待进入手柄选择或默认启动 |
| 手柄选择页 | 没有默认手柄，或你主动重新选择时 | 输入 `1` 到 `8`，按 `Enter` 确认 |
| 默认启动页 | 已保存默认手柄时 | 等待约 1.8 秒自动启动；按 `Enter` 重新选择手柄 |
| 欢迎主界面 | 手柄连接成功后 | 按 `Enter` 打开映射说明；按 `Esc` 清除默认并回到手柄选择；关闭窗口退出 |
| 映射说明页 | 运行中按 `Enter` 后 | 再按 `Enter` 返回主界面 |

关闭 ShikiPad 控制台窗口即可退出。程序会自动释放所有保持中的键盘和鼠标按键。

## 手柄型号

| 编号 | 手柄型号 | 连接方式 |
|:---:|---|---|
| 1 | DualSense (PS5) | USB |
| 2 | DualSense (PS5) | 蓝牙 |
| 3 | DualShock 4 (PS4) | USB |
| 4 | DualShock 4 (PS4) | 蓝牙 |
| 5 | Xbox 360 | USB |
| 6 | Xbox 360 | 蓝牙 |
| 7 | Xbox Series X\|S | USB |
| 8 | Xbox Series X\|S | 蓝牙 |

## 默认启动

首次选择手柄后，提示 `将「...」设为默认启动？` 时直接按 `Enter` 或输入 `Y`，ShikiPad 会记住该手柄型号。

已有默认手柄时，启动页会短暂停留约 1.8 秒后自动继续。如果需要管理默认启动：

- 在默认启动页按 `Enter`：进入手柄选择，输入 `1` 到 `8` 可本次改用对应手柄，并可重新保存为默认。
- 在连接后的欢迎主界面按 `Esc`：清除默认启动，并立即回到非默认的手柄选择流程；之后每次启动都会重新显示手柄选择。

命令行也可以管理启动方式：

| 命令 | 用途 |
|---|---|
| `ShikiPad.exe --controller ds5` | 仅本次直接使用指定手柄，不改默认启动 |
| `ShikiPad.exe --controller-menu` | 强制显示手柄选择 |
| `ShikiPad.exe --clear-default-controller` | 清除默认启动并显示手柄选择 |
| `ShikiPad.exe --identity` | 输出应加入 HidHide 的精确程序路径 |
| `ShikiPad.exe --list-devices` | 枚举 HID 设备 |

可用别名包括 `ds5`、`ds5bt`、`ds4`、`ds4bt`、`xbox360`、`xbox360bt`、`xboxseries`、`xboxseriesbt`。

## 快捷禁用

按住 `Share/Create/View` 与 `Options/Menu` 约 1 秒可切换 ShikiPad 启用或禁用。临时希望手柄作为普通手柄透传时，可以使用这个开关；再次按住同一组合约 1 秒即可恢复 ShikiPad 映射。

## 鼠标与系统键

| 手柄输入 | 电脑输出 | 说明 |
|---|---|---|
| 右摇杆 | 鼠标移动 | 从微调到高速移动的连续幂函数曲线 |
| R3 | 鼠标右键 | 按下瞬间短暂冻结光标，减少点击漂移 |
| L3 | 鼠标左键 | 按住即保持鼠标左键 |
| 左摇杆上/下 | 鼠标滚轮上/下 | 推得越深，滚动越快 |
| PS 分享键 / Create | `Right Alt` | 仅 Sony 手柄 |
| PS 设置键 / Options | `Right Ctrl` | 仅 Sony 手柄 |
| PS Home | `Right Shift` | 仅 Sony 手柄；Xbox Home 会被 Windows 拦截 |

右摇杆逻辑是 ShikiPad 的核心手感之一：死区、指数曲线、最大速度和灵敏度共同决定移动表现。当前正式版保持固定的幂曲线执行路径。

## 左摇杆功能

左摇杆相当于一个 8 方向功能盘。

| 方向 | 输出 |
|---|---|
| 左 | `Shift` |
| 左下 | `Ctrl` |
| 右下 | `Left Alt` |
| 右 | `Win` |
| 左上 | `Esc` |
| 右上 | Fn 层 |
| 上 / 下 | 鼠标滚轮 |

Fn 层激活时，数字行会变成 `F1` 到 `F12`：`1` 到 `0` 对应 `F1` 到 `F10`，`-` 对应 `F11`，`=` 对应 `F12`。

## 蓄力系统

默认情况下，左摇杆同一时间只保持一个修饰方向。蓄力系统可以依次收集多个修饰键，并在解除蓄力时统一释放。

| 手柄 | 开启蓄力 | 解除蓄力 |
|---|---|---|
| DualSense / DualShock 4 | 短按触控板切换，或长按触控板保持 | 再短按一次，或长按后松开 |
| Xbox | 短按 View/Back 或 Menu/Start 切换，或长按任意一个保持 | 再短按一次，或长按后松开 |

示例：开启蓄力后，左摇杆先推左收集 `Shift`，再推左下收集 `Ctrl`，再推左上收集 `Esc`，系统就会收到 `Ctrl + Shift + Esc`。解除蓄力后，所有收集到的键会一起释放。

## 打字键层

下表按键顺序为：`上`、`右`、`方块/X`、`三角/Y`、`左`、`下`、`叉/A`、`圆/B`。

| 按住的键层 | 上 | 右 | 方块/X | 三角/Y | 左 | 下 | 叉/A | 圆/B |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 基础层 | 上方向键 | 右方向键 | 空格 | 退格 | 左方向键 | 下方向键 | 回车 | Tab |
| R1 / RB | i | n | e | a | o | t | h | u |
| L1 / LB | s | r | d | g | l | c | y | z |
| R2 / RT | m | w | j | x | q | f | p | b |
| L2 / LT | k | v | 1 | 2 | 3 | 4 | 5 | 6 |
| R1 + L1 | 7 | 8 | 9 | 0 | - | = | , | . |
| R2 + L2 | `<` | `)` | `[` | `{` | `(` | `>` | `}` | `]` |
| L1 + R2 | `` ` `` | `\` | `'` | `"` | `;` | `~` | `/` | `?` |
| R1 + L2 | `!` | `@` | `#` | `$` | `%` | `^` | `&` | `*` |

基础层按住会连发。字符层是虚拟点按：按下一次只发送一个字符，按住不会连发。输入大写字母或少数 Shift 标点时，先用左摇杆左方向保持 `Shift`，再按目标键。

语音输入快捷键：左摇杆保持 `Win`，再按 `R1 + 方块/X`，即可发送 `Win + H`。

## 时序模型

ShikiPad 使用短时间窗口吸收人手快速滚动输入时的先后误差。

| 设置 | 默认值 | 作用 |
|---|---:|---|
| `comboLayerWindowMs` | 35 ms | R1+L1 等双层组合允许的最大按下间隔 |
| `actionLayerGraceMs` | 35 ms | 动作键先按下时，允许紧随其后的键层接管该动作键 |
| `actionLayerPostGraceMs` | 25 ms | 一个键层松开后、另一个键层按下前的短空窗归属 |
| `layerTakeoverWindowMs` | 30 ms | 限制旧键层与新意图键层重叠的最长时间 |
| `actionLayerSwitchGuardMs` | 35 ms | 字符已经发出后，切换键层时抑制残留误触 |

这些参数都已经编译进当前版本。它们不是装饰性文案，而是直接对应代码中的实际行为。

## 内置默认参数

| 分类 | 设置 | 默认值 |
|---|---|---:|
| 运行 | `enabled` | `true` |
| 注入 | `useScanCode` | `true` |
| 注入 | `useInterception` | `true` |
| 鼠标 | `mouseMaxSpeed` | `20.0` |
| 鼠标 | `mouseSensitivity` | `1.0` |
| 鼠标 | `rightStickDeadzone` | `0.03` |
| 鼠标 | `rightStickCurveExponent` | `3.0` |
| 鼠标 | `r3FreezeMs` | `60` |
| 左摇杆 | `leftStickEnterDeadzone` | `0.35` |
| 左摇杆 | `leftStickExitDeadzone` | `0.25` |
| 扳机 | `triggerPressThreshold` | `0.0` |
| 扳机 | `triggerReleaseThreshold` | `0.0` |
| 连发 | `repeatDelayMs` | `300` |
| 连发 | `repeatIntervalMs` | `18` |
| 连发 | `baseRepeatSlowIntervalMs` | `120` |
| 连发 | `baseRepeatRampMs` | `1000` |
| 滚轮 | `scrollSlowIntervalMs` | `180` |
| 滚轮 | `scrollFastIntervalMs` | `18` |
| 滚轮 | `mouseScrollCurveExponent` | `3.0` |
| 时序 | `comboLayerWindowMs` | `35` |
| 时序 | `actionLayerGraceMs` | `35` |
| 时序 | `actionLayerPostGraceMs` | `25` |
| 时序 | `layerTakeoverWindowMs` | `30` |
| 时序 | `actionLayerSwitchGuardMs` | `35` |
| 蓄力 | `clutchLongPressMs` | `250` |

## 常见问题

### 没有任何键鼠输出

先确认已安装驱动并重启 Windows，然后以管理员权限运行 ShikiPad。如果 Interception 不可用，程序会回退到 `SendInput`，但在虚拟机和部分游戏场景中仍推荐使用驱动模式。

### 游戏里出现双重输入

Sony 手柄可以使用 [HidHide](https://github.com/nefarius/HidHide/releases) 隐藏物理手柄，并把精确的 `ShikiPad.exe` 路径加入 HidHide 应用程序列表。

Xbox 手柄使用 XInput，HidHide 无法在 HID 设备层隐藏它。如果必须隐藏手柄，建议使用可切换到 PS 或 DirectInput 模式的第三方手柄。

### 日志位置

运行日志位于 `ShikiPad.exe` 同目录下的 `logs/shikipad.log`。
