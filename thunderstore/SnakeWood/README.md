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
| Fine Hover Log | 20 Fine Wood, 10 Resin, 3 Ancient Seeds | Light, quick and twitchy: a scout. Fast enough to run down a hare, but carries little. |
| Core Wood Battering Log | 20 Core Wood, 10 Resin, 3 Ancient Seeds | Heavy and slow, but **nothing knocks you off it** and it hauls the heaviest loads. Rams creatures and fells trees in its way. |
| Ancient Bark Hover Log | 20 Ancient Bark, 10 Resin, 3 Withered Bones | Floats high over swamp roots (and leeches), handles water best, carries a lot, and **trails a poison cloud** that sickens creatures caught in it. Its ram poisons too. |
| Yggdrasil Hover Log | 20 Yggdrasil Wood, 10 Resin, 2 Dvergr Key Fragments | The Mistlands scout: quiet, climbs steepest, floats and hops highest, **hops twice more in mid-air**, glides down from cliffs, and trails sparks of light. Fast enough to catch a hare. |
| Ashwood Hover Log | 20 Ashwood, 10 Resin, 1 Bell Fragment | The fastest, just. Trails embers, and its ram sets things on fire. |

### Fish logs

Every kind of fish can be ridden too, but only a **level 5 fish** will do (the rarest catch;
`Fish Quality Required`). They're hard to get, so they ride well: light on stamina, mostly
fast on water, and each has a trick. Repair them with raw fish.

| Log | Cost (plus 1 level-5 fish, 10 Resin) | How it rides |
| --- | --- | --- |
| Hover Perch | 2 Deer Trophies | The easy one: steady, cheap on stamina, quick on rivers and sea. |
| Hover Pike | 5 Serpent Scales | Built for the open sea, 2.2x its land speed over deep water. |
| Hover Tuna | 5 Serpent Scales | The fastest thing at sea (2.7x) and tireless, but sluggish on land. |
| Hover Tetra | 5 Freeze Glands | Mountain goat of a fish: climbs 45° slopes, hops with an extra mid-air hop, trails frost. |
| Hover Trollfish | 3 Withered Bones | Tough swamp brute: never throws you, rides high over leeches, hauls 600, rams. |
| Hover Herring | 1 Dvergr Key Fragment | Quiet, agile scout for the Mistlands coast. |
| Hover Grouper | 2 Fuling Totems | Broad sea hauler (600) that rams and shoves. |
| Hover Coral Cod | 2 Fuling Totems | Smooth and steady: the best fish to shoot from. |
| Hover Anglerfish | 1 Dvergr Key Fragment | Its lure is a bright lamp for night and mist. Quiet. |
| Hover Salmon | 1 Bell Fragment | Leaps: big hops plus a mid-air hop, fast in northern seas. |
| Hover Magmafish | 1 Bell Fragment | Trails embers and sets what it rams on fire. |
| Hover Pufferfish | 5 Serpent Scales | Poisons what it rams and trails a toxic cloud. |

Logs are scouting and utility rides, not a faster way to haul everything: the quickest are only
a little faster than a ridden Lox, and only with light packs.

| Log | Top speed (boost) | Climbs | Carries | Durability | Noise |
| --- | --- | --- | --- | --- | --- |
| Hover Log | 7 (9.1) m/s | 30° | 300 | 100 | normal |
| Fine Hover Log | 9 (13) m/s | 35° | 180 | 60 | a little quieter |
| Core Wood Battering Log | 5.5 (7.2) m/s | 22° | 700 | 300 | loud |
| Ancient Bark Hover Log | 7 (9.5) m/s | 30° | 500 | 150 | normal |
| Yggdrasil Hover Log | 9 (13) m/s | 40° | 200 | 120 | quiet |
| Ashwood Hover Log | 9.5 (13.8) m/s | 35° | 250 | 150 | a little louder |

Updating from 1.2 resets each log's Top Speed and Boost Multiplier to these new defaults once
(other settings you changed are kept).

## Passengers

Other players can jump onto a log and ride along standing on it while someone drives, the way
you stand on a ship's deck. Hold on through the turns!

## Fuel

Every log burns **resin**. A new log starts empty.

- **Shift+E** on a log tops it up from your inventory (up to 20). Using resin on it from the
  hotbar adds one at a time.
- **R** while looking at a log takes the whole resin back out (`Take Out Fuel Key`).
  Deconstructing a log gives back its resin too.
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
crash limit, aim, deep-water speed, climb angle, load capacity, noise, durability and any ram or cloud.

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

## Weight, slopes and noise

- **Carry weight.** Each log lifts up to its **Load Capacity** (your whole inventory weight,
  gear included). Up to about a third of that makes no difference. Above it, the fuller the log,
  the lower its top speed (up to 45% slower), hover and hop height (up to half), and the
  sluggier its acceleration and turning. Over capacity it won't take off, and if you pick up too
  much mid-ride it sputters along low and slow. Core Wood and Ancient Bark carry the most.
- **Slopes.** Each log has a **Max Climb Angle**. Ground steeper than that stops it (hop over it
  or go round), and the log's nose never tips further than that either, from the ground or from
  looking up and down.
- **Noise.** Riding is loud: creatures hear a log idling from about 8 m and one at full speed from
  about 35 m (a running player is about 30). The Yggdrasil log is quietest, Core Wood loudest.
- **No ore or metal** (off by default). Turn on `No Ore Or Metal` and logs refuse anything you
  couldn't take through a portal.

## Wear

Logs wear out from crashes (harder hits wear more, even on Core Wood, which never throws you)
and from ramming. Just riding doesn't wear them. Hover over a log to see its condition.

- A worn-out log won't take off, and one that wears out mid-ride sputters along low and slow.
- **Shift+E** repairs it with **its own wood** (Wood, Fine Wood, Core Wood, Ancient Bark,
  Yggdrasil Wood, Ashwood; raw fish for the fish logs), 10 wear per piece, before it adds any fuel.
- Logs never break or lose materials by default. `Break When Worn Out` makes a worn-out log fall
  apart instead, dropping its fuel and `Broken Material Return` (half) of its materials.

## Water

Over shallow water (rivers, lakes, the shore) logs ride as they do on land. Over **deep water**
the log sinks to skim the surface and slows right down (about a third of its land speed; the
Ancient Bark log keeps more). The **fish logs** are the exception: most are faster at sea than on land. Crossing the ocean on a log is possible, but slow and tiring;
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
- **Trails**, **Trail Opacity** and **Trail Density**: turn the particle trails off, make them fainter, or thicker.
- **Balance**: **Boost Stamina Per Second**, **Hop Stamina**, **Deep Water Depth**,
  **Aim Penalty Per Speed**, **Eitr Per Second** and **Stamina Drain With Eitr**.
  Also **Fish Quality Required** (5), **No Ore Or Metal** (off), the load settings (**Load Free Share**, **Load Speed Penalty**,
  **Load Height Penalty**, **Load Handling Penalty**), and **Noise At Rest** / **Noise At Top Speed**.
- **Fuel**: **Fuel Item** (Resin), **Fuel Capacity**, **Fuel Per Minute** (0 turns fuel off) and
  **Take Out Fuel Key** (R).
- **Wear**: **Wear** (on), **Crash Wear**, **Ram Wear**, **Repair Per Item**, **Break When Worn Out** (off) and **Broken Material Return**.
- **Ram Damages Buildings / Players And Tames**: both off by default.
- One section per log: **Buildable**, **Recipe**, **Aim Penalty**, **Riding Stamina Per Second**, **Top Speed**,
  **Boost Multiplier**, **Acceleration**, **Turn Speed**, **Hover Height**, **Hop Height**,
  **Air Hops**, **Fall Gravity**, **Crash Speed**, **Deep Water Speed**, **Max Climb Angle**,
  **Load Capacity**, **Durability**, **Repair Item**, **Noise**, **Ram Damage**, and
  **Poison Cloud Damage** for Ancient Bark.

The handling settings are read from the **rider's** config while riding, so each player rides
with their own numbers. Settings aren't synced from the server.

## Removing a log

Hammer it like any build piece (with a workbench nearby) to get the materials and any resin
in it back.
