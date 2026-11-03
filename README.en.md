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
- **Dependency-free MIDI file handling** - Standard MIDI File (SMF)
  reading and writing implemented in-house, without external libraries

## Main features

### Playing and sound

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

### Instrument lists and banks

- **Loadable instrument lists** (`.lst` files): beyond the built-in
  General MIDI list, custom lists - even sparse ones with non-
  contiguous indexing - can be loaded or generated
- **Separately handled Bank Select MSB (CC0) and LSB (CC32)**: an
  instrument is identified by a triplet, written in list files as
  `msb.lsb.program=Name` - the shorter `lsb.program=Name` and
  `program=Name` forms are also valid (missing parts are zero). This
  makes bank-stored variations beyond the 128 General MIDI instruments
  reachable under any banking convention (**GM2**: MSB=121 / drums
  MSB=120, **Roland GS**: variation in the MSB, **Yamaha XG**: variation
  in the LSB)
- **Built-in list**: `GM.lst` / `GM.drm` (General MIDI)
- **Dedicated drum kit instrument list** (`.drm` files) for channel 10,
  automatically paired with the active instrument list (matching base
  file name)
- **SoundFont (.sf2) import**: automatically generates the matching
  `.lst` (instruments) and `.drm` (drum kits) files from an `.sf2` file
  and applies them immediately. The SoundFont's bank goes into the Bank
  Select **MSB** - this is measurement-backed: BASSMIDI-based
  VirtualMIDISynth (and FluidSynth in its default `gs` mode) treats CC0
  as the SoundFont bank and ignores CC32
- **Importing several SoundFonts at once** (multi-select, overwrite
  without confirmation, a closing summary of how many files were created
  and how many were replaced)

### MIDI files

- **MIDI recorder**: records playing into a Standard MIDI File with **one
  track per channel** (track name in `"NN - Instrument name"` form, NN
  being the channel number), each channel's initial settings placed at the
  start of its own track
  - A separate, non-modal **recorder window**, **F9** = Start / Stop (in
    the main window too); recording starts on the **first key press**, so
    it never begins with silence; a pedal or modulation set while armed
    is included as well
  - Colour-coded status with elapsed time ("Waiting for a key..." / "●
    RECORDING 0:42"); the main window's button shows the state too
  - **Automatic recording mode**: the recorder splits playing into
    segments at silences - after the pause threshold (3 s by default) an
    attempt shorter than the play-time threshold (5 s by default) is
    discarded, a longer one is saved to its own file, and the recorder
    immediately waits for the next one. Silence only begins once every
    key is released **and** the sustain pedal is up, so a held chord or
    a pedalled decay is never cut
  - Automatic saving into the `Felvetelek` folder (file names based on
    the loaded preset's name, optional date subfolder), a "Last saved"
    line and an "Open folder" button; closing during a recording asks
    what to do (save / discard / cancel)
- **Playback** in the recorder window (**F8** = Play / Stop): plays back
  any `.mid` file to the MIDI output with accurate timing (on its own
  background thread); Stop silences only the playback's notes, live
  playing keeps sounding. You can choose whether the panels' settings
  are restored after playback or the UI switches to the file's settings;
  a fresh recording is loaded into the player automatically after saving
  - **Seeking** with a slider and a time display ("1:22 / 3:41"), by
    mouse or keyboard (← / → = 5 s, Home = start of the file); when
    starting mid-file, the player first sends the channels' settings up
    to that point, so it plays with the right instruments
  - **Overdubbing**: you can play along during playback; you choose
    whether the played-back material is recorded too, and whether Start
    also starts playback. The saved file is aligned to the played-back
    file's timeline, so the two can be layered later
- **Report alongside the recording**: a `.txt` file with the same name is
  generated automatically next to the saved `.mid`, describing its
  contents (channels, instruments, controllers, tempo, key, and the name
  of the current preset file)
- **Utilities menu** - directly from the main program, no separate
  utility programs needed:
  - Bulk-refreshing preset files
  - Bulk-converting old `.mb3` files
  - **Creating a preset from a MIDI file** (from its channel settings)
  - **Modifying MIDI files with the current settings**
  - **Saving a MIDI file's settings in readable form** (a `.txt` report
    for any MIDI file)

### Settings

- Save/load settings to/from JSON files, automatic startup-state
  loading, "Exit without saving" option
- The preset JSON carries a human-readable comment for the instrument,
  including the bank (e.g. `"Hangszer": 131096,  // Ukulele (B8.0)`)
- Import of legacy VB6 `.mb3` presets
- **`MidiBill3.ini`** for settings that aren't part of a preset: the
  position of both windows (moved to the default place if the saved one
  is no longer visible, e.g. after unplugging a monitor), reopening the
  recorder window if it was open on exit, and the recorder's settings
  (automatic mode, the two thresholds, date subfolder, report)
- Robust error handling for corrupted or hand-edited JSON, `.lst`, and
  `.sf2` files

## Project structure

```
MidiBill3/                      - single project
├── MainForm.vb                 - Main window: MIDI devices, channel selector,
│                                 split point, transposition, Reverb, Utilities
├── HangszerPanel.vb            - A single instrument panel (UserControl),
│                                 4 physical instances showing the currently
│                                 selected channel group
├── MidiFelvevoForm.vb          ("Recorder window") - Start/Stop (F9), elapsed
│                                 time, automatic recording (segments split at
│                                 silences), save settings, report
├── ExtraBeallitasok.vb         ("Extra settings") - MidiBill3.ini: window
│                                 positions, recorder settings (non-preset data)
├── ApplicationEvents.vb        - Global error logging (HibaNaplo.txt)
├── Modellek/                   ("Models")
│   ├── AppInfo.vb              - Version number lookup
│   ├── Beallitasok.vb          - Data model (16 channels) + JSON save/load
│   ├── Hangszerkulcs.vb        ("Instrument key") - single source of truth for
│   │                             MSB/LSB/program <-> combined integer key
│   ├── HangszerLista.vb        - Loadable instrument lists (instruments +
│   │                             dedicated drum kit list)
│   ├── SoundFontOlvaso.vb      - Reads .sf2 files to generate .lst/.drm
│   ├── MidiEngine.vb           - MIDI I/O, fan-out, split point, transposition,
│   │                             Bank Select-based instrument switching
│   ├── MidiFelvevo.vb          ("Recorder") - MIDI recording, Standard MIDI
│   │                             File writing (one track per channel)
│   ├── MidiLejatszo.vb         ("Player") - MIDI file playback on its own
│   │                             background thread
│   ├── AutomatikusSzegmentalo.vb ("Automatic segmenter") - the automatic
│   │                             recording decision (save / discard after silence)
│   ├── MidiFajlOlvaso.vb       - Standard MIDI File reading and analysis
│   ├── MidiFajlSzerkeszto.vb   - Modifying MIDI files with the current settings
│   ├── MidiPresetKeszito.vb    - Creating a preset from a MIDI file
│   ├── MidiJelentes.vb         - Human-readable .txt report about a MIDI file
│   ├── CsatornaUzenetek.vb     - Builds a channel's initial MIDI messages
│   ├── Hangnevek.vb            - MIDI note number -> note name conversion
│   ├── IniFajl.vb              ("INI file") - Simple INI file reading/writing
│   └── Utvonalak.vb            ("Paths") - All file paths in one place
│                                 (built-in vs. user content separation)
├── Tesztek/                    ("Tests") - Python verification scripts
│                                 (independent re-implementation of the VB
│                                 logic + mutation testing)
├── GM.lst / GM.drm             - BUILT-IN General MIDI lists (shipped)
├── CHANGELOG.md                - Detailed change log
├── MidiBill3_VBNET_aktualis_allapot.md  - Current state of the project
├── MIDI_felvetel_fazisterv.md  - Phase plan for the MIDI recorder
└── MSB_LSB_GM2_terv.md         - Plan for the MSB/LSB split
```

### Where is data stored?

The program **never writes to its own folder**, so it works when installed
under `C:\Program Files`, where a standard user cannot write. The two
locations are kept separate:

| Location | Contents |
|---|---|
| Program folder (read-only) | `GM.lst`, `GM.drm` - the built-in General MIDI lists |
| `%AppData%\MidiBill3\` | `Kezdet.json` (startup settings), `MidiBill3.ini` (window positions, recorder settings), `HibaNaplo.txt` (error log) |
| `%AppData%\MidiBill3\Presetek\` | Saved preset files (`.json`) |
| `%AppData%\MidiBill3\SF adatok\` | Imported instrument lists (`.lst` / `.drm`) |
| `%AppData%\MidiBill3\Felvetelek\` | `.mid` files saved by the MIDI recorder (plus the `.txt` reports generated next to them) |

`GM.lst` and `GM.drm` are protected: the built-in copies are always loaded,
and the program refuses to create user files under those names.

## History

The VB6 version was refactored and cleaned up in an earlier, longer
collaborative effort (bug fixes, control array cleanup, error handling,
note-tracking system, shutdown/device-handling robustness). This
VB.NET version is a deliberate rewrite of that cleaned-up state, using
modern tools - and has since significantly outgrown the original VB6
version's capabilities (16 fully editable channels, split point,
transposition, Reverb, separate MSB/LSB bank handling, SoundFont
import, MIDI recording and file handling, built-in utilities).

## Under active development

This project is currently under active development. MIDI recording is
complete in both manual and automatic mode (recorder window, recording
starts on the first key press, splitting into segments at silences,
saving and reporting), and playback with seeking and overdubbing works
too; final testing of the recorder window and the metronome are still
in progress.
