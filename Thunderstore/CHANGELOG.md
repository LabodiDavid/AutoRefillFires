# Changelog

All notable changes to Auto Refill Fires will be documented in this file.

## [1.1.0] - 2026-08-29

### Added

- Added a configurable toggle hotkey.
  - Default: `F7`
  - Allows automatic refilling to be enabled or disabled while in-game.
  - The configuration file is automatically reloaded when Auto Refill Fires is disabled, allowing settings to be changed without restarting the game.

- Added configurable logging levels:
  - `None`
  - `Error`
  - `Warning`
  - `Info`
  - `Debug`

- Added `KeepFuelReserve`.
  - Prevents Auto Refill Fires from consuming the last configured amount of fuel from player inventories and containers.
  - Example: a reserve of `10` means the last 10 Wood, Resin, or other fuel items will be kept.

- Added configurable fuel source priority:
  - `PlayerFirst`
  - `ContainersFirst`

- Added support for using multiple nearby containers during a single refill.
  - Fuel is taken from the nearest usable containers first.
  - If the selected source cannot provide enough fuel, the plugin automatically falls back to the other configured source.

- Added `OnlyRefillOwnPieces`.
  - Optionally restricts automatic refilling to fireplaces and torches built by the local player.

- Added more detailed Debug logging for:
  - Fireplace detection
  - Range checks
  - Fuel levels and refill decisions
  - Fuel source selection
  - Nearby container availability
  - Fuel consumption

### Changed

- Significantly reduced unnecessary container scanning.
  - Containers are now discovered once per refill scan cycle and reused for all fireplaces during that cycle.
  - A container is no longer rediscovered once for every individual fuel item added.

- Fuel is now removed in batches instead of one item at a time where possible.

- Nearby containers are sorted by distance and the closest usable container is preferred.

- Improved fireplace type detection.
  - Vanilla campfires using the `fire_pit` prefab are correctly recognized as campfires.

- Improved handling of unknown and modded objects based on Valheim's `Fireplace` component.

### Fixed

- Fixed excessive scene-wide container searches when refilling fireplaces to maximum fuel.
- Fixed campfires potentially being classified as `OtherFireplaces` because of their vanilla prefab name.
- Fixed Debug messages being hidden by the default BepInEx Debug log filter.

---

## [1.0.0] - 2026-08-23

Initial release.

### Features

- Automatic campfire, hearth, standing/wall torch, brazier refilling
- Optional nearby container fuel usage
- Configurable refill radius, treshold
- Refill-to-max or fixed refill amount
- Individual fireplace type toggles