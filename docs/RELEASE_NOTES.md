# Release Notes

## 5.5.1 中英文独立布局与键层时序调整 - 2026-10-23

本次 GitHub 更新汇总了上一版 5.2.1 之后的布局优化、快速连按修复与时序调整；中间版本的研究过程保留在下方记录中。

### 中英文独立键位

- 新增中文全拼和英文两套字母布局，启动时默认中文。两种语言的全部 26 个字母分别参与优化；数字、标点和 Base 操作沿用原来的位置。
- 根据字母频率、双字母与三字母衔接安排位置，结合每侧一个食指切换肩键/扳机的握持方式，关注切层、同侧食指移位和十字键反向的共同成本。允许同一拇指连续按相邻位置，不强制左右手交替。
- 两种语言的前 10 个高频字母均放在肩键层；中文 `ang`、`ing`、`dan` 等组合可在同层完成。整词和技术词不额外加权，避免为少数词牺牲其他组合。
- 在主页或映射说明页按 **Tab** 切换布局，**Enter** 切换页面，**Esc** 退出。切换布局不切换 Windows 输入法；已按下或等待输出的动作保留按下时的语言。
- 页面键位图直接使用实际映射表，README 提供两套完整键位。布局报告列出改善、退步及适用范围，实际手感仍需本人使用手柄验证。

### 快速连按修复

- 修复同一动作位置在上一下尚未完成判定时再次按下，可能丢失后一次点击的问题。
- 每次按下分别记录键层、修饰窗口和语言，按同位置的物理按下顺序输出；退出和清理时一并释放。

### 时序默认值

| 参数 | 原值 | 5.5.1 |
|---|---:|---:|
| `ComboLayerWindowMs` | 25 ms | **35 ms** |
| `ActionLayerGraceMs` | 35 ms | **40 ms** |
| `LayerOccupancyCarryCutoffMs` | 20 ms | **30 ms** |

- `LayerTakeoverWindowMs` 仍为 30 ms，`ModifierBindingWindowMs` 仍为 45 ms，`ActionLayerPostGraceMs` 仍为 15 ms。
- 通常输出仍等待 45 ms；若 Base 动作后 40 ms 才由新单层接管并保持单层，需要等到动作后 75 ms 之后的首个轮询才输出。
- 保留用户选定的晚组合撤销规则：例如 Base 动作在 0 ms 按下、R1 在 20 ms 按下、L1 在 41 ms 按下，等待中的动作回到最初 Base；若 L1 恰好在 40 ms 按下，则仍可归属合法组合层。
- README 同步参数含义、包含边界的判定和本体追溯限制。

### 验证与文档

- 通过实际 MapperForm 的 **13,788 组连续输入序列、41,891 个字母**回归，覆盖双语所有双字母组合、语料三字母、中文拼音及技术词串。
- 通过 35/40 ms 窗口、30 ms 本体截点与上限、45 ms 独立修饰窗口、晚组合撤销和快速重复点按等边界测试。测试使用假输出，不向系统发送按键。
- 新增布局设计说明、常用组合路径和机器可读评估数据。更新 5.5.1 EXE、发布脚本和文件校验清单。
- 沿用 Windows SendInput 输出，无需安装 Interception；HidHide 配置方式沿用原版。运行需要有线 DualSense 和 x64 .NET 8 Desktop Runtime，当前程序仍为未签名内部验证包。

## 5.5.0 字母频率与衔接优先 - 2026-10-23

- Reoptimized all 26 letters with higher shoulder-frequency priority and broader protection: 209 Chinese and 100 English directed transitions exclude immediate layer change plus D-pad reversal. Original numbers, punctuation and Base positions are fixed.
- No whole-word or technical-term bonus. Chinese ing uses a same-layer adjacent face-button route; English c moves to a shoulder layer. Published both gains and regressions, including higher Chinese layer-switch counts.
- Compared strict top-16 shoulder constraints with free-letter searches, independently checked 796 candidate records and all 140,075 two/three/four-letter neighbors per selected layout. Added 108 weight combinations, complete words/syllables, technical vocabulary and literal-space sentence diagnostics; no global optimality or measured-speed claim.
- Added actual-mapper continuous-input regressions: 13,788 sequences / 41,891 letters, every letter pair in two layer-overlap conditions, observed trigrams, Chinese syllables and technical strings. A fake sink produces no system keystrokes.
- Application changes from 5.4.0 are letter tables and version metadata only. Timing, gestures, language snapshots, digits/symbols and console Enter/Tab/Esc behavior remain intact.

## 5.4.0 全部字母重新优化 - 2026-10-23

- Reoptimized all 26 letters independently for Chinese full pinyin and English, with every original number/symbol position fixed. Removed 5.3.1's restriction to moving only three displaced letters.
- Used two seed batches, four cost preferences, 1,024 pair-stage starts and 64 direct full-objective starts per language. Refined shortlisted candidates using complete trigram costs, then exhausted all 140,075 reassignments involving up to four letters at each final layout. No improving neighbor was found under the declared model; this is not proof of global optimality.
- Independently verified 458 candidate scores, protection constraints, selected layouts and corpus metrics. Chinese 162 protected pairs and English 50 protected pairs still have no combined layer change and immediate D-pad reversal. All 64 outputs and original nonletter positions are preserved.
- Added common 2–6-letter/word/pinyin paths, external-corpus tradeoffs and machine-readable evaluation data. Some sequences regress; no measured typing-speed claim is made.
- Changed only layout tables and related documentation/version metadata in the application. Console shortcuts, timing, language snapshots, gestures and the rapid-press fix are unchanged.

## 5.3.1 恢复原数字标点位置 - 2026-10-23

- Restored every number and symbol to the user's original layer/button position in both languages, including the original R1+L1 row and R2 `0`, `-`, `=`.
- Kept 23 letter positions per language. Only the three letters displaced by restored symbols moved: Chinese `f` to L2 Square, `l` to R2 Square, `m` to R2 Triangle; English `b` to L2 Square, `v` to R2 Square, `w` to R2 Triangle. Selected among all six assignments using the existing training model.
- Rechecked the Chinese 162 and English 50 protected transitions, full 64-output inventory and external corpus metrics. Added a regression requiring all 38 nonletter outputs to remain at their legacy positions.
- Page shortcuts, input timing, gestures, language snapshots and the rapid-press fix are unchanged.

## 5.3.0 中英文双布局 - 2026-10-23

- Added separate Chinese full-pinyin and English letter layouts. Both preserve all 64 direct character outputs and share number/symbol positions and the Base layer. Startup defaults to Chinese.
- Tab in either console page switches layouts; Enter retains page navigation and Esc retains exit on both pages. Switching layouts does not switch the Windows IME. Pending/held actions keep the language selected at their physical press.
- Generated the displayed key diagrams from the same tables used by the mapper, eliminating a separate hard-coded manual map.
- Fixed rapid repeated presses of the same action button while an earlier press is still pending. Each press retains its own layer trace and modifier window; same-position presses are emitted in physical order and cleared on runtime cleanup.
- Preserved all layer/modifier timing parameters, trigger hysteresis, late-combo cancellation, combo component consumption, gestures and repeat rules. Clarified the late-combo exception, extra combo-settle wait and consumed-component Base fallback in README.
- Layout optimization permits adjacent same-thumb movement and does not impose alternation. Protected Chinese training syllable transitions/high-frequency cross-syllable pairs and the top 50 English training bigrams exclude a layer change combined with immediate D-pad reversal. These corpus constraints are not a proof of global optimum or measured typing speed.
- Uses the existing SendInput backend and HidHide path. No driver installation or system input-method change is performed.

## 5.2.1 SendInput 版 - 2026-10-22

- Replaced the Interception output backend with Windows `SendInput`. Keyboard events retain scan-code and extended-key flags; keyboard, mouse-button, wheel and pointer events are submitted one at a time in the existing synchronous order.
- Preserved the existing one-retry behavior, confirmed-event bookkeeping, modifier reference counts, temporary-shortcut cleanup and final shutdown release. A successful native return confirms insertion into the Windows input stream, not acceptance by the target application.
- Preserved the Windows HID reader, controller report conversion, mapper, gestures, layer/modifier rules, continuous-input integration and all mapping/timing defaults. Administrator execution and HidHide usage remain unchanged; the existing `C:\ShikiPad\ShikiPad.exe` whitelist path can be reused.
- Removed the Interception wrapper, runtime library, driver installer and driver-management scripts from this branch and its release package. Updating the EXE does not uninstall an existing system driver; use the complete original package's uninstall entry and restart Windows if removal is desired.
- Added regressions for the x64 native input layout, absence of Interception imports, physical-key scan codes and extended flags, signed high-resolution wheel deltas, mouse-button cleanup and initialization without the Interception library. These checks use simulated output and do not send desktop input.
- Preserved the original Interception version on GitHub `main` at `7fb6d57`; the new version uses `codex/sendinput`. The local original-package backup is `ShikiPad-release-interception-7fb6d57.zip` and is not committed to Git.
- Updated package metadata to file version `5.2.1.0` and informational version `5.2.1-sendinput`. The current unsigned package is for internal verification; actual controller/application compatibility, including special input environments and protected desktops, is not claimed as verified.

## V5.2 maintenance - 2026-10-18

- Added root-level `安装驱动.cmd` and `卸载驱动.cmd`, with one shared administrator-elevating script, explicit failure results and manual restart guidance. Interception remains the only keyboard/mouse output backend.
- Kept support documentation under `docs/` in release archives and included README screenshots; embedded icon/manifest files no longer clutter the archive root.
- Applied ShikiFight's reusable report-buffer and cancellable-read principles to the existing HID worker. Each read still publishes only the latest complete state to the original fixed scheduler; report parsing, axis/trigger conversion, touch rules, mappings and all timing/deadzone defaults are preserved.
- Made the HID reader own its asynchronous device stream through cancellation and disposal, eliminating cross-thread handle closure. Disconnects publish a neutral state before reconnect attempts; only valid reports mark the controller connected.
- Tracked confirmed temporary keyboard strokes so runtime cleanup can release a shortcut key whose KeyUp failed, while preserving the existing per-stroke send order and single retry.
- Closed the output path before final shutdown release, preventing a late mapper action from pressing a key again during driver cleanup.
- Corrected the generic startup-error heading so duplicate instances and invalid configuration are not mislabeled as a missing driver.
- Added regressions for every partial-shortcut failure position, modifier reference counting, repeated HID start/stop, cancellation, disconnect and buffer reuse with a shorter next report. These tests do not send desktop input or install/uninstall any driver.

## V5.2 - 2026-07-22

- Replaced the global one-module-per-poll output owner with deterministic non-exclusive processing: every eligible module now runs in order during the same poll, so keyboard actions, mouse buttons, wheel output, and pointer movement no longer suppress one another.
- Removed the competing HID-callback mapper entry and the now-unnecessary tick lock: the HID thread only publishes the latest immutable state, while one fixed 1 ms scheduler exclusively performs mapping and output.
- Restored fine high-resolution wheel deltas from the first available 0.5 accumulated unit while pacing subsequent reports to at most one every 4 ms; maximum stick speed still accumulates 120 units per 15 ms without returning to one wheel report every 1 ms.
- Restored immediate, confirmed delivery for each atomic keyboard and mouse action after testing found that deferred frame batching could desynchronize program-side hold bookkeeping from the strokes actually accepted by the driver.
- Kept left-stick wheel and right-stick pointer integration independent and sends their wheel and movement reports separately in the same scheduler cycle, without relying on an unverified combined stroke.
- Added a center-weighted 0.05 low-speed assist to the cubic right-stick curve: subtle movement reaches its first one-pixel output much sooner, while the correction fades toward the outer range and leaves full-stick speed unchanged.
- Preserved the actual conflict boundaries instead of broad frame blocking: touchpad click/gesture ownership, atomic shortcut and modifier pulses, modifier reference counting, unconditional releases, and the explicit R3 pointer freeze remain intact.
- Preserved a complete modifier tap when a left-stick or Create/Options modifier is released between logical registration and its physical-output stage, preventing very short taps from disappearing.
- Made the modifier-binding wait genuinely independent from the pure layer grace window while retaining the original layer takeover boundaries.
- Kept the documented module call order without a runtime priority/stage gate, added native Interception send-result checks with one retry, serialized context cleanup, and kept injector bookkeeping unchanged after failed sends.
- Added a per-session single-instance lock, HID parse diagnostics, validated editable parameters, implemented the documented linear right-stick curve, and corrected all assembly metadata to V5.2.
- Added reproducible release packaging, SHA-256 manifests, third-party notices, .NET 8 Desktop Runtime documentation, and driver-installer exit-code handling; removed the unused Bluetooth marker from the package definition.
- Stopped tracking the release ZIP; the packaging script creates the ignored root-level `ShikiPad.zip` solely for upload as a GitHub Release asset.
- Left-stick and Create/Options modifier states and 45 ms bindings are registered immediately, while physical KeyDown output follows touchpad output and precedes bindable mouse/action output in the same poll.
- KeyUp, mouse-up, gesture-modifier cleanup, Home/Mute state, and shoulder/trigger layer state remain independent and cannot be suppressed by another module's output.
- Mouse-button and action pending states continue to implement their intentional 45 ms decision windows; once due, they are no longer postponed merely because another module emitted in the same poll.
- Reassigned right-side touch gestures: one-finger up/down restores minimized windows or minimizes all windows; two-finger up/down takes a screenshot or closes the current window; two-finger left/right uses `Win+Shift+Left/Right` to move the current window to the left/right monitor.
- Raised the default mouse sensitivity from 1.0 to 1.5, reduced the combo-layer recognition window from 35 ms to 25 ms, and shortened the action-layer grace window from 45 ms to 35 ms to reduce late layer takeover while typing quickly.
- `KeyTap` now temporarily releases and restores an already-held ordinary target key, preventing an atomic shortcut from stealing another module's held-key ownership.
- Touch-gesture shortcuts now use exact modifier isolation: unrelated held modifiers are suspended for the atomic shortcut and restored afterward, while action-layer taps retain their intentional 45 ms bound modifiers.

## 2026-07-22

- Changed two-finger right-side direct swipes to left `Win+Shift+S`, up `Win+Shift+M`, right `Alt+F4`, and down `Win+M`; all four remain one-shot gestures.
- Set the straight-up and straight-down left-stick wheel sectors to ±25° without moving either vertical centerline, for 50° per wheel sector; each of the four modifier sectors now occupies exactly 65°.

## 2026-07-21

- Caps/Fn is now a persistent toggle: only a letter capitalization or an actual `1..0 - =` to `F1..F12` translation automatically exits the mode. Touchpad clicks and every untranslated action leave it active.
- Raised the left-stick scroll-sector deadzone from 0.15 to 0.30 and the four modifier-sector deadzones from 0.45 to 0.50.
- A primed Home clutch is now consumed only by the eight mapped action positions across every layer or by L3/R3. Touchpad `Delete`, `Backspace`, and `Caps Lock` preserve the clutch state, while Base repeat actions continue emitting the saved modifiers with every repeat pulse.
- Restored the pure layer timing state machine: Base is only the eight D-pad/face mappings with no shoulder or trigger held, and modifier capture can no longer override layer ownership. The 45 ms layer grace, 15 ms post-grace, 35 ms combo window, and 20/30 ms occupancy limits are independent from the new 45 ms modifier-binding window.
- Touchpad clicks, Home, Mute, and mouse buttons retain separate module identities. The independent 45 ms modifier-binding window now covers only L3/R3 and all eight action positions across Base and character layers; Home no longer registers or captures bindings from this window.
- Modifier-bound Base repeat keys now emit a complete chord pulse on the initial event and every repeat tick: modifiers and the action key are pressed together, then the action and that pulse's modifier references are released. The binding never leaves a modifier artificially held between repeats.
- Two-finger right-side mappings are now left `Win+V`, up `Win+Shift+S`, right `Win+E`, and down `Alt+F4`.
- Raised `TouchGestureHoldStillDistance` from 50 to 100 for the post-trigger static-finger constraint. On the first two-finger trigger, the finger that has not reached a 150/180 direction threshold becomes static and settles its current position as the baseline, even if it has already moved 100.
- Once the static role is established, reaching 100 movement, releasing that contact, or changing its touch ID immediately invalidates the whole touch sequence until every contact is released. The static role is never transferred and the remaining mover cannot become another gesture.
- Moving-finger continuation remains available while the same static contact stays within tolerance: lifting and replacing only the mover starts a new two-finger segment from the mover's new touch-down position.
- Every physical 150/180 moving-finger step now settles both movement axes and refreshes the static-finger baseline, including forward, reverse, and no-feedback steps. Timer-only repeats do not refresh the baseline.

## 2026-07-20

- Replaced the parallel modifier/layer windows with one 45 ms action-decision window. A captured modifier wins and locks the layer that existed when the action began; shoulder/trigger changes after that point cannot take over the same action.
- Captured modifiers are sticky action ownership rather than a one-time restoration. They survive the physical modifier's release until a held keyboard or mouse action ends, and fully wrap virtual taps.
- Inputs are now documented as modifier, non-modifier action, or shoulder/trigger layer controls. Base D-pad repeat, touchpad `Delete`/`Backspace`, Home, and Mute bypass modifier look-ahead; non-repeating `Caps Lock`, face/character actions, and L3/R3 can use it.
- Touchpad swipe gestures no longer produce a buffer-zone gesture. The moving finger's start segment and first recognized direction resolve every gesture to the left or right side.
- One-finger mappings now use left-side Alt-Tab/window sizing and right-side desktop/window switching. Two-finger left-side continuation remains intact, while right-side one-shot mappings are now `Win + V`, `Ctrl + Shift + Esc`, screenshot, and `Alt + F4`.
- After recognition, the gesture side and repeat mode remain locked until the moving finger lifts. Every vertical 150 or horizontal 180 movement settles one step and resets both axis accumulators, even when that step has no shortcut feedback; Alt-Tab remains the special four-direction distance gesture.
- The obsolete 70% trajectory-zone parameter and buffer gesture mappings were removed. The middle touchpad-click area still sends real `Caps Lock` and is independent from swipe zoning.

## 2026-07-13

- Touchpad gestures now use three independent zones: left confirmed, middle buffer, and right confirmed. A confirmed zone needs 70% of the pre-trigger trajectory; otherwise the gesture stays in the buffer.
- Gesture zone and finger count lock after recognition and remain stable until the moving finger lifts. Buffer Alt-Tab navigation keeps holding `Alt` and accepts distance-based arrow gestures even after crossing into another zone.
- One-finger and two-finger mappings were reorganized, including 550 ms desktop repeat and 450 ms window/tab/navigation repeat with reverse-direction switching.
- All touchpad hold-then-swipe recognition, mappings, timers, and state were removed.
- Touchpad clicks and gestures now use first-trigger-wins arbitration. `Backspace`, `Delete`, and `Caps Lock` clicks send real key-down/key-up events, remain available in every layer, and correctly consume one-shot Caps/Fn.
- The Chinese README was rewritten around the actual mapper behavior and current parameters; the redundant English README was removed.

## 2026-07-12

- Two-finger direct touchpad mappings were rearranged: left side now handles tab/back/forward navigation, right side now handles screenshot, Task Manager, and `Alt + F4`; two-finger hold-then-swipe is unmapped.
- Two-finger continuation now uses the returning moving finger's real touch-down point as the next segment origin, so quick re-swipes are recognized without losing the first sampled distance.
- Two-finger continuation now refreshes the still-finger baseline between moving-finger segments, so small thumb drift does not accumulate and break later swipes.
- Unmapped two-finger continuation segments are ignored without cancelling continuation, preventing an accidental unmapped direction from making later swipes appear dead.
- Touchpad stillness checks are unified: movement below `TouchGestureHoldStillDistance` (50 in this release) counts as still, but swipe distance is still counted from the real segment start.
- Two-finger touchpad continuation now survives brief edge unreadable samples: a lightly out-of-bounds edge touch is treated as that one touch temporarily leaving instead of pinning it to the edge, and continuation waits instead of restarting when the still finger briefly disappears.
- Touchpad click now sends `Backspace` when the click report has no active touch point, and also when two touch points are active.
- Create/Options system buttons now use deterministic press-before-actions and release-after-actions ordering, so `Right Alt` / `Right Ctrl` combinations are less likely to miss when pressed in the same frame as action keys or touchpad actions.
- Keyboard and mouse hold bookkeeping is serialized inside the injector, reducing release races between mapper ticks, disable/enable, disconnect, and process shutdown.

## 2026-07-11

- Mute short press now toggles the one-shot Caps/Fn layer. Press Mute again before an action key to cancel it and restore normal output.
- Caps/Fn maps `1..0`, `-`, and `=` to `F1..F12`; unshifted letters output as shifted uppercase letters.
- Touchpad gestures now support direct swipe, hold-then-swipe, and two-finger mappings. Hold-then-swipe is recognized at the 150/180 trigger distance after 450 ms of touch duration.
- Touchpad and left-stick modifier overlap is guarded by modifier reference counting, reducing accidental stuck or prematurely released modifier keys.
- The README no longer duplicates a separate install section; driver and setup order is the installation guide.
