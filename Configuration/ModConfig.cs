using BepInEx.Configuration;
using UnityEngine;

namespace AutoRefillFires
{
    internal class ModConfig
    {
        public ConfigEntry<float> Radius { get; }
        public ConfigEntry<float> CheckInterval { get; }
        public ConfigEntry<float> RefillBelowPercent { get; }

        public ConfigEntry<bool> UseNearbyContainers { get; }
        public ConfigEntry<float> ContainerRadius { get; }

        public ConfigEntry<bool> RefillToMax { get; }
        public ConfigEntry<int> RefillAmount { get; }

        public ConfigEntry<bool> FillCampfires { get; }
        public ConfigEntry<bool> FillHearths { get; }
        public ConfigEntry<bool> FillStandingTorches { get; }
        public ConfigEntry<bool> FillWallTorches { get; }
        public ConfigEntry<bool> FillBraziers { get; }
        public ConfigEntry<bool> FillBonfires { get; }
        public ConfigEntry<bool> FillHotTubs { get; }
        public ConfigEntry<bool> FillOtherFireplaces { get; }

        public ConfigEntry<int> KeepFuelReserve { get; }
        public ConfigEntry<FuelSourcePriority> FuelSourcePriority { get; }

        public ConfigEntry<bool> OnlyRefillOwnPieces { get; }
        public ConfigEntry<KeyboardShortcut> ToggleHotkey { get; }
        public ConfigEntry<ModLogLevel> LogLevel { get; }

        public ModConfig(ConfigFile config)
        {
            Radius = config.Bind("General", "Radius", 20f, "Refill fireplaces within this radius around the player.");
            CheckInterval = config.Bind("General", "CheckInterval", 3f, "How often fireplaces are checked, in seconds.");
            RefillBelowPercent = config.Bind("General", "RefillBelowPercent", 0.75f, "Refill when fuel drops below this percentage.");
            RefillToMax = config.Bind("General", "RefillToMax", true, "If true, refill fireplaces to maximum fuel. If false, add only RefillAmount fuel.");
            RefillAmount = config.Bind("General", "RefillAmount", 5, "Amount of fuel to add when RefillToMax is false.");

            UseNearbyContainers = config.Bind("Containers", "UseNearbyContainers", false, "Allow plugin to take fuel from nearby containers.");
            ContainerRadius = config.Bind("Containers", "ContainerRadius", 10f, "Maximum distance from the fireplace to search for containers.");

            FillCampfires = config.Bind("Fireplace Types", "FillCampfires", true, "Automatically refill campfires.");
            FillHearths = config.Bind("Fireplace Types", "FillHearths", true, "Automatically refill hearths.");
            FillStandingTorches = config.Bind("Fireplace Types", "FillStandingTorches", true, "Automatically refill all standing torches, including colored variants.");
            FillWallTorches = config.Bind("Fireplace Types", "FillWallTorches", true, "Automatically refill all wall torches, including colored variants.");
            FillBraziers = config.Bind("Fireplace Types", "FillBraziers", true, "Automatically refill braziers.");
            FillBonfires = config.Bind("Fireplace Types", "FillBonfires", true, "Automatically refill bonfires.");
            FillHotTubs = config.Bind("Fireplace Types", "FillHotTubs", true, "Automatically refill hot tubs.");
            FillOtherFireplaces = config.Bind("Fireplace Types", "FillOtherFireplaces", true, "Automatically refill unknown or modded Fireplace-based objects.");

            ToggleHotkey = config.Bind("General", "ToggleHotkey", new KeyboardShortcut(KeyCode.F7), "Hotkey used to enable or disable automatic refilling.");
            KeepFuelReserve = config.Bind("Fuel", "KeepFuelReserve", 0, "Minimum amount of fuel to keep in the source inventory. 10 means the last 10 Wood/Resin will not be used.");
            FuelSourcePriority = config.Bind("Fuel", "FuelSourcePriority", AutoRefillFires.FuelSourcePriority.PlayerFirst, "Select whether player inventory or nearby containers should be used first.");
            OnlyRefillOwnPieces = config.Bind("General", "OnlyRefillOwnPieces", false, "If enabled, only refill fireplaces and torches built by the local player.");
            LogLevel = config.Bind("Logging", "LogLevel", ModLogLevel.Info, "Logging verbosity. Available values: None, Error, Warning, Info, Debug.");
        }
    }
}
