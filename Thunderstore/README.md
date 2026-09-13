
# Auto Refill Fires

A lightweight BepInEx mod for Valheim that automatically refills nearby fireplaces and torches.

## What's New in 1.2.2
##### [Full CHANGELOG](https://github.com/LabodiDavid/AutoRefillFires/blob/main/Thunderstore/CHANGELOG.md)
- Added optional automatic repair for all supported refillable object types.
- Repair follows the existing `Fill*` type toggles, including Hot Tubs.
- Added configurable repair health threshold and own-piece restriction.

## Features

- Automatically refills campfires, hearths, torches, braziers, bonfires, hot tubs and other compatible fireplaces
- Can use fuel from the player inventory and nearby containers
- Configurable refill radius and threshold
- Refill to maximum or by a fixed amount
- Individual enable/disable options for fireplace types
- Optional automatic repair for supported refillable objects; repair follows the existing `Fill*` type toggles

## Configuration

The configuration file is generated after launching the game once with the mod installed.

`BepInEx/config/simplifydave.autorefillfires.cfg`

### Hotkey

Default:

`F7`

Pressing the hotkey toggles Auto Refill Fires on or off.

When disabled, the configuration file is automatically reloaded, allowing most settings to be changed without restarting Valheim.

### Auto Repair Fires

Automatic repair is disabled by default. Enable it with:

`AutoRepair = true` in the config file.

By default supported objects are repaired when their health falls below 75%:

`RepairBelowPercent = 0.75`

Repair follows the existing `Fill*` type toggles. For example, if `FillHearths = true`, Hearths can also be repaired; if `FillHearths = false`, they are ignored by both refill and repair. The same rule applies to campfires, standing/wall torches, braziers, bonfires, Hot Tubs and compatible `OtherFireplaces`.

Set `OnlyRepairOwnPieces = true` to restrict automatic repair to pieces built by the local player.

The repair logic reuses the existing fireplace and Hot Tub scans, so enabling repair does not add another scene-wide search. Resin candles remain ignored because they are consumable, non-refillable light sources.

### Fuel Reserve

Example:

`KeepFuelReserve = 10`

The mod will leave the last 10 matching fuel items untouched in each fuel source.

### Fuel Source Priority

Available values:

`PlayerFirst`

`ContainersFirst`

If the preferred source does not contain enough usable fuel, the other source is used automatically.

### Logging

Available levels:

`None`, `Error`, `Warning`, `Info`, `Debug`

Use `Debug` to see fireplace detection, fuel levels, refill decisions and fuel source selection.

## Requirements

- Valheim
- BepInExPack Valheim

## Installation

Install using r2modman / Thunderstore Mod Manager, or manually place `AutoRefillFires.dll` inside:

`BepInEx/plugins/AutoRefillFires/`

## License

MIT