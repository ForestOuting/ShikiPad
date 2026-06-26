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
5. For PlayStation controllers, configure HidHide to prevent double input in games.
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

A release archive contains only:

| File | Purpose |
|---|---|
| `ShikiPad.exe` | Main program |
| `install_driver.bat` | Interception driver installer helper |
| `interception.dll` | Interception runtime |
| `driver/install-interception.exe` | Interception driver installer |
| `README.md` / `README.zh-CN.md` | Documentation |

ShikiPad may create `shikipad.default` after launch. It only stores the default controller profile and is not part of the release package.

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

## Clutch

Normally, the left stick holds one modifier at a time. Clutch collects multiple modifiers and releases them together.

| Controller | Activate / hold |
|---|---|
| DualSense / DualShock 4 | Short-tap Touchpad to toggle, or long-press to hold |
| Xbox | Short-tap View/Back or Menu/Start to toggle, or long-press to hold |

## Typing Layers

The v3 release maps letters around familiar keyboard positions. This keeps layouts such as `WASD` and `IJKL` recognizable instead of sorting every letter purely by frequency.

The columns in the following tables correspond to: `↑`, `→`, `□/X`, `△/Y`, `←`, `↓`, `×/A`, `○/B`.

| Layer | ↑ | → | □/X | △/Y | ← | ↓ | ×/A | ○/B |
|---|---|---|---|---|---|---|---|---|
| Base | ↑ | → | Space | Backspace | ← | ↓ | Enter | Tab |
| R1 / RB | u | h | j | i | g | b | k | l |
| L1 / LB | w | d | f | r | a | s | c | v |
| R2 / RT | = | y | o | p | - | 0 | n | m |
| L2 / LT | q | e | t | 1 | z | x | 3 | 2 |
| R1 + L1 | 4 | , | . | 9 | 5 | 6 | 7 | 8 |
| L2 + R2 | ( | ) | : | " | < | > | [ / { | ] / } |
| L1 + R2 | # | * | ? | _ | ; | ! | / | ' |
| L2 + R1 | @ | % | + | $ | & | ^ | \ / \| | backtick / ~ |

ShikiPad sends physical key strokes. Keys requiring Shift (`{`, `}`, `|`, `~`) are generated by holding the left-stick `Shift` modifier along with their respective base keys (`[`, `]`, `\`, backtick).

Base-layer keys repeat while held. Character layers are virtual taps: one press sends one key stroke, and holding does not repeat.

## Troubleshooting

### No keyboard or mouse output

Install Interception, restart Windows, and run `ShikiPad.exe` as administrator.

### Double input in games

Sony controllers can be hidden with HidHide; add the exact `ShikiPad.exe` path to the whitelist. Xbox controllers use XInput, so HidHide usually cannot hide them at the HID layer.

## Timing Model Parameters

ShikiPad uses short time windows to absorb human input errors when typing quickly.

| Parameter | Default | Purpose |
|---|---:|---|
| `comboLayerWindowMs` | 45 ms | Base dual-layer interval. L1+R2 and L2+R1 add +10 ms, so they resolve at 55 ms |
| `actionLayerGraceMs` | 45 ms | Grace window between action key and layer recognition |
| `actionLayerPostGraceMs` | 20 ms | Grace window after a layer is released before a new layer is pressed |
| `layerTakeoverWindowMs` | 30 ms | Max time allowed for an old layer to overlap with a newly intended layer |
| `actionLayerSwitchGuardMs` | 35 ms | Suppress residual mis-touches when switching layers after a character is typed |
