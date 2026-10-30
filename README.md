# HitTiming

**MOD created by MowoM**

HitTiming displays the corrected hit timing in milliseconds, taking the movement speed of the notes into account.

The goal is to make it easier to find the correct global audio offset in Beat Saber, allowing players to play from their headset's centered position while achieving a timing of **0 ms**, in sync with the music.

## Finding your ideal position

HitTiming uses a configurable distance between the note's impact point and the player's centered headset position.

The default value is **1.0 meter**. This represents an average playing distance based on the player's forearm length and approximately three-quarters of the saber blade.

You can adjust this value through the `CorrectionDistance` parameter in:

`Beat Saber/UserData/HitTiming.json`

If you play with your arms fully extended, you may need to increase this value slightly.

For most players, however, only a small forward or backward movement of a few centimeters is usually enough to compensate and reach 0 ms.

Different songs may have slightly different audio offsets. Some sections of a song may also have small timing variations. In these cases, moving just a little along the Z axis can help you synchronize perfectly with the music.

## Customization

The timing text can be customized through `HitTiming.json`, located in the Beat Saber `UserData` folder.

Available settings include:

- Text position (X, Y and Z offsets)
- Text size
- Display duration and fade-out time
- Movement speed and direction
- Timing threshold colors
- Whether to display timing for normal notes only
- Correction distance used for timing calculations

You can edit these values to match your preferences and playing style.

## Requirements

- Beat Saber 1.40.8
- BSIPA 4.3.6 or later

## Building from source

The project targets **.NET Framework 4.7.2**.

The build requires the Beat Saber installation directory to be available through the `BeatSaberDir` environment variable.

The project also uses `BeatSaberModdingTools.Tasks` version `2.0.0-beta4`.

---

Have fun, and I hope HitTiming helps you improve your gameplay! 
