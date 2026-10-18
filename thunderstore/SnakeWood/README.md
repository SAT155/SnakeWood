# SnakeWood

Build a log with the hammer and ride it like a hover bike. It floats over ground and water,
leans into turns, hops, and each wood type handles differently.

**Install on every player's game and on the server.** The logs are new buildable objects, so
anyone without the mod won't see them.

## The logs

All are under the hammer's **Misc** tab and need a workbench nearby. Each one is that wood's
raw log, floating on a coloured glow, with a little saddle made from the wood's build piece.

| Log | Cost | How it rides |
| --- | --- | --- |
| Hover Log | 20 Wood, 5 Resin | Balanced and forgiving. |
| Fine Hover Log | 20 Fine Wood, 5 Feathers | Light, quick and twitchy. |
| Core Wood Battering Log | 20 Core Wood, 10 Resin | Heavy and slow, but **nothing knocks you off it**. Rams creatures and fells trees in its way. |
| Ancient Bark Hover Log | 20 Ancient Bark, 3 Guck | Floats high over swamp roots, handles water best, and **trails a poison cloud** that sickens creatures caught in it. Its ram poisons too. |
| Yggdrasil Hover Log | 20 Yggdrasil Wood, 3 Sap | Floats and hops highest, **hops twice more in mid-air**, glides down from cliffs, and trails sparks of light. |
| Ashwood Hover Log | 20 Ashwood, 2 Surtling Cores | The fastest. Trails embers, and its ram sets things on fire. |

## Controls

| Key | Does |
| --- | --- |
| **E** on a log | Get on. |
| **E** again | Get off. |
| **W / S** | Throttle and reverse. |
| **Mouse** | Steer. The log turns toward where you look, like the Lox. |
| **A / D** | Strafe. With `Steering = Keys`, A and D turn instead. |
| **Shift** | Boost (costs stamina). |
| **Space** | Hop (costs stamina). The Yggdrasil log can hop again in mid-air. |
| **Crouch (Ctrl)** | Brake hard. |
| **Attack, bow, block** | Fight from the log (Mounted Combat). |

Dodging gets you off, as on the Lox. Arrows and other projectiles fired from a log spread more:
each log has its own flat **Aim Penalty** (steady Core Wood least, twitchy Fine Wood and Ashwood
most), and moving adds more on top.

Hover over a log in the build menu to see its stats: speed, turning, hover, hop, balance cost,
crash limit, aim, deep-water speed and any ram or cloud.

## Staying on

- **Balance takes stamina.** Riding drains a little stamina the whole time, and stamina doesn't
  regenerate meanwhile. Fine Wood and Ashwood are hardest to hold on to and Core Wood easiest.
  Run out and you fall off.
- **Crashes throw you.** Slam into a tree, rock or wall hard enough and you're thrown off, carrying
  some of your speed. The Core Wood log never throws you.
- **Going under water throws you.** If a dive takes you below the surface you lose your grip,
  and the log bobs back up.

## Water

Over shallow water (rivers, lakes, the shore) logs ride as they do on land. Over **deep water**
the log sinks to skim the surface and slows right down (about a third of its land speed; the
Ancient Bark log keeps more). Crossing the ocean on a log is possible, but slow and tiring;
boats are still the way to sail. An empty log rests on the water like a real one.

## Ramming

Logs with **Ram Damage** above 0 hit whatever they run into at speed. Damage scales with how
fast you were going. By default Core Wood, Ancient Bark and Ashwood ram.

- Creatures take the hit and get knocked back.
- Core Wood also chops trees, logs and stumps, and chips rock.
- Build pieces, other players and tames are left alone (by the ram and the poison cloud) unless
  you turn on **Ram Damages Buildings** or **Ram Damages Players And Tames**.

## Config

`BepInEx/config/com.snakesquad.snakewood.cfg`, or Configuration Manager (F1):

- **Steering**: `Camera` (default) or `Keys`.
- **Rider Pose**: `attach_lox` (straddle, default) or `attach_chair`.
- **Seat Height**: raise or lower the rider on the log.
- **Mounted Combat**: on by default. Off makes attacking get you off, like the Lox.
- **Model**: `Mixed` (raw log plus build-piece saddle, default), `RawWood`, or `BuildPiece`
  (the build piece laid flat, as in 1.0). Needs a restart.
- **Hover Glow**: the coloured light under each log.
- **Balance**: **Boost Stamina Per Second**, **Hop Stamina**, **Deep Water Depth** and
  **Aim Penalty Per Speed**.
- **Ram Damages Buildings / Players And Tames**: both off by default.
- One section per log: **Buildable**, **Aim Penalty**, **Riding Stamina Per Second**, **Top Speed**,
  **Boost Multiplier**, **Acceleration**, **Turn Speed**, **Hover Height**, **Hop Height**,
  **Air Hops**, **Fall Gravity**, **Crash Speed**, **Deep Water Speed**, **Ram Damage**, and
  **Poison Cloud Damage** for Ancient Bark.

The handling settings are read from the **rider's** config while riding, so each player rides
with their own numbers. Settings aren't synced from the server.

## Removing a log

Hammer it like any build piece (with a workbench nearby) to get the materials back.
