# Sounds: ZSFX, volume and distance

Read 2026-10-25 from the Decompile workflow (Valheim with Deep North), for the ValheimMapMarker
North Wisp. *(verified)* = read in the decompile.

- Vanilla sound prefabs (`sfx_*`) carry an `AudioSource` and a `ZSFX`. `ZSFX` plays in `Start`
  and then **sets `audio.volume` every update** to `m_vol × fade × m_concurrencyVolumeModifier ×
  m_volumeModifier` *(verified)*, so writing `AudioSource.volume` yourself is overwritten. Use the
  public `ZSFX.SetVolumeModifier(float)` *(verified)*; `m_minVol`/`m_maxVol` are public too.
- Distance falloff comes from the prefab's `AudioSource` (`spatialBlend`, rolloff, min/max
  distance). Some vanilla sounds are 2D or have a huge max distance, so spawned far away they
  sound as if at the player (seen in-game by the user, 2026-10-25). To make one positional, set
  on every child `AudioSource` after `Instantiate`: `spatialBlend = 1`, `rolloffMode =
  AudioRolloffMode.Linear`, `minDistance` ~2, `maxDistance` = your range. Needs
  `UnityEngine.AudioModule` referenced in the csproj.
- An `sfx_*` with a `ZNetView` plays for everyone at its spawn point; without one it's local.
- Don't borrow potion sounds (`sfx_Potion_*`): the user's group listens for them to know a
  potion was drunk.
