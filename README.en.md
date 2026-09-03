[🇭🇺 Magyar](README.md) | 🇬🇧 English

# MidiBill3 (VB.NET)

An application that splits incoming MIDI keyboard signals: a single
key press can trigger any of **16 MIDI channels** simultaneously, each
with its own instrument, volume, pan, and vibrato (modulation) - grouped
in fours, with one group of 4 editable in the UI at a time. All 16
channels (including channel 10, the drum channel) are fully editable.

This project is a deliberate, **hand-written rewrite** of an earlier,
stable VB6 application (`MidiBill3` VB6) to VB.NET - not an automated
migration. The goal is the same functionality and user experience on
modern .NET foundations, extended with features the old VB6
architecture couldn't support.

## Technologies

- **.NET 10 (LTS)**
- **WinForms** - user interface, DPI-based scaling, fixed window size
- **NAudio.Midi** - MIDI input/output handling
- **System.Text.Json** - saving/loading settings (replacing the old,
  position-based `.mb3` file format), with human-readable comments

## Main features

- **16 MIDI channels**, grouped in fours (1-4 / 5-8 / 9-12 / 13-16),
  one group editable in the UI at a time - all 16 channels (including
  the drum channel, 10) are fully configurable
- Per-channel enable/disable, with automatic "reconnection" to already-
  held notes when re-enabling a channel
- **Split Point**: the keyboard can be split into two zones (Below/Above)
  at a chosen note, configurable per channel, set manually or via
  "Learn" mode (the next key pressed sets the split point)
- **Global transposition** in semitone/octave steps (±24 semitones)
- **Per-channel octave transposition** (±2 octaves), with immediate
  (retroactive) effect - stacks with the global transposition
- **Global Reverb** (MIDI CC91), on a 0-100% user-facing scale
- **Loadable instrument lists** (`.lst` files): beyond the built-in
  General MIDI list, custom lists - even sparse ones with non-
  contiguous indexing - can be loaded or generated
- **Bank Select-based extended instrument support** (Roland GS / Yamaha
  XG-compatible): access to bank-stored variations beyond the 128
  General MIDI instruments, via `bank.program=Name`-formatted `.lst`
  files
- **Dedicated drum kit instrument list** (`.drm` files) for channel 10,
  automatically paired with the active instrument list (matching base
  file name)
- **SoundFont (.sf2) import**: automatically generates the matching
  `.lst` (instruments) and `.drm` (drum kits) files from an `.sf2` file
  and applies them immediately
- Note-tracking system for targeted (non-wasteful) note-off handling -
  switching the split point or transposition mid-performance never
  leaves a stuck note
- "Silence!" panic button (sweeps all 16 channels)
- Pedal inversion (a passive "mode" switch - never sends MIDI on its own)
- Fault-tolerant MIDI sending (the program doesn't crash even if a USB-
  MIDI interface is unplugged), with MIDI device list refresh and
  automatic validation
- Human-readable MIDI message display (e.g. "NoteOn C4 v90"), with a
  throttled refresh rate
- Save/load settings to/from JSON files, automatic startup-state
  loading, "Exit without saving" option
- Import of legacy VB6 `.mb3` presets
- **Utilities menu**: bulk-refreshing preset files, bulk-converting old
  `.mb3` files - directly from the main program, no separate utility
  programs needed
- Robust error handling for corrupted or hand-edited JSON, `.lst`, and
  `.sf2` files

## Project structure

```
MidiBill3/                    - single project
├── MainForm.vb                - Main window: MIDI devices, channel
│                                 selector, split point, transposition,
│                                 Reverb, Utilities
├── HangszerPanel.vb            - A single instrument panel (UserControl),
│                                 4 physical instances showing the
│                                 currently selected channel group
├── ApplicationEvents.vb        - Global error logging (HibaNaplo.txt)
├── Modellek/                   ("Models")
│   ├── AppInfo.vb              - Version number lookup
│   ├── Beallitasok.vb          - Data model (16 channels) + JSON save/load
│   ├── MidiEngine.vb           - MIDI I/O, fan-out, split point,
│   │                             transposition, Bank Select-based
│   │                             instrument switching
│   ├── HangszerLista.vb        - Loadable instrument lists (instruments +
│   │                             dedicated drum kit list)
│   ├── SoundFontOlvaso.vb      - Reads .sf2 files to generate .lst/.drm
│   ├── Hangnevek.vb            - MIDI note number -> note name conversion
│   └── Utvonalak.vb            ("Paths") - All file paths in one place
│                                 (built-in vs. user content separation)
├── GM.lst                      - BUILT-IN instrument list (shipped)
└── GM.drm                      - BUILT-IN drum kit list (shipped)
```

### Where is data stored?

The program **never writes to its own folder**, so it works when installed
under `C:\Program Files`, where a standard user cannot write. The two
locations are kept separate:

| Location | Contents |
|---|---|
| Program folder (read-only) | `GM.lst`, `GM.drm` - the built-in General MIDI lists |
| `%AppData%\MidiBill3\` | `Kezdet.json` (startup settings), `HibaNaplo.txt` (error log) |
| `%AppData%\MidiBill3\Presetek\` | Saved preset files (`.json`) |
| `%AppData%\MidiBill3\SF adatok\` | Imported instrument lists (`.lst` / `.drm`) |

`GM.lst` and `GM.drm` are protected: the built-in copies are always loaded,
and the program refuses to create user files under those names.

## History

The VB6 version was refactored and cleaned up in an earlier, longer
collaborative effort (bug fixes, control array cleanup, error handling,
note-tracking system, shutdown/device-handling robustness). This
VB.NET version is a deliberate rewrite of that cleaned-up state, using
modern tools - and has since significantly outgrown the original VB6
version's capabilities (16 fully editable channels, split point,
transposition, Reverb, Bank Select-based GS/XG support, SoundFont
import, built-in utilities).

## Under active development

This project is currently under active development.
