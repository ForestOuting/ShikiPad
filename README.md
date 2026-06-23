# ShikiPad

ShikiPad is a native Windows controller-to-keyboard and mouse mapper built for fast gamepad typing. It maps DualSense, DualShock 4, Xbox 360, and Xbox Series X|S controllers to keyboard keys, mouse movement, clicks, scroll wheel input, and multi-key shortcuts.

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

## Highlights

- Full English alphabet, digits, and common punctuation through shoulder/trigger layers.
- Right-stick mouse movement with smooth power-curve acceleration.
- Left-stick scroll wheel, modifier keys, Windows key, Escape, and Fn layer.
- Clutch system for multi-modifier shortcuts such as `Ctrl + Shift + Esc`.
- Interception driver support for low-level keyboard and mouse injection.

## Release Package

A formal release archive should contain:

| File | Purpose |
|---|---|
| `ShikiPad.exe` | Main application |
| `install_driver.bat` | Administrator helper for installing the Interception driver |
| `interception.dll` | Interception runtime DLL used by ShikiPad |
| `driver/install-interception.exe` | Interception driver installer |
| `README.md` / `README.zh-CN.md` | English and Chinese manuals |

ShikiPad may create one optional runtime file, `shikipad.default`, to remember the default launch controller. It is not part of the release package and does not change mapping parameters.

## Install And First Run

1. Extract the release archive to a normal folder.
2. Right-click `install_driver.bat` and choose `Run as administrator`.
3. Wait for `Installation complete!`, then restart Windows. The driver is not active until after reboot.
4. Run `ShikiPad.exe`. It requests administrator rights because low-level input injection needs elevated access.
5. Select your controller profile in the console. After selection, you can save it as the default launch profile so future starts do not require manual input.

## Interface Pages

The ShikiPad console uses three page types:

| Page | When it appears | What to do |
|---|---|---|
| Initial controller page | No default controller exists, or you return from the home screen with `Esc` | Type `1` through `8`, then press `Enter`; press `Esc` to exit |
| Welcome home | The controller connects successfully | Press `Enter` to open the mapping manual; press `Esc` to return to the initial page; closing the window also releases held inputs |
| Mapping manual | After pressing `Enter` while running | Press `Enter` again to return home; press `Esc` to exit |

| # | Controller | Connection |
|:---:|---|---|
| 1 | DualSense (PS5) | USB |
| 2 | DualSense (PS5) | Bluetooth |
| 3 | DualShock 4 (PS4) | USB |
| 4 | DualShock 4 (PS4) | Bluetooth |
| 5 | Xbox 360 | USB |
| 6 | Xbox 360 | Bluetooth |
| 7 | Xbox Series X\|S | USB |
| 8 | Xbox Series X\|S | Bluetooth |

After the controller connects, the console switches to the ShikiPad welcome home screen. Close the ShikiPad console window to exit; held keys and mouse buttons are released automatically.

## Default Launch

When the prompt asks whether to save the selected controller as the default launch profile, press `Enter` or type `Y` to save it.

When a default is saved, ShikiPad no longer shows a default launch page. It starts with the saved profile immediately and waits for the controller to connect.

- Press `Esc` on the welcome home screen to return to the initial controller page and choose another controller for this run.
- Press `Esc` on the initial controller page to exit ShikiPad.

## Emergency Toggle

Hold `Share/Create/View` and `Options/Menu` together for about 1 second to toggle ShikiPad on or off. Use this when you temporarily want the controller to pass through as a normal gamepad. Hold the same combination for about 1 second again to restore ShikiPad mapping.

## Home And Manual

Press `Enter` while ShikiPad is running to open the mapping manual. Press `Enter` again from the manual to return to the ShikiPad home screen. The manual footer also shows this return path.

## Mouse And System Buttons

| Controller input | Output | Notes |
|---|---|---|
| Right stick | Mouse movement | Smooth power curve from fine aiming to fast travel |
| R3 | Right mouse button | Cursor freezes briefly on press to prevent click drift |
| L3 | Left mouse button | Hold to hold the mouse button |
| Left stick up/down | Mouse wheel up/down | Analog scroll speed increases with stick depth |
| PS Share/Create | `Right Alt` | Sony controllers only |
| PS Options/Menu | `Right Ctrl` | Sony controllers only |
| PS Home | `Right Shift` | Sony controllers only; Xbox Home is intercepted by Windows |

Right-stick feel is one of ShikiPad's core interaction details: deadzone, power exponent, maximum speed, and sensitivity work together to define cursor movement. This release keeps the fixed power-curve execution path intact.

## Left Stick Functions

The left stick works as an 8-way function dial.

| Direction | Output |
|---|---|
| Left | `Shift` |
| Down-left | `Ctrl` |
| Down-right | `Left Alt` |
| Right | `Win` |
| Up-left | `Esc` |
| Up-right | Fn layer |
| Up / Down | Mouse wheel |

When Fn is active, number-row outputs become `F1` through `F12`: `1` to `0` become `F1` to `F10`, `-` becomes `F11`, and `=` becomes `F12`.

## Clutch

Normally, the left stick holds one modifier direction at a time. Clutch lets you collect multiple modifiers and release them together.

| Controller | Activate clutch | Release clutch |
|---|---|---|
| DualSense / DualShock 4 | Short-tap Touchpad to toggle, or long-press Touchpad to hold | Short-tap again, or release after a long press |
| Xbox | Short-tap View/Back or Menu/Start to toggle, or long-press either button to hold | Short-tap again, or release after a long press |

Example: activate clutch, push left to collect `Shift`, push down-left to collect `Ctrl`, then push up-left to collect `Esc`. The system receives `Ctrl + Shift + Esc`. Releasing clutch releases all collected keys.

## Typing Layers

Button order in the table: `Up`, `Right`, `Square/X`, `Triangle/Y`, `Left`, `Down`, `Cross/A`, `Circle/B`.

| Held layer | Up | Right | Square/X | Triangle/Y | Left | Down | Cross/A | Circle/B |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Base | Up arrow | Right arrow | Space | Backspace | Left arrow | Down arrow | Enter | Tab |
| R1 / RB | i | n | e | a | o | t | h | u |
| L1 / LB | s | r | d | g | l | c | y | z |
| R2 / RT | m | w | j | x | q | f | p | b |
| L2 / LT | k | v | 1 | 2 | 3 | 4 | 5 | 6 |
| R1 + L1 | 7 | 8 | 9 | 0 | - | = | , | . |
| R2 + L2 | `<` | `)` | `[` | `{` | `(` | `>` | `}` | `]` |
| L1 + R2 | `` ` `` | `\` | `'` | `"` | `;` | `~` | `/` | `?` |
| R1 + L2 | `!` | `@` | `#` | `$` | `%` | `^` | `&` | `*` |

Base-layer keys repeat while held. Character-layer keys are virtual taps: one press sends one character, and holding does not repeat. For uppercase letters and less common shifted symbols, hold left-stick `Shift` while pressing the target key.

Voice typing shortcut: hold left-stick `Win` and press `R1 + Square/X` to send `Win + H`.

## Timing Model

ShikiPad uses short timing windows so fast rolling inputs still resolve to the intended layer.

| Setting | Default | Purpose |
|---|---:|---|
| `comboLayerWindowMs` | 35 ms | Max gap for a two-layer combo such as R1+L1 |
| `actionLayerGraceMs` | 35 ms | Lets an action button join a layer pressed immediately after it |
| `actionLayerPostGraceMs` | 25 ms | Covers the small blank gap after one layer releases before another starts |
| `layerTakeoverWindowMs` | 30 ms | Limits how long an old held layer may overlap a new intended layer |
| `actionLayerSwitchGuardMs` | 35 ms | Suppresses residue while changing layer after a character was sent |

## Built-In Defaults

These values are compiled into the current release.

| Area | Setting | Default |
|---|---|---:|
| Runtime | `enabled` | `true` |
| Injection | `useScanCode` | `true` |
| Mouse | `mouseMaxSpeed` | `20.0` |
| Mouse | `mouseSensitivity` | `1.0` |
| Mouse | `rightStickDeadzone` | `0.03` |
| Mouse | `rightStickCurveExponent` | `3.0` |
| Mouse | `r3FreezeMs` | `60` |
| Left stick | `leftStickEnterDeadzone` | `0.35` |
| Left stick | `leftStickExitDeadzone` | `0.25` |
| Triggers | `triggerPressThreshold` | `0.0` |
| Triggers | `triggerReleaseThreshold` | `0.0` |
| Repeat | `repeatDelayMs` | `300` |
| Repeat | `repeatIntervalMs` | `18` |
| Repeat | `baseRepeatSlowIntervalMs` | `120` |
| Repeat | `baseRepeatRampMs` | `1000` |
| Scroll | `scrollSlowIntervalMs` | `180` |
| Scroll | `scrollFastIntervalMs` | `18` |
| Scroll | `mouseScrollCurveExponent` | `3.0` |
| Timing | `comboLayerWindowMs` | `35` |
| Timing | `actionLayerGraceMs` | `35` |
| Timing | `actionLayerPostGraceMs` | `25` |
| Timing | `layerTakeoverWindowMs` | `30` |
| Timing | `actionLayerSwitchGuardMs` | `35` |
| Clutch | `clutchLongPressMs` | `250` |

## Troubleshooting

### No input is sent

Install the driver, restart Windows, then run ShikiPad as administrator. This version requires Interception; if Interception is unavailable, ShikiPad stops with an install/restart/admin prompt and does not fall back to `SendInput`.

### Double input in games

Sony controllers can be hidden with [HidHide](https://github.com/nefarius/HidHide/releases). Hide the physical controller and add the exact `ShikiPad.exe` path to HidHide applications.

Xbox controllers use XInput, so HidHide cannot hide them at the HID device layer. If hiding is required, use a controller that can switch to PS or DirectInput mode.
