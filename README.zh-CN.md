# ShikiPad

Windows 原生手柄键鼠映射工具。支持 DualSense、DualShock 4、Xbox 360、Xbox Series X|S，把手柄转换成键盘、鼠标、滚轮和常用组合输入。

English documentation: [README.md](README.md)

## 发布包

正式压缩包只包含：

| 文件 | 用途 |
|---|---|
| `ShikiPad.exe` | 主程序 |
| `install_driver.bat` | Interception 驱动安装脚本 |
| `interception.dll` | Interception 运行库 |
| `driver/install-interception.exe` | Interception 驱动安装器 |
| `README.md` / `README.zh-CN.md` | 说明文档 |

运行后可能生成 `shikipad.default`，只记录默认启动手柄型号，不属于发布包。

## 安装

1. 解压发布包。
2. 右键 `install_driver.bat`，以管理员身份运行。
3. 看到 `Installation complete!` 后重启 Windows。
4. 运行 `ShikiPad.exe`。程序需要管理员权限，因为键鼠注入依赖 Interception。
5. 首次运行时输入 `1` 到 `8` 选择手柄型号，可保存为默认启动。

## 页面按键

| 页面 | Enter | Esc |
|---|---|---|
| 选择手柄 | 确认选择 | 退出 |
| 主界面 | 打开映射说明 | 返回选择手柄 |
| 映射说明 | 返回主界面 | 关闭软件 |

关闭控制台窗口也会退出，并释放保持中的键盘和鼠标按键。

## 鼠标与系统键

| 手柄输入 | 输出 |
|---|---|
| 右摇杆 | 鼠标移动 |
| L3 | 鼠标左键 |
| R3 | 鼠标右键，按下瞬间短暂冻结光标 |
| 左摇杆上 / 下 | 鼠标滚轮 |
| Sony Create / Share | `Right Alt` |
| Sony Options / Menu | `Right Ctrl` |
| Sony Home | `Right Shift` |
| DualSense 静音键 | `Caps Lock` |
| Share/Create/View + Options/Menu 长按约 1 秒 | 启用 / 禁用 ShikiPad |

## 左摇杆

| 方向 | 输出 |
|---|---|
| 左 | `Shift` |
| 左下 | `Ctrl` |
| 右下 | `Left Alt` |
| 右 | `Win` |
| 左上 | `Esc` |
| 右上 | Fn 层 |
| 上 / 下 | 鼠标滚轮 |

Fn 层会把数字行转换为 `F1` 到 `F12`：`1..0` 对应 `F1..F10`，`-` 对应 `F11`，`=` 对应 `F12`。

## 蓄力

默认左摇杆同一时间只保持一个修饰键。蓄力可收集多个修饰键，并在解除后统一释放。

| 手柄 | 开启 / 保持 |
|---|---|
| DualSense / DualShock 4 | 触控板短按切换，长按保持 |
| Xbox | View/Back 或 Menu/Start 短按切换，长按保持 |

## 打字键层

下表列序为：`↑`、`→`、`□/X`、`△/Y`、`←`、`↓`、`×/A`、`○/B`。

| 键层 | ↑ | → | □/X | △/Y | ← | ↓ | ×/A | ○/B |
|---|---|---|---|---|---|---|---|---|
| 基础层 | ↑ | → | Space | Backspace | ← | ↓ | Enter | Tab |
| R1 / RB | i | n | e | l | o | t | h | u |
| L1 / LB | w | d | c | r | a | s | y | z |
| R2 / RT | m | g | j | x | q | f | p | b |
| L2 / LT | k | v | 1 | 2 | 3 | 4 | 5 | 6 |
| R1 + L1 | 7 | 8 | 9 | 0 | - | = | , | . |
| L2 + R2 | ( | ) | : | " | [ | ] | ' | _ |
| L1 + R2 | { | } | ? | ! | # | * | / | ^ |
| L2 + R1 | ; | @ | + | % | & | $ | \ | ` |

程序发送的是物理键。需要 Shift 的按键（`<`, `>`, `|`, `~`）可通过按住左摇杆 `Shift` 并结合对应的基础键（`,`, `.`, `\`, `` ` ``）来输入。

基础层按住会连发。字符层是虚拟点按：按下一次只发送一次，按住不会连发。

## 常见问题

### 没有键鼠输出

确认已安装 Interception、重启 Windows，并以管理员权限运行 `ShikiPad.exe`。

### 游戏里双重输入

Sony 手柄可用 HidHide 隐藏物理手柄，并把 `ShikiPad.exe` 加入白名单。Xbox 手柄走 XInput，HidHide 通常不能在 HID 层隐藏它。

## 时序模型参数

ShikiPad 使用短时间窗口吸收人手快速滚动输入时的先后误差。

| 参数名 | 默认值 | 作用 |
|---|---:|---|
| `comboLayerWindowMs` | 45 ms | 双层组合允许的最大按下间隔 |
| `actionLayerGraceMs` | 35 ms | 动作键与键层的识别宽容窗口 |
| `actionLayerPostGraceMs` | 20 ms | 松开键层到按下新键层间的空窗期归属 |
| `layerTakeoverWindowMs` | 30 ms | 限制旧键层与新意图键层重叠的最长时间 |
| `actionLayerSwitchGuardMs` | 35 ms | 字符发出后切换键层时抑制残留误触 |
