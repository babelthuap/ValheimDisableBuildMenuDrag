Fixes an issue where selecting pieces in the hammer build menu accidentally triggers menu dragging.

- Problem: In Valheim 1.0 the build menu is larger and therefore scrollable. By default, you can click and drag to scroll the menu. But I find myself very often accidentally triggering this click & drag when I simply intended to select a build piece.
- My fix: Trigger selection on pointerdown instead of Unity's default pointerup. I actually kinda prefer all menus this way. Makes the game feel more responsive.

This is the very first mod I've ever published. If you find any bugs, I'd be very happy if you send them my way! I posted the source code at https://github.com/babelthuap/ValheimDisableBuildMenuDrag
