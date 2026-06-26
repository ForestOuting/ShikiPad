# ShikiPad

Windows 原生手柄键鼠映射工具. 推荐使用 PlayStation 手柄. DualSense 和 DualShock 4 可以使用 ShikiPad 的完整功能, Xbox 手柄通过 XInput 工作, 但会受到 Windows 系统行为限制.

English documentation: [README.md](README.md)

## 手柄选择

优先使用 DualSense 或 DualShock 4. 这类手柄能提供最完整的输入面, 包括 Sony Home, Create/Share, Options/Menu, 以及 DualSense Mute 等按键.

很多第三方手柄支持多种模式. 如果可以切换, 建议切到 PS4 / DualShock 4 模式再使用 ShikiPad. Xbox 模式仍可用于普通映射, 但 Windows 会接管一部分 Xbox 输入栈. 例如 Xbox Guide/Home 键无法通过 XInput 读取, HidHide 通常也不能在 HID 设备层隐藏 Xbox 手柄.

ShikiPad 现在必须使用 Interception 来输出键盘和鼠标. 这不会阻止 Xbox 手柄被读取, 因为 Xbox 输入走 XInput, 键鼠输出走 Interception. 实际限制不在 Interception, 而在 Windows 的 XInput / Xbox 栈.

## 驱动与 HidHide 顺序

推荐设置顺序:

1. 先连接手柄, 确认 Windows 能识别.
2. 右键 `install_driver.bat`, 以管理员身份安装 Interception.
3. 重启 Windows.
4. 以管理员身份运行 `ShikiPad.exe`, 确认能正常输出键盘和鼠标.
5. 如果使用 PlayStation 手柄, 再配置 HidHide, 避免游戏里出现双重输入.
6. 修改 HidHide 后, 拔插一次手柄让设置生效.

PlayStation 手柄的 HidHide 设置:

1. 在 `Applications` 里加入 `ShikiPad.exe` 的准确路径.
2. 取消勾选 `Inverse application cloak`.
3. 在 `Devices` 里勾选对应的 PlayStation 手柄. 正常会出现红色锁标志.
4. 如果分不清对应设备, 先让手柄成功连接电脑, 再临时勾选所有匹配的 game controller.
5. 勾选 `Filter-out disconnected`, `Gaming devices only`, `Enable device hiding`.
6. 关闭 HidHide, 然后拔插手柄.

如果想让电脑或游戏重新正常识别手柄, 重新勾选 `Inverse application cloak`, 或关闭 `Enable device hiding`, 然后拔插手柄.

## 发布包

正式压缩包只包含:

| 文件 | 用途 |
|---|---|
| `ShikiPad.exe` | 主程序 |
| `install_driver.bat` | Interception 驱动安装脚本 |
| `interception.dll` | Interception 运行库 |
| `driver/install-interception.exe` | Interception 驱动安装器 |
| `README.md` / `README.zh-CN.md` | 说明文档 |

运行后可能生成 `shikipad.default`, 只记录默认启动手柄型号, 不属于发布包.

## 安装

1. 解压发布包.
2. 右键 `install_driver.bat`, 以管理员身份运行.
3. 看到 `Installation complete!` 后重启 Windows.
4. 运行 `ShikiPad.exe`. 程序需要管理员权限, 因为键鼠注入依赖 Interception.
5. 首次运行时输入 `1` 到 `8` 选择手柄型号, 可保存为默认启动.

## 页面按键

| 页面 | Enter | Esc |
|---|---|---|
| 选择手柄 | 确认选择 | 退出 |
| 主界面 | 打开映射说明 | 返回选择手柄 |
| 映射说明 | 返回主界面 | 关闭软件 |

关闭控制台窗口也会退出, 并释放保持中的键盘和鼠标按键.

## 鼠标与系统键

| 手柄输入 | 输出 |
|---|---|
| 右摇杆 | 鼠标移动 |
| L3 | 鼠标左键 |
| R3 | 鼠标右键, 按下瞬间短暂冻结光标 |
| 左摇杆上 / 下 | 鼠标滚轮 |
| Sony Create / Share | `Right Alt` |
| Sony Options / Menu | `Right Ctrl` |
| Sony Home | `Right Shift` |
| DualSense 静音键 | `Caps Lock` |
| Share/Create/View + Options/Menu 长按约 1 秒 | 启用 / 禁用 ShikiPad |

## 语音输入

如果觉得手柄打字仍然太难, 建议搭配 Typeless, 闪电说等语音输入软件. PlayStation 手柄在这里很顺手: Share 是 `Right Alt`, Options/Menu 是 `Right Ctrl`, Home 是 `Right Shift`, 很适合触发语音输入相关快捷键. 支持麦克风的 PlayStation 手柄离嘴比较近, 安静环境下也能有不错的识别效果.

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

Fn 层会把数字行转换为 `F1` 到 `F12`: `1..0` 对应 `F1..F10`, `-` 对应 `F11`, `=` 对应 `F12`.

## 蓄力

默认左摇杆同一时间只保持一个修饰键. 蓄力可收集多个修饰键, 并在解除后统一释放.

| 手柄 | 开启 / 保持 |
|---|---|
| DualSense / DualShock 4 | 触控板短按切换, 长按保持 |
| Xbox | View/Back 或 Menu/Start 短按切换, 长按保持 |

## 打字键层

v3 正式版键位是按照键盘按键位置映射的. 这样会保留 `WASD` 和 `IJKL` 等常见键位布局方式, 而不是完全按字母频率重新排序.

下表列序为: `↑`, `→`, `□/X`, `△/Y`, `←`, `↓`, `×/A`, `○/B`.

| 键层 | ↑ | → | □/X | △/Y | ← | ↓ | ×/A | ○/B |
|---|---|---|---|---|---|---|---|---|
| 基础层 | ↑ | → | Space | Backspace | ← | ↓ | Enter | Tab |
| R1 / RB | u | h | j | i | g | b | k | l |
| L1 / LB | w | d | f | r | a | s | c | v |
| R2 / RT | = | y | o | p | - | 0 | n | m |
| L2 / LT | q | e | t | 1 | z | x | 3 | 2 |
| R1 + L1 | 4 | , | . | 9 | 5 | 6 | 7 | 8 |
| L2 + R2 | ( | ) | : | " | < | > | [ / { | ] / } |
| L1 + R2 | # | * | ? | _ | ; | ! | / | ' |
| L2 + R1 | @ | % | + | $ | & | ^ | \ / \| | 反引号 / ~ |

程序发送的是物理键. 需要 Shift 的按键 (`{`, `}`, `|`, `~`) 可通过按住左摇杆 `Shift` 并结合对应的基础键 (`[`, `]`, `\`, 反引号) 来输入.

基础层按住会连发. 字符层是虚拟点按: 按下一次只发送一次, 按住不会连发.

## 常见问题

### 没有键鼠输出

确认已安装 Interception, 重启 Windows, 并以管理员权限运行 `ShikiPad.exe`.

### 游戏里双重输入

Sony 手柄可用 HidHide 隐藏物理手柄, 并把 `ShikiPad.exe` 加入白名单. Xbox 手柄走 XInput, HidHide 通常不能在 HID 层隐藏它.

## 时序模型参数

ShikiPad 使用短时间窗口吸收人手快速滚动输入时的先后误差.

| 参数名 | 默认值 | 作用 |
|---|---:|---|
| `comboLayerWindowMs` | 45 ms | 基础双层组合窗口. L1+R2 和 L2+R1 额外 +10 ms, 实际按 55 ms 判定 |
| `actionLayerGraceMs` | 45 ms | 动作键与键层的识别宽容窗口 |
| `actionLayerPostGraceMs` | 20 ms | 松开键层到按下新键层间的空窗期归属 |
| `layerTakeoverWindowMs` | 30 ms | 限制旧键层与新意图键层重叠的最长时间 |
| `actionLayerSwitchGuardMs` | 35 ms | 字符发出后切换键层时抑制残留误触 |
