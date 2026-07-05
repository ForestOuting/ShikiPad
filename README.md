# ShikiPad

ShikiPad is a native Windows controller-to-keyboard and mouse mapper. It works best with PlayStation controllers. DualSense and DualShock 4 expose the full ShikiPad feature set, while Xbox controllers work through XInput and are limited by Windows system behavior.

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

## Controller Choice

Use a DualSense or DualShock 4 controller when possible. This gives ShikiPad the most complete input surface, including Sony-specific buttons such as Home, Create/Share, Options/Menu, and DualSense Mute.

Many third-party controllers support multiple modes. If yours can switch modes, PS4 / DualShock 4 mode is recommended for ShikiPad. Xbox mode can still work for normal mapping, but Windows owns parts of the Xbox stack. In particular, the Xbox Guide/Home button cannot be read through XInput, and HidHide usually cannot hide Xbox controllers at the HID device layer.

ShikiPad now requires Interception for keyboard and mouse output. This does not stop Xbox controllers from being read, because Xbox input is read through XInput and output is sent through Interception. The practical limitation is not Interception, but the Windows XInput/Xbox stack.

## Driver And HidHide Order

Recommended setup order:

1. Connect the controller once and confirm Windows can see it.
2. Install Interception with `install_driver.bat` as administrator.
3. Restart Windows.
4. Run `ShikiPad.exe` as administrator and confirm it can send keyboard and mouse output.
5. For PlayStation controllers, configure HidHide to prevent system or game double input.
6. Unplug and reconnect the controller after HidHide changes.

HidHide setup for PlayStation controllers:

1. In `Applications`, add the exact path to `ShikiPad.exe`.
2. Uncheck `Inverse application cloak`.
3. In `Devices`, check the target PlayStation controller. A red lock icon should appear.
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
| `shikipad.default` | Default controller profile |
| `shiki.ico` | Program icon |
| `ShikiPad.manifest` | Windows application manifest |

## Install

1. Extract the release archive.
2. Right-click `install_driver.bat` and choose `Run as administrator`.
3. Restart Windows after `Installation complete!`.
4. Run `ShikiPad.exe` as administrator.
5. Type `1` to `8` to select a controller profile. You may save it as the default launch profile.

## Console Pages

| Page | Enter | Esc |
|---|---|---|
| Controller selection | Confirm selection | Exit |
| Home | Open mapping manual | Return to controller selection |
| Mapping manual | Return home | Exit ShikiPad |

Closing the console window also exits and releases held keyboard and mouse inputs.

## Mouse And System Buttons

| Controller input | Output |
|---|---|
| Right stick | Mouse movement |
| L3 | Left mouse button |
| R3 | Right mouse button with a short cursor freeze on press |
| Left stick up / down | Mouse wheel |
| Sony Create / Share | `Right Alt` |
| Sony Options / Menu | `Right Ctrl` |
| Sony Home | `Right Shift` |
| DualSense Mute | `Caps Lock` |
| Share/Create/View + Options/Menu for about 1 second | Enable / disable ShikiPad |

### Mouse Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `MouseSensitivity` | 1.0 | Overall right-stick mouse sensitivity |
| `MouseMaxSpeed` | 20.0 | Base maximum speed factor at full right-stick tilt |
| `RightStickDeadzone` | 0.015 | Right-stick mouse deadzone |
| `RightStickCurve` | power | Right-stick curve type; the current implementation uses a power curve |
| `RightStickCurveExponent` | 3.0 | Right-stick radius curve exponent |
| Mouse frame multiplier | 120.0 | Internal multiplier in the right-stick velocity formula |
| Mouse rounding threshold | 0.5 px | Fractional mouse movement is emitted once it reaches half a pixel |
| `MaxMouseFrameSeconds` | 0.05 s | Per-frame mouse integration cap to prevent large jumps after a stalled frame |
| `R3FreezeMs` | 60 ms | Cursor freeze duration when R3 starts a right click |

The right stick uses continuous velocity integration: `radius = sqrt(x*x + y*y)`, `normalized = (radius - RightStickDeadzone) / (1 - RightStickDeadzone)`, `power = normalized ^ RightStickCurveExponent`, and per-frame movement is `direction * power * MouseMaxSpeed * deltaSec * 120 * MouseSensitivity`. X/Y fractional pixels are accumulated separately, rounded to integer pixels once they reach 0.5px, and the remainder is kept.

## Touchpad Gestures

Touchpad gestures are available on PlayStation controllers. The current rule is lenient: if two fingers appear at any point during the gesture, the gesture uses the two-finger map; only gestures that never have two fingers use the one-finger map. Direction no longer uses the center point. Instead, once any active finger moves from its own start point by `TouchGestureThreshold`, ShikiPad recognizes that finger's largest left / right / up / down component. No shortcut fires before that threshold is reached.

### Touchpad Mappings

| Gesture | Up | Down | Left | Right |
|---|---|---|---|---|
| One-finger direct swipe | `Alt + Shift + Esc` previous window | `Alt + Esc` next window | `Win + Ctrl + ←` previous window | `Win + Ctrl + →` next window |
| One-finger hold-then-swipe | `Home` | `End` | `Alt + F4` close app | `Shift + Win + S` screenshot |
| Two-finger direct swipe | `Ctrl + Shift + Tab` previous tab | `Ctrl + Tab` next tab | `Win + Shift + ←` move to left monitor | `Win + Shift + →` move to right monitor |
| Two-finger hold-then-swipe | `Ctrl + Shift + Esc` control panel | Empty | Empty | Empty |

All touchpad gestures repeat after recognition except one-finger hold-left close app, one-finger hold-right screenshot, two-finger direct left/right monitor moves, two-finger hold-up control panel, and empty two-finger hold down/left/right gestures. After the first shortcut fires, ShikiPad waits `TouchGestureRepeatDelayMs`, then repeats at `TouchGestureRepeatMs`; repeat hold only requires at least one finger to remain on the touchpad.

### Touchpad Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `TouchGestureMoveStartThreshold` | 50 | Distance that marks the finger as moving; this only changes state and does not fire a shortcut |
| `TouchGestureThreshold` | 250 | Direction distance required to recognize a swipe |
| `TouchGestureHoldMs` | 150 ms | If recognition happens after this much time from touch start, the gesture uses the hold-then-swipe map |
| `TouchGestureRepeatDelayMs` | 550 ms | Delay between the initial gesture shortcut and the first repeat |
| `TouchGestureRepeatMs` | 350 ms | Repeat interval while the recognized gesture remains held |

## Voice Input

If controller typing still feels difficult, pair ShikiPad with voice input software such as Typeless or Shandian Shuo. PlayStation controllers are especially convenient here: Share maps to `Right Alt`, Options/Menu maps to `Right Ctrl`, and Home maps to `Right Shift`, so voice input shortcuts can be triggered without leaving the controller. The built-in microphone on supported PlayStation controllers also sits close to your mouth and can produce good recognition results in a quiet room.

## Left Stick

| Direction | Output |
|---|---|
| Left | `Shift` |
| Down-left | `Ctrl` |
| Down-right | `Left Alt` |
| Right | `Win` |
| Up-left | `Esc` |
| Up-right | Fn layer |
| Up / Down | Mouse wheel |

Fn turns number-row keys into `F1` to `F12`: `1..0` map to `F1..F10`, `-` maps to `F11`, and `=` maps to `F12`.

Left-stick wheel speed is continuous by radius: once the stick first enters the Up/Down sector, wheel mode uses the current vertical axis for wheel direction, so small drifts into diagonal up/down sectors do not interrupt scrolling. Distance from center drives an exponent-3.0 speed curve from the 1500 ms slow floor to the 15 ms fast ceiling.

When the left stick first enters a non-wheel functional sector, ShikiPad keeps that modifier intent until the stick returns below the exit deadzone. Wheel output uses the current Up/Down sector for direction, while wheel speed still follows the current radius continuously.

### Left-Stick Wheel Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `LeftStickEnterDeadzone` | 0.25 | Radius needed to enter a left-stick functional sector |
| `LeftStickExitDeadzone` | 0.15 | Radius below which left-stick wheel/modifier intent resets |
| `MouseScrollCurveExponent` | 3.0 | Left-stick wheel radius curve exponent |
| `ScrollSlowIntervalMs` | 1500 ms | Slowest wheel interval |
| `ScrollFastIntervalMs` | 15 ms | Fastest wheel interval |
| `WheelDelta` | 120 | One standard wheel detent |
| `WheelRoundingThreshold` | 0.5 | Same idea as right-stick mouse rounding: fractional wheel amount rounds to an integer once it reaches half a unit |
| `MaxWheelDeltaPerFrame` | 120 | Maximum wheel output per frame |

The left-stick wheel now follows the right-stick mouse integration idea more closely. Radius is normalized as `(radius - LeftStickEnterDeadzone) / (1 - LeftStickEnterDeadzone)`, then `power = normalized ^ MouseScrollCurveExponent`. Maximum speed is `WheelDelta * 1000 / ScrollFastIntervalMs`; current speed is `maximum speed * power`, with a slow floor of `WheelDelta * 1000 / ScrollSlowIntervalMs`. Fractional wheel amount accumulates each frame and rounds to an integer after it reaches 0.5, just like right-stick pixel movement, capped at 120 per frame.

## Clutch

Normally, the left stick holds one modifier at a time. Clutch collects multiple modifiers and releases them together.

While clutch is active, the currently collected modifiers remain held even if the left stick moves away from their original direction. This lets you keep a modifier, move the stick up or down for wheel input, or move to another modifier direction to add it to the stack. A short-tap clutch lock releases automatically after any action button actually sends a key; if no action key has fired yet, short-tap again to cancel the lock. Long-press clutch still holds while pressed and releases on button up.

| Controller | Activate / hold |
|---|---|
| DualSense / DualShock 4 | Short-tap Touchpad to lock until the next action key, short-tap again to cancel, or long-press to hold |
| Xbox | Short-tap View/Back or Menu/Start to lock until the next action key, short-tap again to cancel, or long-press to hold |

### Clutch Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `ClutchLongPressMs` | 250 ms | Long-press time for holding clutch on Touchpad / View / Menu |

## Typing Layers

The v3 release maps letters around familiar keyboard positions. This keeps layouts such as `WASD` and `IJKL` recognizable instead of sorting every letter purely by frequency.

The columns in the following tables correspond to: `↑`, `→`, `□/X`, `△/Y`, `←`, `↓`, `×/A`, `○/B`.

| Layer | ↑ | → | □/X | △/Y | ← | ↓ | ×/A | ○/B |
|---|---|---|---|---|---|---|---|---|
| Base | ↑ | → | Space | Backspace | ← | ↓ | Enter | Tab |
| R1 / RB | o | p | j | i | n | m | k | l |
| L1 / LB | w | d | q | e | a | s | z | x |
| R2 / RT | 0 | g | y | u | - | = | b | h |
| L2 / LT | r | f | t | 1 | c | v | 3 | 2 |
| R1 + L1 | 4 | , | . | 7 | 5 | 6 | 9 | 8 |
| L2 + R2 | + | / | & | * | _ | ^ | $ | % |
| L1 + R2 | [ | ] | ! | ? | { | } | @ | # |
| R1 + L2 | ( | ) | ; | ' | < | > | backtick | \ |

The program sends physical keycodes. Characters requiring Shift (", :, |, ~) can be entered by holding the left stick `Shift` direction and pressing the corresponding base key (', ;, \, backtick).

Base-layer keys repeat while held. Character layers are virtual taps: one press sends one key stroke, and holding does not repeat.

### Base Repeat Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `RepeatDelayMs` | 300 ms | Delay after the initial base-layer press before repeat starts |
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
| `ActionLayerGraceMs` | 35 ms | Grace window between action key and layer recognition |
| `ActionLayerPostGraceMs` | 15 ms | Grace window after a layer is released before a new layer is pressed |
| `LayerTakeoverWindowMs` | 25 ms | Cumulative body cap; after the 15 ms cutoff lands inside a boundary old layer body, backward tracing can continue only until cumulative body occupancy reaches 25 ms |
| `LayerOccupancyCarryCutoffMs` | 15 ms | Cumulative body cutoff for backward layer tracing; total lookback is still `ActionLayerGraceMs`, but once cumulative body occupancy reaches this cutoff, tracing can continue only inside the current boundary body up to `LayerTakeoverWindowMs` and cannot cross into its pre-window or older layers |
| `ActionLayerSwitchGuardMs` | 35 ms | Suppress residual mis-touches when switching layers after a character is typed |

Combo layers are treated as their own layers: the same physical single-key press that helps form a combo still occupies the 35 ms timeline, but it does not count as that combo layer's own body accumulation and cannot trigger that combo layer's 15 ms / 25 ms body limits.

## Troubleshooting

### No keyboard or mouse output

Install Interception, restart Windows, and run `ShikiPad.exe` as administrator.

### System or game double input

If Windows still sees the physical controller while ShikiPad is running, the same stick movement can be handled twice: once by ShikiPad and once by Windows or the focused app. Typical symptoms include left-stick `Alt` plus `Tab` jumping unpredictably between windows, or the left-stick `Win` modifier failing because Windows treats controller input as Start menu, taskbar, or app icon navigation. Configure HidHide as described above so only ShikiPad can see the PlayStation controller. Xbox controllers use XInput, so HidHide usually cannot hide them at the HID layer.

If HidHide is already configured as described above but double input or Windows input stealing still happens, try placing the whole ShikiPad folder at the root of the C drive, for example `C:\ShikiPad`. This is the location currently used by the author.
