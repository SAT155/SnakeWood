# SnakeWood

Build a log with the hammer and ride it like a hover bike. It floats over ground and water,
leans into turns, hops, and each wood type handles differently.

**Install on every player's game and on the server** to build and ride logs. Players without the
mod still see something: each log is saved as a vanilla half log (what a felled tree splits
into), so to them it's a log floating along with the rider (they can't ride or chop it). The game
can't shrink that log for them, so the real hover log grows to about the same size instead. If the mod is ever removed,
saved logs become ordinary tree logs. `Vanilla Disguise` turns this off.

## The logs

All are under the hammer's **Misc** tab and need a workbench nearby. Each one is that wood's
raw log, floating on a coloured glow, with a little saddle made from the wood's build piece.

Each log is gated behind a boss: the magic that makes wood float comes from that boss's
summoning item. Recipes are config entries (`Recipe = Wood:20,Resin:10,TrophyDeer:2`), so a
server admin can change them.

| Log | Cost | How it rides |
| --- | --- | --- |
| Hover Log | 20 Wood, 10 Resin, 2 Deer Trophies | Balanced and forgiving. |
| Fine Hover Log | 20 Fine Wood, 10 Resin, 3 Ancient Seeds | Light, quick and twitchy. |
| Core Wood Battering Log | 20 Core Wood, 10 Resin, 3 Ancient Seeds | Heavy and slow, but **nothing knocks you off it**. Rams creatures and fells trees in its way. |
| Ancient Bark Hover Log | 20 Ancient Bark, 10 Resin, 3 Withered Bones | Floats high over swamp roots, handles water best, and **trails a poison cloud** that sickens creatures caught in it. Its ram poisons too. |
| Yggdrasil Hover Log | 20 Yggdrasil Wood, 10 Resin, 2 Dvergr Key Fragments | Floats and hops highest, **hops twice more in mid-air**, glides down from cliffs, and trails sparks of light. |
| Hover Pike | 1 Pike, 10 Resin, 5 Serpent Scales | A giant enchanted pike for the open sea: faster than a longship over deep water (twice its land speed), but slow and floppy on land. Gentle stamina drain, but long crossings still wear you down. |
| Ashwood Hover Log | 20 Ashwood, 10 Resin, 1 Bell Fragment | The fastest. Trails embers, and its ram sets things on fire. |

## Passengers

Other players can jump onto a log and ride along standing on it while someone drives, the way
you stand on a ship's deck. Hold on through the turns!

## Fuel

Every log burns **resin**. A new log starts empty.

- **Shift+E** on a log tops it up from your inventory (up to 20). Using resin on it from the
  hotbar adds one at a time.
- Riding burns about 1 resin a minute. A parked log burns nothing.
- An empty log won't lift off. If one runs dry mid-ride it sputters along low and slow, with no
  boost or hops, until you refuel.

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
| **Look up / down** | Tip the nose. Nose up rides higher at speed to clear rising ground; nose down sits lower and dives faster. |
| **Attack, bow, block** | Fight from the log (Mounted Combat). |

When you attack, draw a bow or block, your rider stands up on the log for the move and sits
back down a moment after (`Combat Stance = Seated` tries staying seated instead; experimental). Dodging gets you off, as on the Lox. Arrows and other projectiles fired from a log spread more:
each log has its own flat **Aim Penalty** (steady Core Wood least, twitchy Fine Wood and Ashwood
most), and moving adds more on top.

Hover over a log in the build menu to see its stats: speed, turning, hover, hop, balance cost,
crash limit, aim, deep-water speed and any ram or cloud.

## Staying on

- **Balance takes stamina.** Riding drains a little stamina the whole time, and stamina doesn't
  regenerate meanwhile. Fine Wood and Ashwood are hardest to hold on to and Core Wood easiest.
  Run out and you fall off.
- **Eitr helps.** If you have eitr (Mistlands food), riding draws a slow trickle of it
  (0.5/s) and your balance stamina drain drops to a quarter.
- **Crashes throw you.** Slam into a tree, rock or wall hard enough and you're thrown off, carrying
  some of your speed. The Core Wood log never throws you.
- **Going under water throws you.** If a dive takes you below the surface you lose your grip,
  and the log bobs back up.

## Water

Over shallow water (rivers, lakes, the shore) logs ride as they do on land. Over **deep water**
the log sinks to skim the surface and slows right down (about a third of its land speed; the
Ancient Bark log keeps more). The **Hover Pike** is the exception: it's twice as fast at sea. Crossing the ocean on a log is possible, but slow and tiring;
boats are still the way to sail. An empty log rests on the water like a real one.

## Ramming

Logs with **Ram Damage** above 0 hit whatever they run into at speed. Damage scales with how
fast the log was moving into it; a parked or stopped log doesn't hurt anything that walks into it. By default Core Wood, Ancient Bark and Ashwood ram.

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
- **Combat Stance**: `Stand` (default) or `Seated` (experimental).
- **Vanilla Disguise**: on by default (see the top of this page).
- **Pitch With Look** and **Max Pitch**: nose tilt from looking up or down.
- **Model**: `Mixed` (raw log plus build-piece saddle, default), `RawWood`, or `BuildPiece`
  (the build piece laid flat, as in 1.0). Needs a restart.
- **Hover Glow**: the coloured light under each log.
- **Trails** and **Trail Opacity**: turn the particle trails off, or make them fainter.
- **Balance**: **Boost Stamina Per Second**, **Hop Stamina**, **Deep Water Depth**,
  **Aim Penalty Per Speed**, **Eitr Per Second** and **Stamina Drain With Eitr**.
- **Fuel**: **Fuel Item** (Resin), **Fuel Capacity** and **Fuel Per Minute** (0 turns fuel off).
- **Ram Damages Buildings / Players And Tames**: both off by default.
- One section per log: **Buildable**, **Recipe**, **Aim Penalty**, **Riding Stamina Per Second**, **Top Speed**,
  **Boost Multiplier**, **Acceleration**, **Turn Speed**, **Hover Height**, **Hop Height**,
  **Air Hops**, **Fall Gravity**, **Crash Speed**, **Deep Water Speed**, **Ram Damage**, and
  **Poison Cloud Damage** for Ancient Bark.

The handling settings are read from the **rider's** config while riding, so each player rides
with their own numbers. Settings aren't synced from the server.

## Removing a log

Hammer it like any build piece (with a workbench nearby) to get the materials back.
