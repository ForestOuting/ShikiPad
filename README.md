# ShikiPad

ShikiPad is a native Windows controller-to-keyboard and mouse mapper focused on the wired PS5 DualSense controller.

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

## Supported Controller

Use an official DualSense controller over USB. Bluetooth, Xbox, and DualShock 4 modes are intentionally not supported in the current build.

ShikiPad now requires Interception for keyboard and mouse output.

Capability summary:

| Mode | ShikiPad support | Limits |
|---|---|---|
| DualSense USB | Full PlayStation feature set: buttons, sticks, triggers, Home, touchpad click/gestures, Create/Options, DualSense Mute | Connect by USB before starting ShikiPad |

## Driver And Installation Order

Recommended setup order:

1. Connect the controller once and confirm Windows can see it.
2. Install Interception with `install_driver.bat` as administrator.
3. Restart Windows.
4. Run `ShikiPad.exe` as administrator and confirm it can send keyboard and mouse output.
5. Configure HidHide for the DualSense controller to prevent system or game double input.
6. Unplug and reconnect the controller after HidHide changes.

HidHide setup for DualSense:

1. In `Applications`, add the exact path to `ShikiPad.exe`.
2. Uncheck `Inverse application cloak`.
3. In `Devices`, check the target DualSense controller. A red lock icon should appear.
4. If the target controller is unclear, connect the controller successfully first, then temporarily check all matching game controllers.
5. Check `Filter-out disconnected`, `Gaming devices only`, and `Enable device hiding`.
6. Close HidHide, then unplug and reconnect the controller.

To let Windows and games see the controller normally again, re-check `Inverse application cloak` or disable `Enable device hiding`, then unplug and reconnect the controller.

## Release Package

A release archive follows the actual desktop `ShikiPad.zip` package. The current package contains:

| File | Purpose |
|---|---|
| `driver/install-interception.exe` | Interception driver installer |
| `ShikiPad.exe` | Main program |
| `interception.dll` | Interception runtime |
| `install_driver.bat` | Interception driver installer helper |
| `README.md` / `README.zh-CN.md` | Documentation |
| `RELEASE_NOTES.md` | Release notes |
| `shiki.ico` | Program icon |
| `ShikiPad.manifest` | Windows application manifest |

## Startup

ShikiPad needs administrator privileges for Interception output, so the recommended startup method is a Windows scheduled task with highest privileges. Run an elevated PowerShell or Terminal in the ShikiPad folder and use:

```powershell
.\ShikiPad.exe --install-startup
```

This creates a Task Scheduler entry named `ShikiPad` that starts the current `ShikiPad.exe` path when the user logs in. If you move the ShikiPad folder later, run the install command again from the new folder.

To remove startup:

```powershell
.\ShikiPad.exe --uninstall-startup
```

Do not rely on the normal Startup folder unless you are willing to handle UAC manually, because Startup-folder shortcuts cannot reliably start ShikiPad elevated.

## Console Pages

| Page | Enter | Esc |
|---|---|---|
| Home | Open mapping manual | Exit ShikiPad |
| Mapping manual | Return home | Exit ShikiPad |

Closing the console window also exits and releases held keyboard and mouse inputs.

## Mouse And System Buttons

| Controller input | Output |
|---|---|
| Right stick | Mouse movement |
| L3 | Left mouse button |
| R3 | Right mouse button with a short cursor freeze on press |
| Left stick up / down | Mouse wheel |
| Create | `Right Alt` |
| Options | `Right Ctrl` |
| Short-tap DualSense Mute | Toggle one-shot Caps/Fn layer for the next action key |
| Long-press DualSense Mute | Enable / disable ShikiPad |
| No active touch point during touchpad click | `Backspace` |
| Two active touch points during touchpad click | `Backspace` |
| One active touch point in the left confirmed zone during touchpad click | `Delete` |
| One active touch point in the right confirmed zone during touchpad click | `Backspace` |
| One active touch point in the middle buffer during touchpad click | Tap real `Caps Lock` |

### Mouse Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `MouseSensitivity` | 1.0 | Overall right-stick mouse sensitivity |
| `MouseMaxSpeed` | 20.0 | Base maximum speed factor at full right-stick tilt |
| `RightStickDeadzone` | 0.015 | Right-stick mouse deadzone |
| `RightStickCurve` | power | Right-stick curve type; the current implementation uses a power curve |
| `RightStickCurveExponent` | 3.0 | Right-stick radius curve exponent |
| `RightStickSmoothingMs` | 5 ms | Short exponential smoothing on the right-stick X/Y input before the mouse curve |
| Mouse frame multiplier | 120.0 | Internal multiplier in the right-stick velocity formula |
| Mouse rounding threshold | 0.5 px | Fractional mouse movement is emitted once it reaches half a pixel |
| `MaxMouseFrameSeconds` | 0.05 s | Per-frame mouse integration cap to prevent large jumps after a stalled frame |
| `R3FreezeMs` | 60 ms | Cursor freeze duration when R3 starts a right click |

The right stick uses continuous velocity integration. It first applies the 5 ms input smoother to X/Y, then calculates `radius = sqrt(x*x + y*y)`, `normalized = (radius - RightStickDeadzone) / (1 - RightStickDeadzone)`, `power = normalized ^ RightStickCurveExponent`, and per-frame movement as `direction * power * MouseMaxSpeed * deltaSec * 120 * MouseSensitivity`. X/Y fractional pixels are accumulated separately, rounded to integer pixels once they reach 0.5px, and the remainder is kept.

## Touchpad Gestures

Touchpad gestures are available on the wired DualSense USB HID report. One-finger and two-finger gestures both exist in direct-swipe and hold-then-swipe forms. Finger count is decided only before a gesture has been recognized: if two touch points appear before recognition, the gesture counts as two-finger; once a gesture has been recognized, later extra touch points are ignored so the locked gesture stays stable. A finger must move more than `TouchGestureHoldStillDistance` from its start point before ShikiPad treats it as moving, but direct-vs-hold classification is decided later, when the swipe reaches the 150/180 trigger distance. If the touch has lasted at least `TouchGestureHoldMs`, currently 450 ms, at that trigger moment, the gesture counts as hold-then-swipe; otherwise it counts as direct-swipe. Direction no longer uses the center point. Instead, vertical gestures trigger at 150 distance and horizontal gestures trigger at 180 distance. Touchpad side is decided only at first recognition: the leftmost and rightmost `TouchGestureSideConfirmedWidth` pixels are confirmed zones, so starts in the left confirmed zone lock left and starts in the right confirmed zone lock right. Starts in the middle buffer lock by crossing the center split: right-buffer-to-left-buffer motion locks left, and left-buffer-to-right-buffer motion locks right. Distance moved inside the buffer before side lock still counts toward that direction's first trigger. For distance-repeat gestures, first recognition also consumes every complete direction-specific segment and then continues counting from the last consumed trigger point. After recognition, the side stays locked until touch release and the whole touchpad remains valid movement space for that gesture.

### Touchpad Mappings

| Gesture | Up | Down | Left | Right |
|---|---|---|---|---|
| Left-half one-finger direct swipe | `Alt + Shift + Esc` previous window | `Alt + Esc` next window | Enter Alt-Tab with `Alt + Shift + Tab`, then hold `Alt` | Enter Alt-Tab with `Alt + Tab`, then hold `Alt` |
| Right-half one-finger direct swipe | `Win + ↑` maximize | `Win + ↓` restore/minimize | `Win + Ctrl + ←` previous desktop | `Win + Ctrl + →` next desktop |
| Left-half one-finger hold-then-swipe | `Win + Shift + M` restore minimized windows | `Win + M` minimize all windows | Unmapped | Unmapped |
| Right-half one-finger hold-then-swipe | `Home` | `End` | `Win + Shift + ←` move window to left monitor | `Win + Shift + →` move window to right monitor |
| Left-half two-finger direct swipe | `Ctrl + Shift + Esc` | `Shift + Win + S` screenshot | Unmapped | `Alt + F4` |
| Right-half two-finger direct swipe | `Ctrl + Shift + Tab` previous tab | `Ctrl + Tab` next tab | `Alt + ←` back | `Alt + →` forward |
| Right-half two-finger hold-then-swipe | Unmapped | Unmapped | `Win + Shift + ←` move window to left monitor | `Win + Shift + →` move window to right monitor |

Touchpad click is not the clutch key. It checks the active touch point state when the click begins. No active touch point sends `Backspace`, and any two active touch points also send `Backspace`. With exactly one active touch point, the left confirmed zone sends `Delete`, the right confirmed zone sends `Backspace`, and the middle buffer taps real `Caps Lock`; this is pure Caps Lock and does not enable Fn. Touchpad `Delete` and `Backspace` count as base-layer action keys. Whether a short-tap Home clutch lock can be consumed by an action key is decided once, when that Home short-tap lock is formed. If at least one modifier had already been collected at that moment, touchpad `Delete` or `Backspace` clears the lock after firing; modifiers collected later do not make that same lock consumable. Touchpad `Delete` and `Backspace` use the same progressive repeat timing as base-layer repeat. The middle-buffer Caps Lock click fires once on click-down and does not repeat.

Every touchpad swipe first trigger requires movement from that touch point's start by the direction threshold: 150 for up/down and 180 for left/right. If the touch starts in the middle buffer, distance moved before side lock still counts toward the horizontal 180. After the side locks and the shortcut fires, that trigger point becomes the new origin for later repeat or reverse checks.

Middle-buffer horizontal recognition is not a fixed decision based only on the start buffer. If the touch starts in the left buffer, `550..959`, moving left by 180 immediately locks left and fires the left-side shortcut. Moving right by 180 while still inside the left buffer does not lock right; it waits until the touch enters the right buffer, `960..1369`, then locks right and fires the right-side shortcut once. The right buffer is symmetric: moving right by 180 locks right immediately, while moving left must enter the left buffer before it locks left. In short, moving away from the center can lock within the current buffer, while moving toward the other side locks only after crossing the center split.

Distance-based repeat applies to the left-half left/right Alt-Tab entry gesture after the first trigger. The first left/right trigger enters the Alt-Tab switcher once with `Alt + Shift + Tab` or `Alt + Tab`, then only `Alt` stays held while the finger remains on the touchpad. After the switcher is open, each additional movement segment sends the matching arrow key: 180 left/right sends `←`/`→`, and 150 up/down sends `↑`/`↓`. Reversing does not use Shift; it sends the reverse arrow. If one state update jumps across multiple complete segments, ShikiPad sends one arrow per extra segment after the initial Alt-Tab entry and keeps the leftover distance for the next arrow.

Time-based repeat applies to left-half one-finger direct up/down window switching, right-half one-finger direct left/right desktop switching, and right-half two-finger direct up/down tab switching. The first trigger uses the same direction thresholds: 150 for up/down and 180 for left/right. Left-half up/down and right-half two-finger up/down tab switching use `TouchGestureTimeRepeatIntervalMs`, currently 450 ms. Right-half one-finger left/right desktop switching uses `TouchGestureDesktopRepeatIntervalMs`, currently 550 ms. Continuing in the same direction by that direction's repeat distance does not fire an extra shortcut; it only refreshes the origin used for reverse detection. Reversing without lifting by 150 vertically or 180 horizontally from that latest origin immediately fires the reverse shortcut, then the reverse direction continues with the same fixed interval for that shortcut. Hold-then-swipe shortcuts, right-half one-finger up/down `Win + ↑/↓`, two-finger left-half shortcuts, and two-finger right-half horizontal `Alt + ←/→` shortcuts do not repeat.

### Touchpad Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `TouchGestureHoldStillDistance` | 50 | Maximum movement that still counts as staying still; moving farther records the movement start time |
| `TouchGestureVerticalThreshold` | 150 | Distance required to recognize and trigger an up/down swipe |
| `TouchGestureHorizontalThreshold` | 180 | Distance required to recognize and trigger a left/right swipe |
| `TouchGestureVerticalRepeatDistance` | 150 | Movement distance required for each vertical repeat/reverse/navigation trigger |
| `TouchGestureHorizontalRepeatDistance` | 180 | Movement distance required for each horizontal repeat/reverse/navigation trigger |
| `TouchGestureTimeRepeatDelayMs` | 450 ms | Initial delay before left-half up/down time repeat starts |
| `TouchGestureTimeRepeatIntervalMs` | 450 ms | Fixed interval for left-half up/down time repeat after either forward or reverse trigger |
| `TouchGestureDesktopRepeatIntervalMs` | 550 ms | Initial delay and fixed interval for right-half left/right desktop switching repeat |
| `TouchGestureSideConfirmedWidth` | 550 | Width of each left/right confirmed zone; the 550..959 and 960..1369 middle buffers lock by crossing the center split |
| `TouchGestureHoldMs` | 450 ms | Touch duration required at the 150/180 trigger moment to count as hold-then-swipe |

## Voice Input

If controller typing still feels difficult, pair ShikiPad with voice input software such as Typeless or Shandian Shuo. DualSense is especially convenient here: Create maps to `Right Alt`, Options maps to `Right Ctrl`, Home handles clutch, Mute short-tap toggles the one-shot Caps/Fn layer, and Mute long-press toggles ShikiPad enabled / disabled.

## Left Stick

| Direction | Output |
|---|---|
| Upper-left | `Left Shift` |
| Lower-left | `Ctrl` |
| Upper-right | `Win` |
| Lower-right | `Left Alt` |
| Up / Down center sectors | Mouse wheel |

The left stick is divided into six sectors: three in the upper half and three in the lower half. The pure left/right sectors are removed.

Left-stick wheel speed is continuous by radius while the current sector is Up/Down. Up/Down wheel sectors enter at `LeftStickEnterDeadzone`, currently 0.15. Moving into a modifier sector immediately stops wheel output; that modifier starts only after the stick reaches `LeftStickModifierEnterDeadzone`, currently 0.45. Moving back into Up/Down resumes wheel output from the current radius once the wheel threshold is met. Distance from center drives an exponent-3.0 speed curve from the 1500 ms slow floor to the 15 ms fast ceiling.

Left-stick modifier sectors are immediate, not locked. The 360 degrees are still divided into six equal 60-degree sectors, but the radial deadzone is sector-specific: wheel sectors enter at 0.15, and modifier sectors enter at 0.45. ShikiPad follows the current sector every frame: moving from `Left Shift` to `Win`, `Ctrl`, `Left Alt`, or wheel sectors releases the previous output and applies the current one only while that target sector reaches its own threshold. If the target sector is below its threshold, left-stick output is neutral.

### Left-Stick Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `LeftStickEnterDeadzone` | 0.15 | Radius needed to enter an Up/Down wheel sector; the wheel accumulator resets below this same threshold |
| `LeftStickModifierEnterDeadzone` | 0.45 | Radius needed to enter one of the four modifier sectors |
| `MouseScrollCurveExponent` | 3.0 | Left-stick wheel radius curve exponent |
| `MouseScrollSmoothingMs` | 5 ms | Short exponential smoothing on the normalized wheel radius before the scroll curve |
| `ScrollSlowIntervalMs` | 1500 ms | Slowest wheel interval |
| `ScrollFastIntervalMs` | 15 ms | Fastest wheel interval |
| `WheelDelta` | 120 | One standard wheel detent |
| `WheelRoundingThreshold` | 0.5 | Same idea as right-stick mouse rounding: fractional wheel amount rounds to an integer once it reaches half a unit |
| `MaxWheelDeltaPerFrame` | 120 | Maximum wheel output per frame |

The left-stick wheel now follows the right-stick mouse integration idea more closely. Radius is normalized as `(radius - LeftStickEnterDeadzone) / (1 - LeftStickEnterDeadzone)`, the normalized radius receives the same short 5 ms style of smoothing, then `power = normalized ^ MouseScrollCurveExponent`. Maximum speed is `WheelDelta * 1000 / ScrollFastIntervalMs`; current speed is `maximum speed * power`, with a slow floor of `WheelDelta * 1000 / ScrollSlowIntervalMs`. Fractional wheel amount accumulates each frame and rounds to an integer after it reaches 0.5, just like right-stick pixel movement, capped at 120 per frame.

## Clutch

Normally, the left stick holds one modifier at a time. Clutch collects multiple modifiers and releases them together.

While clutch is active, the currently collected modifiers remain held even as the left stick moves elsewhere. To add another modifier, move directly into its sector; to use wheel input, move into the Up/Down sector. Home is only a clutch key and no longer becomes real `Left Shift` when no modifier is active. A short-tap clutch lock records whether it can be consumed at the moment the lock is formed: if at least one modifier has already been collected then, the next action key releases that short-tap lock after firing; if no modifier has been collected then, later modifier collection does not make that same lock action-consumable, and it must be cancelled with another short tap. Long-press clutch still holds while pressed and releases on button up. Action buttons keep their normal mappings while clutch modifiers are held. Pressing a normal mapped `1` still sends `1`, not `F1`.

Mute provides the controller Caps/Fn layer. Short-tap Mute toggles one-shot Caps/Fn on or off; pressing it again before an action key cancels the layer and restores normal output. While active, unshifted action mappings `1..0`, `-`, and `=` become `F1..F12`; unshifted letters are sent as shifted uppercase letters instead of their normal lowercase output. The next action key always clears Caps/Fn. Other keys keep their normal mapping. Long-press Mute uses the same timing as Home clutch, `ClutchLongPressMs`, and toggles ShikiPad enabled / disabled.

Touchpad middle-buffer click taps real system `Caps Lock`, so the keyboard indicator follows it. It does not enable Fn and does not participate in clutch release.

| Controller | Activate / hold |
|---|---|
| DualSense | Short-tap Home to toggle clutch, or long-press Home to hold clutch until release; action keys consume a short-tap clutch only if at least one modifier was already collected when that short-tap lock formed |

### Clutch Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `ClutchLongPressMs` | 250 ms | Long-press time for holding clutch on Home and for Mute long-press enable / disable |

## Typing Layers

The v3 release maps letters around familiar keyboard positions. This keeps layouts such as `WASD` and `IJKL` recognizable instead of sorting every letter purely by frequency.

The columns in the following tables correspond to: `↑`, `→`, `□`, `△`, `←`, `↓`, `×`, `○`.

| Layer | ↑ | → | □ | △ | ← | ↓ | × | ○ |
|---|---|---|---|---|---|---|---|---|
| Base | ↑ | → | Tab | Esc | ← | ↓ | Space | Enter |
| R1 | o | p | j | i | n | m | k | l |
| L1 | w | d | q | e | a | s | z | x |
| R2 | 0 | g | y | u | - | = | b | h |
| L2 | r | f | t | 1 | c | v | 3 | 2 |
| R1 + L1 | 4 | , | . | 7 | 5 | 6 | 9 | 8 |
| L2 + R2 | + | / | & | * | _ | ^ | $ | % |
| L1 + R2 | [ | ] | ! | ? | { | } | @ | # |
| R1 + L2 | ( | ) | ; | ' | < | > | backtick | \ |

The program sends physical keycodes. Characters requiring Shift (", :, |, ~) are shifted automatically by the corresponding layer entries.

Base-layer D-pad keys repeat while held. Base-layer face buttons (`Square`, `Triangle`, `Cross`, `Circle`) do not repeat. Character layers are virtual taps: one press sends one key stroke, and holding does not repeat. Once an action key has resolved to a layer and is held, later shoulder/trigger changes do not reassign that held physical key until it is released.

### Base Repeat Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `RepeatDelayMs` | 300 ms | Delay after the first repeatable base-layer or touchpad `Delete` / `Backspace` press before repeat starts |
| `BaseRepeatSlowIntervalMs` | 120 ms | Starting repeat interval |
| `BaseRepeatRampMs` | 1500 ms | Time to ramp from slow repeat to fastest repeat |
| `RepeatIntervalMs` | 12 ms | Fastest repeat interval |
| Repeat curve exponent | 3.0 | Cubic acceleration over frequency; the ramp segment is continuous |

If multiple layer buttons are pressed on the same polling timestamp, tie priority is `R1 > L1 > R2 > L2`. Combo formation still only considers the latest two active layer buttons after that priority sort.

### Layer Timing Parameters

ShikiPad uses short time windows to absorb human input errors when typing quickly.

| Parameter | Current | Purpose |
|---|---:|---|
| `ComboLayerWindowMs` | 35 ms | Maximum interval for two layer buttons to form a combo layer |
| `ActionLayerGraceMs` | 45 ms | Grace window between action key and layer recognition |
| `ActionLayerPostGraceMs` | 15 ms | Grace window after a layer is released before a new layer is pressed; releases from a layer input that overlapped another layer input do not get this post-grace window |
| `LayerTakeoverWindowMs` | 30 ms | Cumulative body cap; after the 20 ms cutoff lands inside a boundary old layer body, backward tracing can continue only until cumulative body occupancy reaches 30 ms |
| `LayerOccupancyCarryCutoffMs` | 20 ms | Cumulative body cutoff for backward layer tracing; total lookback is still `ActionLayerGraceMs`, but once cumulative body occupancy reaches this cutoff, tracing can continue only inside the current boundary body up to `LayerTakeoverWindowMs` and cannot cross into its pre-window or older layers |

Combo layers are treated as their own layers: the same physical single-key press that helps form a combo still occupies the 35 ms timeline, but it does not count as that combo layer's own body accumulation and cannot trigger that combo layer's 20 ms / 30 ms body limits.

## Troubleshooting

### No keyboard or mouse output

Install Interception, restart Windows, and run `ShikiPad.exe` as administrator.

### System or game double input

If Windows still sees the physical controller while ShikiPad is running, the same stick movement can be handled twice: once by ShikiPad and once by Windows or the focused app. Typical symptoms include left-stick `Alt` plus `Tab` jumping unpredictably between windows, or the left-stick `Win` modifier failing because Windows treats controller input as Start menu, taskbar, or app icon navigation. Configure HidHide as described above so only ShikiPad can see the DualSense controller.

If HidHide is already configured as described above but double input or Windows input stealing still happens, try placing the whole ShikiPad folder at the root of the C drive, for example `C:\ShikiPad`. This is the location currently used by the author.
