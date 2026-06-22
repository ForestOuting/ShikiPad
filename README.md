# ShikiPad — Windows Controller-to-Keyboard & Mouse Mapper

> Map **DualSense (PS5)** · **DualShock 4 (PS4)** · **Xbox 360** · **Xbox Series X|S** controllers to keyboard and mouse.

ShikiPad is a native Windows application purpose-built for **fast controller typing**. Whether you're lying in bed, using a handheld streaming device, or simply prefer a gamepad over a keyboard, ShikiPad delivers fluid, accurate character input and precise mouse control.

Key features:
- 🎯 **All 26 English letters + digits + punctuation** accessible via shoulder/trigger layers, arranged by letter frequency
- 🖱️ Right stick mouse control with adjustable acceleration curve and deadzone
- ⚡ Original "Clutch" system for effortless multi-modifier shortcuts like `Ctrl + Shift + Esc`
- 🛡️ Interception kernel driver support — works inside VMs and some anti-cheat games
- ⚙️ Every runtime parameter is tunable via a JSON config file

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

---

## 📥 Installation & First Run

### 1. Download
Download the latest release archive from the **Releases** section on the right side of the GitHub page (or clone this repository). Extract it to any folder.

### 2. Install the Kernel Driver (Required)
ShikiPad uses the [Interception](https://github.com/oblitum/Interception) kernel driver for hardware-level keyboard injection, ensuring it works inside VMs and games with low-level anti-cheat.

1. In the extracted folder, find **`install_driver.bat`**.
2. **Right-click → Run as administrator**.
3. Wait for the command prompt to display `Installation complete!`, then close the window.
4. ⚠️ **You MUST restart your computer** for the driver to take effect.

### 3. Launch
After restarting, double-click `ShikiPad.exe`. In the terminal window, select your controller profile `[1..8, Enter = 1]`:

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

The program runs in the background after selection. **Close the terminal window to exit.**

### 4. Emergency Toggle
While running, **hold Share/Create/View + Options/Menu simultaneously for 2 seconds** to toggle ShikiPad on/off — useful when you need to temporarily restore raw controller input.

---

## 🎮 Complete Controls Guide

### 🖱️ Mouse Control (Right Stick)

| Controller Input | Output | Notes |
|---|---|---|
| **Right Stick** movement | Mouse cursor movement | Power-curve acceleration: gentle for fine control, fast when pushed hard |
| **R3** (Right Stick click) | Right Mouse Button | Cursor freezes briefly on click to prevent accidental movement |
| **L3** (Left Stick click) | Left Mouse Button | — |
| **PS Share/Create** (DS4/DS5) | `Right Alt` | Hold to press, release to let go |
| **PS Options/Menu** (DS4/DS5) | `Right Ctrl` | Hold to press, release to let go |
| **PS Home Button** (DS4/DS5) | `Right Shift` | Hold to press, release to let go. Xbox Home button is intercepted by Windows for Xbox Game Bar and cannot be mapped |

---

### 🛠️ Modifiers & Functions (Left Stick)

The Left Stick acts as an **8-way function dial**: push in a direction to hold the corresponding key, release to center to let go.

| Direction | Output | Typical Use |
|---|---|---|
| **← Left** | `Shift` | Uppercase letters, shifted symbols |
| **↙ Down-Left** | `Ctrl` | Shortcuts (e.g., Ctrl+C to copy) |
| **↘ Down-Right** | `Left Alt` | Shortcuts (e.g., Alt+Tab to switch windows) |
| **→ Right** | `Win` | Start menu, system shortcuts |
| **↖ Up-Left** | `Esc` | Cancel, exit |
| **↑ Up** | Mouse Wheel ↑ | Scroll up (accelerates when held) |
| **↓ Down** | Mouse Wheel ↓ | Scroll down (accelerates when held) |
| **↗ Up-Right** | Activate `Fn` layer | Converts number keys to F1–F12 |

#### ⚡ Clutch System (Modifier Accumulator)

By default, the Left Stick can only hold one modifier at a time. When you need multi-modifier shortcuts like `Ctrl + Shift + Esc` (Task Manager), use the **Clutch**:

**How to trigger:**

| Controller Type | Clutch ON | Clutch OFF |
|---|---|---|
| DS4 / DS5 | Short press **Touchpad** to toggle on, or long press to hold | Short press Touchpad again, or release after a long press |
| Xbox | Short press **View / Back** or **Menu / Start** to toggle on, or long press either button to hold | Short press the same clutch button again, or release after a long press |

- **Short press** = click-toggle clutch on/off.
- **Long press** = hold-to-clutch; releasing the button always ends clutch, even if a previous short press had toggled it on.
- On PS controllers, **Share/Create**, **Options/Menu**, and **Home** are free physical-key outputs: `Right Alt`, `Right Ctrl`, and `Right Shift`.

**How to use:** While the Clutch is active, freely move the Left Stick to "collect" modifiers one by one. For example: push Left (collect `Shift`), then push Down-Left (collect `Ctrl`), then push Up-Left (collect `Esc`) — all three keys are now held simultaneously, just like pressing `Ctrl + Shift + Esc` on a real keyboard! Releasing the Clutch releases all collected modifiers at once.

#### Fn Layer (F1 – F12)

Push the Left Stick to **Up-Right (↗)** to activate the Fn layer. While held, pressing number keys `1`–`0` and `-`/`=` will output `F1` through `F12`.

---

### ⌨️ Typing & Action Buttons

#### Base Layer (No Shoulder/Trigger Held)

| Controller Button | Output | Notes |
|---|---|---|
| **D-Pad ↑↓←→** | Arrow Keys | Hold to repeat |
| **□ Square** (Xbox: X) | `Space` | Hold to repeat |
| **△ Triangle** (Xbox: Y) | `Backspace` | Hold to repeat |
| **✕ Cross** (Xbox: A) | `Enter` | Hold to repeat |
| **○ Circle** (Xbox: B) | `Tab` | Hold to repeat |

> Base Layer keys **auto-repeat when held** — starting after 300ms with gradual acceleration, mimicking real keyboard behavior.

#### Character Layers (Hold Shoulder/Trigger — Fast Typing)

When you hold **R1, L1, R2, or L2** (or a combination), the D-Pad and Face buttons turn into English letters, digits, and punctuation.

To prevent accidental repeats during fast typing, **all Character Layer inputs are single virtual taps** — press once for one character, holding will NOT repeat. Letters are arranged by English usage frequency: the 8 most common letters (`i n e a o t h u`) are on the R1 layer, the next 8 on L1, and so on.

**Complete Character Mapping Table:**

*Button order (left to right): Up, Right, Square, Triangle, Left, Down, Cross, Circle*

| Shoulder/Trigger Held | ↑ | → | □(X) | △(Y) | ← | ↓ | ✕(A) | ○(B) |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **R1 (RB)** | i | n | e | a | o | t | h | u |
| **L1 (LB)** | s | r | d | g | l | c | y | z |
| **R2 (RT)** | m | w | j | x | q | f | p | b |
| **L2 (LT)** | k | v | 1 | 2 | 3 | 4 | 5 | 6 |
| **R1 + L1** | 7 | 8 | 9 | 0 | - | = | , | . |
| **R2 + L2** | `'` | `/` | `;` | `[` | `]` | `\` | `` ` `` | — |

> **Uppercase & shifted symbols:** Push the Left Stick **Left (Shift)** while pressing the letter key. For example: `Shift` + R1 + `e` = `E`, `Shift` + L2 + `1` = `!`.

> **💡 Voice Typing Tip:** If controller typing still feels too slow, you can use `Win + H` (`Left Stick →` + `R1 + □/X`) to open **Windows Voice Typing** (enable "Online speech recognition" in Windows Settings for best results).
> For even better speed and accuracy, we highly recommend using third-party AI voice typing software like **Typeless**, **Whisper**, or **闪电说**. Additionally, if you're using a DualSense (PS5) controller, you can switch your system input device to the controller's built-in microphone — speaking close to it yields excellent recognition results!

---

### 🔧 Layer Detection Timing (Advanced)

ShikiPad uses a precise timing system to correctly determine whether you intended to press a "base key" or a "character key":

> **Note: The combo layer window has been changed from 25ms to 30ms.**


| Parameter | Default | Purpose |
|---|---|---|
| `comboLayerWindowMs` | 30ms | R1+L1 or R2+L2 must be pressed within this time gap to be recognized as a combo layer |
| `actionLayerGraceMs` | 35ms | Pre-confirmation window. After an action button is pressed, a shoulder/trigger pressed within this window can still define the final layer |
| `actionLayerPostGraceMs` | 35ms | Post-release attribution window. After a layer modifier is released, action buttons pressed during this blank gap start as the released layer unless a later pre-confirmed layer covers them |
| `layerTakeoverWindowMs` | 30ms | Held-layer takeover window. Only applies when the old layer was still held after the action button press; it limits how much overlap a later layer may take over |

In short: pre-confirmation is **35ms**, post-release attribution is **35ms**, combo layers use a **30ms** pairing window, and held-layer takeover allows **30ms** of overlap.

---

## ⚙️ Advanced Configuration

On first launch, ShikiPad auto-generates a `shikipad.json` config file in the same directory. Edit it with any text editor (e.g., Notepad), save, and restart ShikiPad. Valid user values are preserved across launches; ShikiPad only falls back to defaults for missing, obsolete, or invalid entries.

See `shikipad.example.json` for a clean default template.

### Mouse

| Parameter | Default | Description |
|---|---|---|
| `mouseMaxSpeed` | `20.0` | Maximum cursor speed unit when the right stick is fully pushed. Effective maximum is `mouseMaxSpeed * 120 * mouseSensitivity` pixels/second |
| `mouseSensitivity` | `1.0` | Global multiplier for mouse speed |
| `rightStickDeadzone` | `0.03` | Right stick idle-noise deadzone. This filters hardware rest drift while preserving light intentional movement above the idle band |
| `rightStickCurveExponent` | `3.0` | Power curve exponent. Higher values = more precise at low deflection |
| `mouseScrollCurveExponent`| `3.0` | Left stick scroll curve exponent. Higher values = more precise at low deflection |
| `r3FreezeMs` | `60` | Cursor freeze duration (ms) after pressing R3. Clicking the stick often causes accidental nudges; this briefly ignores stick movement to ensure stable clicks |

### Left Stick / Modifiers

| Parameter | Default | Description |
|---|---|---|
| `leftStickEnterDeadzone` | `0.35` | Left stick must be pushed past this threshold to register a direction |
| `leftStickExitDeadzone` | `0.25` | Left stick must return below this threshold to register as "centered" |

### Triggers / Shoulders

| Parameter | Default | Description |
|---|---|---|
| `triggerPressThreshold` | `0.1` | L2/R2 "pressed" threshold |
| `triggerReleaseThreshold` | `0.05` | L2/R2 "released" threshold (lower than press to create hysteresis and prevent jitter) |

### Typing & Layers

| Parameter | Default | Description |
|---|---|---|
| `comboLayerWindowMs` | `30` | Max time gap (ms) between R1+L1 or R2+L2 to trigger a combo layer |
| `actionLayerGraceMs` | `35` | Pre-confirmation window (ms). After an action button is pressed, a new layer pressed within this window may cover it |
| `actionLayerPostGraceMs` | `35` | Post-release attribution window (ms). Starts after the layer modifier is released and covers only the blank gap before another layer is pressed |
| `layerTakeoverWindowMs` | `30` | Held-layer takeover window (ms). Applies only to overlap while the old layer was still held; it is not used for the post-release blank gap |
| `actionLayerSwitchGuardMs` | `35` | Already-sent character switch guard (ms). This is separate from post-release attribution; it suppresses residue when a held character key changes layers after it has already been sent |
| `clutchLongPressMs` | `250` | Press duration that separates a clutch short press from a clutch long press |

### Repeat / Scroll

| Parameter | Default | Description |
|---|---|---|
| `repeatDelayMs` | `300` | Initial delay before Base Layer key repeat starts |
| `repeatIntervalMs` | `32` | Fastest repeat interval at full speed, matching a high keyboard repeat rate |
| `baseRepeatSlowIntervalMs` | `240` | Starting repeat interval before the acceleration ramp |
| `baseRepeatRampMs` | `2500` | Time spent ramping from the slow repeat interval to the fastest interval |
| `scrollSlowIntervalMs` | `160` | Reference slow scroll interval (ms). Scroll ramps up from zero near the deadzone instead of starting with a full wheel notch |
| `scrollFastIntervalMs` | `18` | Fastest scroll interval when the stick is fully held (ms) |

### System

| Parameter | Default | Description |
|---|---|---|
| `configVersion` | `6` | Config file schema marker. Keep this value unless release notes say otherwise |
| `useInterception` | `true` | Use the Interception kernel driver. Set `false` to fall back to `SendInput` |
| `useScanCode` | `true` | Send hardware scan codes (better compatibility with some games and VMs) |

---

## 💡 FAQ

### Double Input Issue
If a game receives both raw gamepad input and ShikiPad's keyboard input simultaneously:

**Sony controllers (DualSense / DualShock 4):** Install [HidHide](https://github.com/nefarius/HidHide/releases), hide your physical controller, and add the full path of `ShikiPad.exe` to HidHide's application whitelist.

> ⚠️ **WARNING — Xbox controllers CANNOT be hidden by HidHide.** Xbox controllers use the XInput API (an API-level interface), while HidHide only operates at the HID device layer and has no effect on XInput devices.
>
> If you need to hide an Xbox-protocol controller, consider using a **third-party gamepad** from brands like **GameSir**, **Flydigi**, or **Betop** that support switching to **PS / DirectInput mode**. In DirectInput mode, the controller appears as a standard HID device and **can** be hidden by HidHide.
