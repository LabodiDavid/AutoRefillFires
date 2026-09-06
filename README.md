
# Auto Refill Fires

A lightweight BepInEx mod for Valheim that automatically refills nearby fireplaces and torches.

## What's New in 1.2.0
##### [Full CHANGELOG](https://github.com/LabodiDavid/AutoRefillFires/blob/main/Thunderstore/CHANGELOG.md)
- Added automatic Hot Tub refilling with a separate `FillHotTubs` toggle
- Fixed Resin candles being incorrectly treated as refillable objects
- Major internal refactor and code cleanup

## Features

- Automatically refills campfires, hearths, torches, braziers, bonfires and other compatible fireplaces
- Automatically uses the correct fuel type
- Can use fuel from the player inventory and nearby containers
- Configurable refill radius and threshold
- Refill to maximum or by a fixed amount
- Individual enable/disable options for fireplace types

## Configuration

The configuration file is generated after launching the game once with the mod installed.

`BepInEx/config/simplifydave.autorefillfires.cfg`

### Hotkey

Default:

`F7`

Pressing the hotkey toggles Auto Refill Fires on or off.

When disabled, the configuration file is automatically reloaded, allowing most settings to be changed without restarting Valheim.

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