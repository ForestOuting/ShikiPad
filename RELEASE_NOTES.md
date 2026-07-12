# Release Notes

## 2026-07-12

- Two-finger touchpad continuation now survives brief edge unreadable samples: one out-of-bounds touch point no longer drops the whole touch report, and continuation waits instead of restarting when the still finger briefly disappears.
- Touchpad click now sends `Backspace` when the click report has no active touch point, and also when two touch points are active.
- Create/Options system buttons now use deterministic press-before-actions and release-after-actions ordering, so `Right Alt` / `Right Ctrl` combinations are less likely to miss when pressed in the same frame as action keys or touchpad actions.
- Keyboard and mouse hold bookkeeping is serialized inside the injector, reducing release races between mapper ticks, disable/enable, disconnect, and process shutdown.

## 2026-07-11

- Mute short press now toggles the one-shot Caps/Fn layer. Press Mute again before an action key to cancel it and restore normal output.
- Caps/Fn maps `1..0`, `-`, and `=` to `F1..F12`; unshifted letters output as shifted uppercase letters.
- Touchpad gestures now support direct swipe, hold-then-swipe, and two-finger mappings. Hold-then-swipe is recognized at the 150/180 trigger distance after 450 ms of touch duration.
- Touchpad and left-stick modifier overlap is guarded by modifier reference counting, reducing accidental stuck or prematurely released modifier keys.
- The README no longer duplicates a separate install section; driver and setup order is the installation guide.
