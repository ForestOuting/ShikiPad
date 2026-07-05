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
5. 如果使用 PlayStation 手柄, 再配置 HidHide, 避免系统或游戏里出现双重输入.
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

正式压缩包内容以桌面发布包 `ShikiPad.zip` 为准, 当前包含:

| 文件 | 用途 |
|---|---|
| `driver/install-interception.exe` | Interception 驱动安装器 |
| `ShikiPad.exe` | 主程序 |
| `interception.dll` | Interception 运行库 |
| `install_driver.bat` | Interception 驱动安装脚本 |
| `README.md` / `README.zh-CN.md` | 说明文档 |
| `shikipad.default` | 默认启动手柄型号 |
| `shiki.ico` | 程序图标 |
| `ShikiPad.manifest` | Windows 程序清单 |

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

### 鼠标参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `MouseSensitivity` | 1.0 | 右摇杆鼠标移动整体灵敏度 |
| `MouseMaxSpeed` | 20.0 | 右摇杆推满时的基础最高速度系数 |
| `RightStickDeadzone` | 0.015 | 右摇杆鼠标移动死区 |
| `RightStickCurve` | power | 右摇杆曲线类型; 当前实现使用幂函数曲线 |
| `RightStickCurveExponent` | 3.0 | 右摇杆半径曲线指数 |
| 鼠标帧倍率 | 120.0 | 右摇杆速度公式中的内部倍率 |
| 鼠标取整阈值 | 0.5 px | 小数鼠标位移累计到半个像素后输出整数移动 |
| `MaxMouseFrameSeconds` | 0.05 s | 单帧鼠标积分时间上限, 避免卡顿帧造成大跳 |
| `R3FreezeMs` | 60 ms | R3 按下右键瞬间冻结光标的时间 |

右摇杆使用连续速度积分: `radius = sqrt(x*x + y*y)`, `normalized = (radius - RightStickDeadzone) / (1 - RightStickDeadzone)`, `power = normalized ^ RightStickCurveExponent`, 每帧位移为 `direction * power * MouseMaxSpeed * deltaSec * 120 * MouseSensitivity`. X/Y 两轴分别累计小数像素, 达到 0.5px 后四舍五入输出整数像素, 剩余小数继续保留.

## 触控板手势

触控板手势只在 PlayStation 手柄上可用. 当前规则是: 手势过程中只要出现过二指, 这次手势就按二指处理; 全程没有二指才按一指处理. 方向不再看中心点, 而是看任意一个当前触点从自己的起点移动到 `TouchGestureThreshold` 后, 取这个触点左 / 右 / 上 / 下四个方向分量里最大的方向. 没达到阈值不会触发快捷键.

### 触控板映射

| 手势 | 上 | 下 | 左 | 右 |
|---|---|---|---|---|
| 一指直接滑 | `Alt + Shift + Esc` 上一个窗口 | `Alt + Esc` 下一个窗口 | `Win + Ctrl + ←` 前一个窗口 | `Win + Ctrl + →` 后一个窗口 |
| 一指长按后滑 | `Ctrl + Shift + Tab` 上一个标签页 | `Ctrl + Tab` 下一个标签页 | `Alt + F4` 关闭软件 | `Shift + Win + S` 截图 |
| 二指直接滑 | `Home` | `End` | `Win + Shift + ←` 移到左显示器 | `Win + Shift + →` 移到右显示器 |
| 二指长按后滑 | 空位 | 空位 | 空位 | 空位 |

除了一指长按后左滑关闭软件、一指长按后右滑截图、二指直接左/右滑移动窗口到显示器、二指长按后滑空位之外, 其余触控板手势都会在识别后持续连发. 第一次快捷键触发后, 先等待 `TouchGestureRepeatDelayMs`, 再按 `TouchGestureRepeatMs` 的间隔连续触发; 连发保持时只要求触控板上仍有至少一根手指.

### 触控板参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `TouchGestureMoveStartThreshold` | 50 | 认为手指已经开始移动的距离; 只改变状态, 不触发快捷键 |
| `TouchGestureThreshold` | 250 | 真正识别为滑动手势的主方向距离 |
| `TouchGestureHoldMs` | 150 ms | 从触摸开始到触发识别时, 超过此时间就按“长按后滑”处理 |
| `TouchGestureRepeatDelayMs` | 550 ms | 手势首次触发后, 到第一次连发之间的等待时间 |
| `TouchGestureRepeatMs` | 350 ms | 手势识别后保持触控板时的连发间隔 |

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

左摇杆滚轮速度按半径连续变化: 首次进入上/下扇区后进入滚轮模式, 滚轮模式内用当前上下轴决定滚轮方向, 这样轻微偏到斜上/斜下不会打断滚轮. 距离圆心的远近驱动指数 3.0 的速度曲线, 从 1500ms 慢速下限过渡到 15ms 快速上限.

左摇杆首次进入非滚轮功能扇区后, 会保持该修饰键意图直到回到退出死区以下. 滚轮输出使用当前上/下扇区决定方向, 而滚轮速度仍然按当前半径连续变化.

### 左摇杆滚轮参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `LeftStickEnterDeadzone` | 0.25 | 左摇杆进入功能扇区的半径 |
| `LeftStickExitDeadzone` | 0.15 | 左摇杆退出功能扇区并重置滚轮/修饰键意图的半径 |
| `MouseScrollCurveExponent` | 3.0 | 左摇杆滚轮半径曲线指数 |
| `ScrollSlowIntervalMs` | 1500 ms | 最慢滚轮间隔 |
| `ScrollFastIntervalMs` | 15 ms | 最快滚轮间隔 |
| `WheelDelta` | 120 | 一个标准滚轮格 |
| `WheelRoundingThreshold` | 0.5 | 和右摇杆鼠标一致, 小数滚轮量累计到半个单位后四舍五入输出整数 |
| `MaxWheelDeltaPerFrame` | 120 | 单帧最多输出的滚轮量 |

左摇杆滚轮现在尽量贴近右摇杆鼠标的积分思路: 半径先归一化为 `(radius - LeftStickEnterDeadzone) / (1 - LeftStickEnterDeadzone)`, 再计算 `power = normalized ^ MouseScrollCurveExponent`. 最高速度是 `WheelDelta * 1000 / ScrollFastIntervalMs`, 当前速度为 `最高速度 * power`, 同时不低于 `WheelDelta * 1000 / ScrollSlowIntervalMs` 的慢速下限. 程序每帧累计小数滚轮量, 达到 0.5 后像右摇杆像素一样四舍五入输出整数滚轮单位, 每帧最多输出 120.

## 蓄力

默认左摇杆同一时间只保持一个修饰键. 蓄力可收集多个修饰键, 并在解除后统一释放.

蓄力激活时, 已收集的修饰键会继续保持, 即使左摇杆离开原来的方向也不会丢失. 这样可以先保留一个修饰键, 再把摇杆推到上/下触发滚轮, 或转到其他修饰键方向继续叠加. 解除蓄力后, 收集到的修饰键会统一释放.

| 手柄 | 开启 / 保持 |
|---|---|
| DualSense / DualShock 4 | 触控板短按切换, 长按保持 |
| Xbox | View/Back 或 Menu/Start 短按切换, 长按保持 |

### 蓄力参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `ClutchLongPressMs` | 250 ms | 触控板 / View / Menu 长按保持蓄力的判定时间 |

## 打字键层

v3 正式版键位是按照键盘按键位置映射的. 这样会保留 `WASD` 和 `IJKL` 等常见键位布局方式, 而不是完全按字母频率重新排序.

下表列序为: `↑`, `→`, `□/X`, `△/Y`, `←`, `↓`, `×/A`, `○/B`.

| 键层 | ↑ | → | □/X | △/Y | ← | ↓ | ×/A | ○/B |
|---|---|---|---|---|---|---|---|---|
| 基础层 | ↑ | → | Space | Backspace | ← | ↓ | Enter | Tab |
| R1 / RB | o | p | j | i | n | m | k | l |
| L1 / LB | w | d | q | e | a | s | z | x |
| R2 / RT | 0 | g | y | u | - | = | b | h |
| L2 / LT | r | f | t | 1 | c | v | 3 | 2 |
| R1 + L1 | 4 | , | . | 7 | 5 | 6 | 9 | 8 |
| L2 + R2 | + | / | & | * | _ | ^ | $ | % |
| L1 + R2 | [ | ] | ! | ? | { | } | @ | # |
| R1 + L2 | ( | ) | ; | ' | < | > | 反引号 | \ |

程序发送的是物理键. 需要 Shift 的按键 (", :, \|, ~) 可通过按住左摇杆 `Shift` 并结合对应的基础键 (', ;, \\, 反引号) 来输入.

基础层按住会连发. 字符层是虚拟点按: 按下一次只发送一次, 按住不会连发.

### 基础层连发参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `RepeatDelayMs` | 300 ms | 基础层按下首发后, 等待多久开始连发 |
| `BaseRepeatSlowIntervalMs` | 120 ms | 连发起步间隔 |
| `BaseRepeatRampMs` | 1500 ms | 从慢速连发爬坡到最快连发的时间 |
| `RepeatIntervalMs` | 12 ms | 最快连发间隔 |
| 连发曲线指数 | 3.0 | 对频率做三次方加速, 爬坡段是连续函数 |

如果多个键层按键落在同一个轮询时间戳上, 同时按下优先级为 `R1 > L1 > R2 > L2`. 组合层仍然只会在这个排序之后查看最新的两个仍按住键层.

### 键层时序参数

ShikiPad 使用短时间窗口吸收人手快速按键输入时的先后误差.

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `ComboLayerWindowMs` | 35 ms | 两个键层按键结成组合层的最大间隔 |
| `ActionLayerGraceMs` | 35 ms | 动作键与键层的识别宽容窗口 |
| `ActionLayerPostGraceMs` | 15 ms | 松开键层到按下新键层间的空窗期归属 |
| `LayerTakeoverWindowMs` | 25 ms | 本体累计上限; 15ms 截止点落入某个旧键层本体后, 向前追溯最多继续到累计本体占用 25ms |
| `LayerOccupancyCarryCutoffMs` | 15 ms | 键层本体累计的向前追溯截止点; 总视野仍是 `ActionLayerGraceMs`, 但累计本体占用到达这个点后, 只能在当前边界键层本体内继续追到 `LayerTakeoverWindowMs`, 不能跨到它的前置窗口或更旧键层 |
| `ActionLayerSwitchGuardMs` | 35 ms | 字符发出后切换键层时抑制残留误触 |

组合层按单独键层处理: 组成组合层的同一次单键按下仍然占用 35ms 时间线, 但不算作该组合层自己的本体累计, 也不会触发该组合层的 15ms/25ms 本体累计限制.

## 常见问题

### 没有键鼠输出

确认已安装 Interception, 重启 Windows, 并以管理员权限运行 `ShikiPad.exe`.

### 系统或游戏里双重输入

如果 ShikiPad 运行时 Windows 仍然能看到物理手柄, 同一个摇杆输入就可能被处理两次: 一次由 ShikiPad 处理, 一次由 Windows 或当前软件处理. 典型表现包括左摇杆保持 `Alt` 再按 `Tab` 时在各个窗口之间乱跳, 或左摇杆 `Win` 修饰键触发不灵, 因为 Windows 把手柄输入识别成开始菜单, 任务栏或应用图标导航. 按上文的 HidHide 设置隐藏 PlayStation 手柄后, 只让 ShikiPad 读取手柄, 就能解决这类问题. Xbox 手柄走 XInput, HidHide 通常不能在 HID 层隐藏它.
