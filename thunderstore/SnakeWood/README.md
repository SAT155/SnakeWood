# SnakeWood

Build a log with the hammer and ride it like a hover bike. It floats over ground and water,
leans into turns, hops, and each wood type handles differently.

**Install on every player's game and on the server.** The logs are new buildable objects, so
anyone without the mod won't see them.

## The logs

All are under the hammer's **Misc** tab and need a workbench nearby.

| Log | Cost | How it rides |
| --- | --- | --- |
| Hover Log | 20 Wood, 5 Resin | Balanced and forgiving. |
| Fine Hover Log | 20 Fine Wood, 5 Feathers | Light, quick and twitchy. |
| Core Wood Battering Log | 20 Core Wood, 10 Resin | Heavy and slow to get going. Rams creatures and fells trees in its way. |
| Ancient Bark Hover Log | 20 Ancient Bark, 3 Guck | Floats high enough to clear swamp roots. Its ram poisons. |
| Yggdrasil Hover Log | 20 Yggdrasil Wood, 3 Sap | Floats and hops highest, and glides down from cliffs. |
| Ashwood Hover Log | 20 Ashwood, 2 Surtling Cores | The fastest. Its ram sets things on fire. |

Each log looks like the vanilla pole or beam of its wood, laid flat.

## Controls

| Key | Does |
| --- | --- |
| **E** on a log | Get on. |
| **E** again | Get off. |
| **W / S** | Throttle and reverse. |
| **Mouse** | Steer. The log turns toward where you look, like the Lox. |
| **A / D** | Strafe. With `Steering = Keys`, A and D turn instead and the mouse looks around freely. |
| **Shift** | Boost. |
| **Space** | Hop. On other mounts Space gets you off; on a log it hops. |
| **Right mouse (block)** | Brake hard. |

Attacking or dodging also gets you off, as on the Lox.

## Ramming

Logs with **Ram Damage** above 0 hit whatever they run into at speed. Damage scales with how
fast you were going. By default Core Wood, Ancient Bark and Ashwood ram.

- Creatures take the hit and get knocked back.
- Core Wood also chops trees, logs and stumps, and chips rock.
- Build pieces, other players and tames are left alone unless you turn on
  **Ram Damages Buildings** or **Ram Damages Players And Tames**.

## Config

`BepInEx/config/com.snakesquad.snakewood.cfg`, or Configuration Manager (F1):

- **Steering**: `Camera` (default) or `Keys`.
- **Rider Pose**: `attach_lox` (straddle, default) or `attach_chair`.
- **Seat Height**: raise or lower the rider on the log.
- **Ram Damages Buildings / Players And Tames**: both off by default.
- One section per log: **Buildable**, **Top Speed**, **Boost Multiplier**, **Acceleration**,
  **Turn Speed**, **Hover Height**, **Hop Strength**, **Fall Gravity**, **Ram Damage**.

The handling settings are read from the **rider's** config while riding, so each player rides
with their own numbers. Settings aren't synced from the server.

## Removing a log

Hammer it like any build piece (with a workbench nearby) to get the materials back.
