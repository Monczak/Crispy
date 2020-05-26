This is a simple CHIP-8 emulator built with MonoGame.

-- CONTROLS --

The CHIP-8 hex keyboard matrix is implemented as follows:
1	2	3	4
Q	W	E	R
A	S	D	F
Z	X	C	V

Other hotkeys:
Space - pause/unpause the emulator
F1 - open this help menu
F2 - take a screenshot (screenshots are saved to the Screenshots folder)
F3 - load a program from disk
F4 - reset the emulator
F5 - select previous savestate slot
F6 - select next savestate slot
F7 - load state from selected slot
F8 - save state to selected slot
F9 - frame advance
` (grave key) - rewind
Esc - close the emulator

-- SAVESTATES --

Crispy provides 6 savestate slots for each program. They are saved to the Savestates folder.

-- FRAME ADVANCE --

When frame advance is on (after pressing F9), Crispy waits for the next draw call and then pauses the emulation. Pressing F9 again will run the program until the next draw call.
Frame advance does not pause the emulator - the CPU may spin or wait for a keypress. Frame advance in Crispy only pauses the emulation when something is drawn to the screen.
To resume normal execution, unpause the emulator by pressing Space.

-- REWIND --

Crispy records the most recent 10 seconds of emulation and can play them backwards, as if it was rewinding a tape. This is useful if you made a mistake during gameplay and didn't savestate.
To trigger the rewind, hold the Grave (`) key. The rewind will stop if it has reached the end of the buffer.
Rewinding is possible during pause and frame advance. The emulation will play back, and upon finishing the rewind Crispy will return to the previous mode.