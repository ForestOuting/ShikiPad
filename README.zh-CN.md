# ShikiPad

Windows 原生手柄键鼠映射工具, 目前只支持 PS5 DualSense 手柄的有线 USB 模式.

English documentation: [README.md](README.md)

## 支持的手柄

请使用官方 DualSense 手柄并通过 USB 连接. 当前版本不支持蓝牙、Xbox、DualShock 4 或其他手柄模式.

ShikiPad 现在必须使用 Interception 来输出键盘和鼠标.

能力简表:

| 模式 | ShikiPad 支持 | 限制 |
|---|---|---|
| DualSense USB | 完整 PlayStation 功能: 按键、摇杆、扳机、Home、触控板按压/手势、Create/Options、DualSense 静音键 | 启动 ShikiPad 前请先用 USB 连接手柄 |

## 驱动与 HidHide 顺序

推荐设置顺序:

1. 先连接手柄, 确认 Windows 能识别.
2. 右键 `install_driver.bat`, 以管理员身份安装 Interception.
3. 重启 Windows.
4. 以管理员身份运行 `ShikiPad.exe`, 确认能正常输出键盘和鼠标.
5. 为 DualSense 配置 HidHide, 避免系统或游戏里出现双重输入.
6. 修改 HidHide 后, 拔插一次手柄让设置生效.

DualSense 的 HidHide 设置:

1. 在 `Applications` 里加入 `ShikiPad.exe` 的准确路径.
2. 取消勾选 `Inverse application cloak`.
3. 在 `Devices` 里勾选对应的 DualSense 手柄. 正常会出现红色锁标志.
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
| `shiki.ico` | 程序图标 |
| `ShikiPad.manifest` | Windows 程序清单 |

## 安装

1. 解压发布包.
2. 右键 `install_driver.bat`, 以管理员身份运行.
3. 看到 `Installation complete!` 后重启 Windows.
4. 用 USB 连接 PS5 DualSense 手柄.
5. 运行 `ShikiPad.exe`. 程序需要管理员权限, 因为键鼠注入依赖 Interception. 程序会直接进入有线 DualSense 连接状态, 不再显示手柄选择页面.

## 开机自启动

ShikiPad 需要管理员权限才能稳定使用 Interception 输出键盘和鼠标, 所以推荐用“任务计划程序 + 最高权限”实现开机登录后自启动. 在 ShikiPad 文件夹里打开管理员 PowerShell 或终端, 执行:

```powershell
.\ShikiPad.exe --install-startup
```

这会创建一个名为 `ShikiPad` 的任务计划程序条目, 在当前用户登录时启动当前路径的 `ShikiPad.exe`. 如果之后移动了 ShikiPad 文件夹, 需要在新文件夹里重新执行一次安装命令.

取消开机自启动:

```powershell
.\ShikiPad.exe --uninstall-startup
```

不建议只把快捷方式丢进普通“启动”文件夹, 因为启动文件夹不能可靠地以管理员权限启动 ShikiPad, 容易被 UAC 拦住.

## 页面按键

| 页面 | Enter | Esc |
|---|---|---|
| 主界面 | 打开映射说明 | 关闭软件 |
| 映射说明 | 返回主界面 | 关闭软件 |

关闭控制台窗口也会退出, 并释放保持中的键盘和鼠标按键.

## 鼠标与系统键

| 手柄输入 | 输出 |
|---|---|
| 右摇杆 | 鼠标移动 |
| L3 | 鼠标左键 |
| R3 | 鼠标右键, 按下瞬间短暂冻结光标 |
| 左摇杆上 / 下 | 鼠标滚轮 |
| Create | `Right Alt` |
| Options | `Right Ctrl` |
| DualSense 静音键按下 | 启用 / 禁用 ShikiPad |
| 触控板按压时任一触点在左确定区 | `Delete` |
| 触控板按压时任一触点在右确定区 | `Backspace` |
| 触控板按压时所有触点都在中间缓冲区 | 切换真实 `Caps Lock` 和手柄 Fn 层 |

### 鼠标参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `MouseSensitivity` | 1.0 | 右摇杆鼠标移动整体灵敏度 |
| `MouseMaxSpeed` | 20.0 | 右摇杆推满时的基础最高速度系数 |
| `RightStickDeadzone` | 0.015 | 右摇杆鼠标移动死区 |
| `RightStickCurve` | power | 右摇杆曲线类型; 当前实现使用幂函数曲线 |
| `RightStickCurveExponent` | 3.0 | 右摇杆半径曲线指数 |
| `RightStickSmoothingMs` | 5 ms | 右摇杆 X/Y 输入进入鼠标曲线前的短指数平滑时间 |
| 鼠标帧倍率 | 120.0 | 右摇杆速度公式中的内部倍率 |
| 鼠标取整阈值 | 0.5 px | 小数鼠标位移累计到半个像素后输出整数移动 |
| `MaxMouseFrameSeconds` | 0.05 s | 单帧鼠标积分时间上限, 避免卡顿帧造成大跳 |
| `R3FreezeMs` | 60 ms | R3 按下右键瞬间冻结光标的时间 |

右摇杆使用连续速度积分: 先对 X/Y 输入做 5ms 短指数平滑, 再计算 `radius = sqrt(x*x + y*y)`, `normalized = (radius - RightStickDeadzone) / (1 - RightStickDeadzone)`, `power = normalized ^ RightStickCurveExponent`, 每帧位移为 `direction * power * MouseMaxSpeed * deltaSec * 120 * MouseSensitivity`. X/Y 两轴分别累计小数像素, 达到 0.5px 后四舍五入输出整数像素, 剩余小数继续保留.

## 触控板手势

触控板手势依赖有线 DualSense USB HID 完整报告. 现在只启用一指直接滑映射; 二指识别只在手势完成识别前有效. 如果识别前出现两个触点, 这次手势就算二指; 当前二指没有快捷键, 所以会直接阻断到抬手. 一指手势识别完成后, 后续追加的触点会被忽略, 已锁定的一指手势继续保持稳定. 手指从起点移动超过 `TouchGestureHoldStillDistance` 才算开始移动; 如果开始移动前已经静止满 `TouchGestureHoldMs`, 这次算长按后滑, 当前不触发快捷键. 方向不再看中心点, 纵向手势移动 150 触发, 横向手势移动 180 触发. 左右半区只在第一次识别时决定: 触控板左右各 `TouchGestureSideConfirmedWidth` 是确定区, 起点在左确定区就锁左, 起点在右确定区就锁右; 如果起点在中间缓冲区, 通过跨过中心分割线来锁定, 右缓冲进入左缓冲就锁左, 左缓冲进入右缓冲就锁右. 锁定前已经在缓冲区里移动的距离仍计入该方向的第一次触发. 对于按距离连发的手势, 第一次识别也会消费完整的方向距离段, 然后从最后一个已消费的触发点继续累计. 识别完成后半区锁定到抬手, 后续整个触控板都只作为这个手势的移动空间.

### 触控板映射

| 手势 | 上 | 下 | 左 | 右 |
|---|---|---|---|---|
| 左半区一指直接滑 | `Alt + Shift + Esc` 上一个窗口 | `Alt + Esc` 下一个窗口 | 用 `Alt + Shift + Tab` 进入窗口切换, 然后只按住 `Alt` | 用 `Alt + Tab` 进入窗口切换, 然后只按住 `Alt` |
| 右半区一指直接滑 | `Win + ↑` 最大化 | `Win + ↓` 还原/最小化 | `Win + Ctrl + ←` 前一个桌面 | `Win + Ctrl + →` 后一个桌面 |

触控板按压不是蓄力键, 而是在按下瞬间检查当前活动触点的 X 位置: 任一触点在左确定区就输出 `Delete`, 任一触点在右确定区就输出 `Backspace`; 如果左右确定区同时有触点, 则优先输出 `Backspace`. 只有所有活动触点都在中间缓冲区时才切换真实 `Caps Lock` 和手柄 Fn 层. 当 Home 处于蓄力模式时, 触控板 `Delete` 和 `Backspace` 算作基础层动作键, 触发后会让短按锁定的蓄力归零; 当 Home 作为真实 `Left Shift` 被按住时, 触控板 `Delete` / `Backspace` 会自然变成 `Shift + Delete` 这样的组合. 触控板 `Delete` 和 `Backspace` 使用和基础层相同的渐进连发逻辑. 中间缓冲区的 Caps/Fn 是独立的键层开关, 不是基础层动作键; 它只在按下瞬间触发一次, 不连发.

所有一指直接滑第一次触发都需要从本次触点起点移动到对应方向距离: 上/下是 150, 左/右是 180. 如果起点在中间缓冲区, 锁定半区前已经移动的距离也算在横向 180 里; 锁定并触发后, 这个触发点就是后续连发或反向判断的新起点.

缓冲区横向快速识别不是简单按起点所在缓冲区固定判定. 起点在左缓冲区 `550..959` 时, 向左移动 180 会直接锁左并触发左区快捷键; 向右移动时, 只在左缓冲区内移动 180 不会判右, 必须进入右缓冲区 `960..1369` 后才锁右并触发一次右区快捷键. 起点在右缓冲区时规则对称: 向右移动 180 直接锁右; 向左移动时必须进入左缓冲区后才锁左. 也就是说, 朝远离中心的方向可以在本缓冲区快速锁定, 朝另一侧移动则要跨过中心分割线后才锁定到另一侧.

按距离连发用于左半区左/右 Alt-Tab 进入窗口切换后的导航. 左/右第一次触发只按一次 `Alt + Shift + Tab` 或 `Alt + Tab` 进入窗口切换界面, 然后在手指仍停留在触控板时只保持 `Alt`. 进入窗口切换界面后, 每继续移动一个方向距离就发送对应方向键: 左 / 右各 180 发送 `←` / `→`, 上 / 下各 150 发送 `↑` / `↓`. 反向不再使用 Shift, 只发送反向方向键. 如果某一次状态更新直接跳过多个完整方向距离段, 初次 Alt-Tab 之外的额外段数会按方向键补齐, 剩余不足触发距离的部分继续保留到下一次方向键触发.

按时间连发用于左半区上/下窗口切换, 以及右半区左/右桌面切换. 第一次触发同样使用方向距离: 上/下 150, 左/右 180. 左半区上/下使用 `TouchGestureTimeRepeatIntervalMs`, 当前是 450 ms. 右半区左/右桌面切换使用 `TouchGestureDesktopRepeatIntervalMs`, 当前是 550 ms. 同方向继续移动到该方向距离不额外触发, 只刷新反向判断的新起点. 不抬手切成反向时, 从这个最新起点反向移动纵向 150 或横向 180 会立刻触发反向快捷键, 然后反向也按这个快捷键自己的固定间隔继续时间连发. 右半区上/下 `Win + ↑/↓` 不连发.

### 触控板参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `TouchGestureHoldStillDistance` | 50 | 判断手指仍算静止的最大移动距离; 超过后才记录开始移动时间 |
| `TouchGestureVerticalThreshold` | 150 | 识别并触发上/下滑动所需的移动距离 |
| `TouchGestureHorizontalThreshold` | 180 | 识别并触发左/右滑动所需的移动距离 |
| `TouchGestureVerticalRepeatDistance` | 150 | 每次纵向连发/反向/导航触发所需的移动距离 |
| `TouchGestureHorizontalRepeatDistance` | 180 | 每次横向连发/反向/导航触发所需的移动距离 |
| `TouchGestureTimeRepeatDelayMs` | 450 ms | 左半区上/下时间连发的首次等待时间 |
| `TouchGestureTimeRepeatIntervalMs` | 450 ms | 左半区上/下时间连发的固定间隔; 正向和反向触发后都按这个间隔继续 |
| `TouchGestureDesktopRepeatIntervalMs` | 550 ms | 右半区左/右桌面切换时间连发的首次等待和固定间隔 |
| `TouchGestureSideConfirmedWidth` | 550 | 左右两侧确定区宽度; 550..959 和 960..1369 的中间缓冲区通过跨过中心分割线锁定左右 |
| `TouchGestureHoldMs` | 600 ms | 手指开始移动前静止这么久就算长按后滑; 当前长按后滑没有映射 |

## 语音输入

如果觉得手柄打字仍然太难, 建议搭配 Typeless, 闪电说等语音输入软件. DualSense 在这里很顺手: Create 是 `Right Alt`, Options 是 `Right Ctrl`, Home 根据按下时的状态负责蓄力或真实 `Left Shift`, DualSense 静音键按下即切换启用 / 禁用 ShikiPad.

## 左摇杆

| 方向 | 输出 |
|---|---|
| 左上 | `Left Shift` |
| 左下 | `Ctrl` |
| 右上 | `Win` |
| 右下 | `Left Alt` |
| 上 / 下中间区 | 鼠标滚轮 |

左摇杆现在是六等分: 上半区三个扇区, 下半区三个扇区. 原来的纯左 / 纯右扇区已经删除.

左摇杆滚轮速度在当前扇区为上/下时按半径连续变化. 上/下滚轮扇区使用 `LeftStickEnterDeadzone` 进入, 当前是 0.15. 移动到修饰键扇区会立刻停止滚轮; 这个修饰键只有在半径达到 `LeftStickModifierEnterDeadzone` 后才触发, 当前是 0.45. 再移回上/下扇区时, 达到滚轮门槛后就按当前半径继续滚轮. 距离圆心的远近驱动指数 3.0 的速度曲线, 从 1500ms 慢速下限过渡到 15ms 快速上限.

左摇杆修饰键扇区是即时生效, 不再锁定第一次触发的修饰键. 360 度角度仍然六等分, 每个扇区 60 度, 只是半径死区按扇区类型不同: 滚轮扇区 0.15 进入, 修饰键扇区 0.45 进入. ShikiPad 每帧都跟随当前扇区: 从 `Left Shift` 移到 `Win`, `Ctrl`, `Left Alt` 或滚轮扇区时, 会释放前一个输出, 并且只有目标扇区达到自己的门槛时才应用当前输出. 如果目标扇区没有达到自己的门槛, 左摇杆输出就是中立.

### 左摇杆参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `LeftStickEnterDeadzone` | 0.15 | 左摇杆进入上/下滚轮扇区的半径; 低于同一个阈值也会清空滚轮累计量 |
| `LeftStickModifierEnterDeadzone` | 0.45 | 左摇杆进入四个修饰键扇区的半径 |
| `MouseScrollCurveExponent` | 3.0 | 左摇杆滚轮半径曲线指数 |
| `MouseScrollSmoothingMs` | 5 ms | 左摇杆滚轮归一化半径进入滚轮曲线前的短指数平滑时间 |
| `ScrollSlowIntervalMs` | 1500 ms | 最慢滚轮间隔 |
| `ScrollFastIntervalMs` | 15 ms | 最快滚轮间隔 |
| `WheelDelta` | 120 | 一个标准滚轮格 |
| `WheelRoundingThreshold` | 0.5 | 和右摇杆鼠标一致, 小数滚轮量累计到半个单位后四舍五入输出整数 |
| `MaxWheelDeltaPerFrame` | 120 | 单帧最多输出的滚轮量 |

左摇杆滚轮现在尽量贴近右摇杆鼠标的积分思路: 半径先归一化为 `(radius - LeftStickEnterDeadzone) / (1 - LeftStickEnterDeadzone)`, 归一化半径经过同样 5ms 风格的短平滑后, 再计算 `power = normalized ^ MouseScrollCurveExponent`. 最高速度是 `WheelDelta * 1000 / ScrollFastIntervalMs`, 当前速度为 `最高速度 * power`, 同时不低于 `WheelDelta * 1000 / ScrollSlowIntervalMs` 的慢速下限. 程序每帧累计小数滚轮量, 达到 0.5 后像右摇杆像素一样四舍五入输出整数滚轮单位, 每帧最多输出 120.

## 蓄力

默认左摇杆同一时间只保持一个修饰键. 蓄力可收集多个修饰键, 并在解除后统一释放.

蓄力激活时, 已收集的修饰键会继续保持, 即使左摇杆移动到其他位置也不会丢失. 如果要继续叠加另一个修饰键, 直接移动到对应修饰键扇区即可; 如果要在蓄力期间滚轮, 直接移动到上/下扇区即可. 短按锁定的蓄力会在任意动作键实际发出后自动解除, 包括触控板确定区的基础层 `Delete` / `Backspace`; 如果还没按动作键, 再短按一次会取消锁定; 长按保持的蓄力仍然按住保持, 松开释放.

Home 在按下瞬间决定本次模式. 如果按下 Home 的那一刻没有左摇杆修饰键处于触发状态, Home 就是一个真实的 `Left Shift` 按键, 按住 Home 就按住 `Left Shift`, 松开 Home 就松开 `Left Shift`. 在这次 Home-as-Shift 模式期间, 左摇杆修饰键不会输出; Home 松开之后, 当前左摇杆扇区可以立刻正常生效.

如果按下 Home 的那一刻已经有左摇杆修饰键处于触发状态, Home 就遵循纯净蓄力逻辑: 短按锁定已收集的修饰键直到下一次动作键, 如果还没触发动作键则再次短按取消, 长按则保持修饰键直到 Home 松开. 蓄力期间动作键保持正常映射, 正常映射里的 `1` 仍然输出 `1`, 不会变成 `F1`.

触控板中间缓冲区按压会切换真实系统 `Caps Lock`, 键盘指示灯会跟着变化. ShikiPad 同时把这次状态记为手柄 Fn 层: 这个层打开时, 未带 Shift 的动作键映射 `1..0`, `-`, `=` 会变成 `F1..F12`; 触发一次这样的 Fn 动作后, ShikiPad 会把 `Caps Lock` 恢复到打开手柄 Caps/Fn 层之前的状态, 并清空这个层. 蓄力释放仍然只由 Home 自己决定: 短按蓄力在动作键触发后释放, 长按蓄力则在 Home 松开时释放. 其他动作键在真实 `Caps Lock` 打开时按正常按键发送, 不会清空手柄 Fn 层. 如果要手动取消, 再次按压触控板中间缓冲区. 手柄 Fn 层优先于 Home 蓄力输出, 所以可以先打开 Caps/Fn 层, 再用 Home 收集 `Win + Alt`, 然后按 `4` 触发 `Win + Alt + F4`.

| 手柄 | 开启 / 保持 |
|---|---|
| DualSense | 先触发左摇杆修饰键再按 Home: 短按锁定到下一次动作键, 再短按取消, 长按保持; 没有修饰键时按 Home 就是真实 `Left Shift` |

### 蓄力参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `ClutchLongPressMs` | 250 ms | Home 长按保持蓄力的判定时间 |

## 打字键层

v3 正式版键位是按照键盘按键位置映射的. 这样会保留 `WASD` 和 `IJKL` 等常见键位布局方式, 而不是完全按字母频率重新排序.

下表列序为: `↑`, `→`, `□`, `△`, `←`, `↓`, `×`, `○`.

| 键层 | ↑ | → | □ | △ | ← | ↓ | × | ○ |
|---|---|---|---|---|---|---|---|---|
| 基础层 | ↑ | → | Tab | Esc | ← | ↓ | Space | Enter |
| R1 | o | p | j | i | n | m | k | l |
| L1 | w | d | q | e | a | s | z | x |
| R2 | 0 | g | y | u | - | = | b | h |
| L2 | r | f | t | 1 | c | v | 3 | 2 |
| R1 + L1 | 4 | , | . | 7 | 5 | 6 | 9 | 8 |
| L2 + R2 | + | / | & | * | _ | ^ | $ | % |
| L1 + R2 | [ | ] | ! | ? | { | } | @ | # |
| R1 + L2 | ( | ) | ; | ' | < | > | 反引号 | \ |

程序发送的是物理键. 需要 Shift 的按键 (", :, \|, ~) 会由对应键层自动发送 Shift; 没有左摇杆修饰键时按住 Home 也会按住真实 `Left Shift`.

基础层方向键按住会连发. 基础层图案键 (`Square`, `Triangle`, `Cross`, `Circle`) 不连发. 字符层是虚拟点按: 按下一次只发送一次, 按住不会连发. 动作键一旦解析到某个键层并进入保持状态, 后续肩键或扳机键变化不会把这个仍按住的物理键重新分配到其他键层, 必须松开后再按才会重新判定.

### 基础层连发参数

| 参数名 | 当前值 | 作用 |
|---|---:|---|
| `RepeatDelayMs` | 300 ms | 可连发的基础层按键或触控板 `Delete` / `Backspace` 首发后, 等待多久开始连发 |
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
| `ActionLayerGraceMs` | 45 ms | 动作键与键层的识别宽容窗口 |
| `ActionLayerPostGraceMs` | 15 ms | 松开键层到按下新键层间的空窗期归属; 如果这个键层输入曾经和其他键层输入重叠, 本次松开不获得后置窗口 |
| `LayerTakeoverWindowMs` | 30 ms | 本体累计上限; 20ms 截止点落入某个旧键层本体后, 向前追溯最多继续到累计本体占用 30ms |
| `LayerOccupancyCarryCutoffMs` | 20 ms | 键层本体累计的向前追溯截止点; 总视野仍是 `ActionLayerGraceMs`, 但累计本体占用到达这个点后, 只能在当前边界键层本体内继续追到 `LayerTakeoverWindowMs`, 不能跨到它的前置窗口或更旧键层 |

组合层按单独键层处理: 组成组合层的同一次单键按下仍然占用 35ms 时间线, 但不算作该组合层自己的本体累计, 也不会触发该组合层的 20ms/30ms 本体累计限制.

## 常见问题

### 没有键鼠输出

确认已安装 Interception, 重启 Windows, 并以管理员权限运行 `ShikiPad.exe`.

### 系统或游戏里双重输入

如果 ShikiPad 运行时 Windows 仍然能看到物理手柄, 同一个摇杆输入就可能被处理两次: 一次由 ShikiPad 处理, 一次由 Windows 或当前软件处理. 典型表现包括左摇杆保持 `Alt` 再按 `Tab` 时在各个窗口之间乱跳, 或左摇杆 `Win` 修饰键触发不灵, 因为 Windows 把手柄输入识别成开始菜单, 任务栏或应用图标导航. 按上文的 HidHide 设置隐藏 DualSense 后, 只让 ShikiPad 读取手柄, 就能解决这类问题.

如果已经按上文配置 HidHide, 但仍然出现双重输入或系统抢输入, 可以试试把 ShikiPad 整个文件夹放到 C 盘根目录运行, 例如 `C:\ShikiPad`. 这是当前作者实际使用的位置.
