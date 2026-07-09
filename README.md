# ShikiPad

ShikiPad is a native Windows controller-to-keyboard and mouse mapper. It works best with PlayStation controllers. DualSense and DualShock 4 expose the full ShikiPad feature set, while Xbox controllers work through XInput and are limited by Windows system behavior.

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

## Controller Choice

Use a DualSense or DualShock 4 controller when possible. This gives ShikiPad the most complete input surface, including Sony-specific buttons such as Home, Create/Share, Options/Menu, and DualSense Mute.

Many third-party controllers support multiple modes. If yours can switch modes, PS4 / DualShock 4 mode is recommended for ShikiPad. Xbox mode can still work for normal mapping, but Windows owns parts of the Xbox stack. In particular, the Xbox Guide/Home button cannot be read through XInput, and HidHide usually cannot hide Xbox controllers at the HID device layer.

ShikiPad now requires Interception for keyboard and mouse output. This does not stop Xbox controllers from being read, because Xbox input is read through XInput and output is sent through Interception. The practical limitation is not Interception, but the Windows XInput/Xbox stack.

Mode capability summary:

| Mode | ShikiPad support | Limits |
|---|---|---|
| DualSense USB | Full PlayStation feature set: buttons, sticks, triggers, Home, touchpad click/gestures, Create/Options, DualSense Mute | Recommended when Bluetooth is unreliable |
| DualSense Bluetooth | Full only when Windows delivers the enhanced `0x31` HID report | If the system or Bluetooth stack only exposes the simple `0x01` report, ShikiPad can read normal buttons/sticks/triggers/Home but cannot read touchpad coordinates or DualSense Mute; use USB, reconnect the controller, or update/change the Bluetooth adapter/driver |
| DualShock 4 USB / Bluetooth | Buttons, sticks, triggers, Home, touchpad click/gestures, Share/Options | No DualSense Mute key |
| Xbox / XInput | Normal XInput buttons, sticks, triggers, View/Back, Menu/Start | No touchpad; Guide/Home is usually reserved by Windows and cannot be read through XInput; HidHide usually cannot hide XInput devices at the HID layer |

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
| Sony Create / Share, Xbox View / Back | `Right Alt` |
| Sony Options / Menu, Xbox Menu / Start | `Right Ctrl` |
| Press DualSense Mute | Enable / disable ShikiPad |
| Any touch point in the left confirmed zone during touchpad click | `Delete` |
| Any touch point in the right confirmed zone during touchpad click | `Backspace` |
| All touch points in the middle buffer during touchpad click | Toggle real `Caps Lock` plus controller Fn layer |

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

Touchpad gestures are available on PlayStation controllers. Only one-finger direct swipe maps are active for now. Two-finger recognition only applies before a gesture has been recognized; if two touch points are active before recognition, the gesture counts as two-finger and, because two-finger gestures currently have no shortcuts, ShikiPad blocks it until release. Once a one-finger gesture has been recognized, later extra touch points are ignored so the locked one-finger gesture stays stable. A finger must move more than `TouchGestureHoldStillDistance` from its start point before ShikiPad treats it as moving; if it stayed still for `TouchGestureHoldMs` before that, the gesture counts as hold-then-swipe and does not fire the direct-swipe shortcuts. Direction no longer uses the center point. Instead, vertical gestures trigger at 150 distance and horizontal gestures trigger at 180 distance. Touchpad side is decided only at first recognition: the leftmost and rightmost `TouchGestureSideConfirmedWidth` pixels are confirmed zones, so starts in the left confirmed zone lock left and starts in the right confirmed zone lock right. Starts in the middle buffer lock by crossing the center split: right-buffer-to-left-buffer motion locks left, and left-buffer-to-right-buffer motion locks right. Distance moved inside the buffer before side lock still counts toward that direction's first trigger. For distance-repeat gestures, first recognition also consumes every complete direction-specific segment and then continues counting from the last consumed trigger point. After recognition, the side stays locked until touch release and the whole touchpad remains valid movement space for that gesture.

### Touchpad Mappings

| Gesture | Up | Down | Left | Right |
|---|---|---|---|---|
| Left-half one-finger direct swipe | `Alt + Shift + Esc` previous window | `Alt + Esc` next window | Enter Alt-Tab with `Alt + Shift + Tab`, then hold `Alt` | Enter Alt-Tab with `Alt + Tab`, then hold `Alt` |
| Right-half one-finger direct swipe | `Win + ↑` maximize | `Win + ↓` restore/minimize | `Win + Ctrl + ←` previous desktop | `Win + Ctrl + →` next desktop |

Touchpad click is not the clutch key. It checks the active touch point X positions when the click begins. Any touch point in the left confirmed zone sends `Delete`, any touch point in the right confirmed zone sends `Backspace`, and if active touch points are simultaneously in both confirmed zones, `Backspace` wins. Only when all active touch points are in the middle buffer does the click toggle real `Caps Lock` plus the controller Fn layer. Touchpad `Delete` and `Backspace` count as base-layer action keys while Home is in clutch mode, so they clear a short-tap clutch lock after firing. When Home is being held as a real `Left Shift`, touchpad `Delete` / `Backspace` naturally become shifted combinations such as `Shift + Delete`. Touchpad `Delete` and `Backspace` use the same progressive repeat timing as base-layer repeat. The middle-buffer Caps/Fn click is a separate layer toggle: it is not a base-layer action key, fires once on click-down, and does not repeat. Xbox controllers have no touchpad, so this function is skipped.

Every one-finger direct swipe first trigger requires movement from that touch point's start by the direction threshold: 150 for up/down and 180 for left/right. If the touch starts in the middle buffer, distance moved before side lock still counts toward the horizontal 180. After the side locks and the shortcut fires, that trigger point becomes the new origin for later repeat or reverse checks.

Middle-buffer horizontal recognition is not a fixed decision based only on the start buffer. If the touch starts in the left buffer, `550..959`, moving left by 180 immediately locks left and fires the left-side shortcut. Moving right by 180 while still inside the left buffer does not lock right; it waits until the touch enters the right buffer, `960..1369`, then locks right and fires the right-side shortcut once. The right buffer is symmetric: moving right by 180 locks right immediately, while moving left must enter the left buffer before it locks left. In short, moving away from the center can lock within the current buffer, while moving toward the other side locks only after crossing the center split.

Distance-based repeat applies to the left-half left/right Alt-Tab entry gesture after the first trigger. The first left/right trigger enters the Alt-Tab switcher once with `Alt + Shift + Tab` or `Alt + Tab`, then only `Alt` stays held while the finger remains on the touchpad. After the switcher is open, each additional movement segment sends the matching arrow key: 180 left/right sends `←`/`→`, and 150 up/down sends `↑`/`↓`. Reversing does not use Shift; it sends the reverse arrow. If one state update jumps across multiple complete segments, ShikiPad sends one arrow per extra segment after the initial Alt-Tab entry and keeps the leftover distance for the next arrow.

Time-based repeat applies to left-half up/down window switching and right-half left/right desktop switching. The first trigger uses the same direction thresholds: 150 for up/down and 180 for left/right. Left-half up/down fires every `TouchGestureTimeRepeatIntervalMs`, currently 450 ms. Right-half left/right desktop switching uses `TouchGestureDesktopRepeatIntervalMs`, currently 550 ms. Continuing in the same direction by that direction's repeat distance does not fire an extra shortcut; it only refreshes the origin used for reverse detection. Reversing without lifting by 150 vertically or 180 horizontally from that latest origin immediately fires the reverse shortcut, then the reverse direction continues with the same fixed interval for that shortcut. Right-half up/down `Win + ↑/↓` shortcuts do not repeat.

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
| `TouchGestureHoldMs` | 600 ms | Time the finger must stay still before movement starts to count as hold-then-swipe; hold-then-swipe gestures are currently unmapped |

## Voice Input

If controller typing still feels difficult, pair ShikiPad with voice input software such as Typeless or Shandian Shuo. PlayStation controllers are especially convenient here: Share maps to `Right Alt`, Options/Menu maps to `Right Ctrl`, Home handles clutch or real `Left Shift` depending on the press state, and pressing DualSense Mute toggles ShikiPad enabled / disabled. The built-in microphone on supported PlayStation controllers also sits close to your mouth and can produce good recognition results in a quiet room.

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

While clutch is active, the currently collected modifiers remain held even as the left stick moves elsewhere. To add another modifier, move directly into its sector; to use wheel input, move into the Up/Down sector. A short-tap clutch lock releases automatically after any action key actually sends a key, including touchpad-confirmed base-layer `Delete` / `Backspace`; if no action key has fired yet, short-tap again to cancel the lock. Long-press clutch still holds while pressed and releases on button up.

Home decides its mode when Home is pressed. If no left-stick modifier is already active at that moment, Home becomes a real `Left Shift` key for that press and releases `Left Shift` when Home is released. While this Home-as-Shift mode is active, left-stick modifiers do not output. After Home is released, the current left-stick sector can take effect immediately.

If a left-stick modifier is already active when Home is pressed, Home follows the pure clutch logic: short-tap locks the collected modifiers until the next action key, short-tap again cancels if no action has fired, and long-press holds the collected modifiers until Home is released. Action buttons keep their normal mappings while clutch modifiers are held. Pressing a normal mapped `1` still sends `1`, not `F1`.

Touchpad middle-buffer click toggles real system `Caps Lock`, so the keyboard indicator follows it. ShikiPad also tracks this as the controller Fn layer: while it is active, unshifted action mappings `1..0`, `-`, and `=` become `F1..F12`; after one such Fn action fires, ShikiPad restores `Caps Lock` to the state it had before the controller Caps/Fn layer was opened and clears the layer. Clutch release remains governed only by Home: short-tap clutch releases after an action key fires, while long-press clutch releases when Home is released. Other action keys are sent normally while real `Caps Lock` is active and do not clear the controller Fn layer. Press the touchpad middle buffer again to cancel and restore `Caps Lock` manually. This controller Fn layer resolves before Home clutch output, so Caps/Fn layer plus Home-collected modifiers can produce shortcuts such as `Win + Alt + F4`.

| Controller | Activate / hold |
|---|---|
| DualSense / DualShock 4 | Hold a left-stick modifier first, then short-tap Home to lock until the next action key, short-tap again to cancel, or long-press Home to hold; press Home without an active modifier for real `Left Shift` |
| Xbox | Guide/Home is usually unavailable through XInput, so this function is skipped |

### Clutch Parameters

| Parameter | Current | Purpose |
|---|---:|---|
| `ClutchLongPressMs` | 250 ms | Long-press time for holding clutch on Home |

## Typing Layers

The v3 release maps letters around familiar keyboard positions. This keeps layouts such as `WASD` and `IJKL` recognizable instead of sorting every letter purely by frequency.

The columns in the following tables correspond to: `↑`, `→`, `□/X`, `△/Y`, `←`, `↓`, `×/A`, `○/B`.

| Layer | ↑ | → | □/X | △/Y | ← | ↓ | ×/A | ○/B |
|---|---|---|---|---|---|---|---|---|
| Base | ↑ | → | Tab | Esc | ← | ↓ | Space | Enter |
| R1 / RB | o | p | j | i | n | m | k | l |
| L1 / LB | w | d | q | e | a | s | z | x |
| R2 / RT | 0 | g | y | u | - | = | b | h |
| L2 / LT | r | f | t | 1 | c | v | 3 | 2 |
| R1 + L1 | 4 | , | . | 7 | 5 | 6 | 9 | 8 |
| L2 + R2 | + | / | & | * | _ | ^ | $ | % |
| L1 + R2 | [ | ] | ! | ? | { | } | @ | # |
| R1 + L2 | ( | ) | ; | ' | < | > | backtick | \ |

The program sends physical keycodes. Characters requiring Shift (", :, |, ~) are shifted automatically by the corresponding layer entries; on PlayStation controllers, pressing Home without an active left-stick modifier holds a real `Left Shift`.

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

If Windows still sees the physical controller while ShikiPad is running, the same stick movement can be handled twice: once by ShikiPad and once by Windows or the focused app. Typical symptoms include left-stick `Alt` plus `Tab` jumping unpredictably between windows, or the left-stick `Win` modifier failing because Windows treats controller input as Start menu, taskbar, or app icon navigation. Configure HidHide as described above so only ShikiPad can see the PlayStation controller. Xbox controllers use XInput, so HidHide usually cannot hide them at the HID layer.

If HidHide is already configured as described above but double input or Windows input stealing still happens, try placing the whole ShikiPad folder at the root of the C drive, for example `C:\ShikiPad`. This is the location currently used by the author.
