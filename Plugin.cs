using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AutoRefillFires
{
    public enum ModLogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }
    public enum FuelSourcePriority
    {
        PlayerFirst,
        ContainersFirst
    }

    [BepInPlugin(
        Plugin.PluginGuid,
        Plugin.PluginName,
        Plugin.PluginVersion
    )]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "simplifydave.autorefillfires";
        public const string PluginName = "Auto Refill Fires";
        public const string PluginVersion = VersionInfo.Version;

        private ConfigEntry<float> _radius;
        private ConfigEntry<float> _checkInterval;
        private ConfigEntry<float> _refillBelowPercent;

        private ConfigEntry<bool> _useNearbyContainers;
        private ConfigEntry<float> _containerRadius;

        private ConfigEntry<bool> _refillToMax;
        private ConfigEntry<int> _refillAmount;
        private ConfigEntry<bool> _fillCampfires;
        private ConfigEntry<bool> _fillHearths;
        private ConfigEntry<bool> _fillStandingTorches;
        private ConfigEntry<bool> _fillWallTorches;
        private ConfigEntry<bool> _fillBraziers;
        private ConfigEntry<bool> _fillBonfires;
        private ConfigEntry<bool> _fillHotTubs;
        private ConfigEntry<bool> _fillOtherFireplaces;
        private ConfigEntry<int> _keepFuelReserve;
        private ConfigEntry<FuelSourcePriority> _fuelSourcePriority;
        private ConfigEntry<bool> _onlyRefillOwnPieces;
        private ConfigEntry<KeyboardShortcut> _toggleHotkey;
        private ConfigEntry<ModLogLevel> _logLevel;
        private bool _modEnabled = true;

        private float _nextCheckTime;

        private class ContainerCandidate
        {
            public Container Container;
            public float Distance;
        }
        private class FireplaceCandidate
        {
            public Fireplace Fireplace;
            public float Distance;
            public float FuelPercent;
        }

        private void Awake()
        {
            _radius = Config.Bind(
                "General",
                "Radius",
                20f,
                "Refill fireplaces within this radius around the player."
            );

            _checkInterval = Config.Bind(
                "General",
                "CheckInterval",
                3f,
                "How often fireplaces are checked, in seconds."
            );

            _refillBelowPercent = Config.Bind(
                "General",
                "RefillBelowPercent",
                0.75f,
                "Refill when fuel drops below this percentage."
            );

            _refillToMax = Config.Bind(
                "General",
                "RefillToMax",
                true,
                "If true, refill fireplaces to maximum fuel. If false, add only RefillAmount fuel."
            );

            _refillAmount = Config.Bind(
                "General",
                "RefillAmount",
                5,
                "Amount of fuel to add when RefillToMax is false."
            );

            _useNearbyContainers = Config.Bind(
                "Containers",
                "UseNearbyContainers",
                false,
                "Allow plugin to take fuel from nearby containers."
            );

            _containerRadius = Config.Bind(
                "Containers",
                "ContainerRadius",
                10f,
                "Maximum distance from the fireplace to search for containers."
            );

            _fillCampfires = Config.Bind(
                "Fireplace Types",
                "FillCampfires",
                true,
                "Automatically refill campfires."
            );

            _fillHearths = Config.Bind(
                "Fireplace Types",
                "FillHearths",
                true,
                "Automatically refill hearths."
            );

            _fillStandingTorches = Config.Bind(
                "Fireplace Types",
                "FillStandingTorches",
                true,
                "Automatically refill all standing torches, including colored variants."
            );

            _fillWallTorches = Config.Bind(
                "Fireplace Types",
                "FillWallTorches",
                true,
                "Automatically refill all wall torches, including colored variants."
            );

            _fillBraziers = Config.Bind(
                "Fireplace Types",
                "FillBraziers",
                true,
                "Automatically refill braziers."
            );

            _fillBonfires = Config.Bind(
                "Fireplace Types",
                "FillBonfires",
                true,
                "Automatically refill bonfires."
            );

            _fillHotTubs = Config.Bind(
                "Fireplace Types",
                "FillHotTubs",
                true,
                "Automatically refill hot tubs."
            );

            _fillOtherFireplaces = Config.Bind(
                "Fireplace Types",
                "FillOtherFireplaces",
                true,
                "Automatically refill unknown or modded Fireplace-based objects."
            );

            _toggleHotkey = Config.Bind(
                "General",
                "ToggleHotkey",
                new KeyboardShortcut(KeyCode.F7),
                "Hotkey used to enable or disable automatic refilling."
            );

            _keepFuelReserve = Config.Bind(
                "Fuel",
                "KeepFuelReserve",
                0,
                "Minimum amount of fuel to keep in the source inventory. 10 means the last 10 Wood/Resin will not be used."
            );

            _fuelSourcePriority = Config.Bind(
                "Fuel",
                "FuelSourcePriority",
                FuelSourcePriority.PlayerFirst,
                "Select whether player inventory or nearby containers should be used first."
            );

            _onlyRefillOwnPieces = Config.Bind(
                "General",
                "OnlyRefillOwnPieces",
                false,
                "If enabled, only refill fireplaces and torches built by the local player."
            );

            _logLevel = Config.Bind(
                "Logging",
                "LogLevel",
                ModLogLevel.Info,
                "Logging verbosity. Available values: None, Error, Warning, Info, Debug."
            );

            Logger.LogInfo("Auto Refill Fires loaded!");
        }

        private void Update()
        {
            if (_toggleHotkey.Value.IsDown())
            {
                _modEnabled = !_modEnabled;

                // Reload config when turning the mod OFF
                if (!_modEnabled)
                {
                    try
                    {
                        Config.Reload();
                        LogInfo("Configuration reloaded.");
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"Failed to reload configuration: {ex}");
                    }
                }

                string status = _modEnabled
                    ? "Auto Refill Fires: ENABLED"
                    : "Auto Refill Fires: DISABLED";

                Player player = Player.m_localPlayer;

                if (player != null)
                {
                    player.Message(
                        MessageHud.MessageType.Center,
                        status
                    );
                }

                LogInfo(status);
            }

            if (!_modEnabled)
                return;

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + _checkInterval.Value;

            Player localPlayer = Player.m_localPlayer;

            if (localPlayer == null)
                return;

            RefillNearbyFireplaces(localPlayer);
        }

        private bool ShouldRefillFireplace(Fireplace fireplace)
        {
            string objectName = fireplace.gameObject.name
                .Replace("(Clone)", "")
                .ToLowerInvariant();

            bool result;
            string matchedRule;

            if (objectName.Contains("fire_pit") || objectName.Contains("firepit"))
            {
                result = _fillCampfires.Value;
                matchedRule = "FillCampfires";
            }
            else if (objectName.Contains("hearth"))
            {
                result = _fillHearths.Value;
                matchedRule = "FillHearths";
            }
            else if (objectName.Contains("groundtorch"))
            {
                result = _fillStandingTorches.Value;
                matchedRule = "FillStandingTorches";
            }
            else if (objectName.Contains("walltorch"))
            {
                result = _fillWallTorches.Value;
                matchedRule = "FillWallTorches";
            }
            else if (objectName.Contains("brazier"))
            {
                result = _fillBraziers.Value;
                matchedRule = "FillBraziers";
            }
            else if (objectName.Contains("bonfire"))
            {
                result = _fillBonfires.Value;
                matchedRule = "FillBonfires";
            }
            else if (objectName.Contains("hottub") || objectName.Contains("hot_tub"))
            {
                result = _fillHotTubs.Value;
                matchedRule = "FillHotTubs";
            }
            else
            {
                result = _fillOtherFireplaces.Value;
                matchedRule = "FillOtherFireplaces";
            }

            LogDebug(
                $"ShouldRefillFireplace: name='{fireplace.gameObject.name}', " +
                $"normalized='{objectName}', matchedRule={matchedRule}, result={result}"
            );

            return result;
        }

        private void RefillNearbyFireplaces(Player player)
        {
            Fireplace[] fireplaces =
                Object.FindObjectsByType<Fireplace>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            Smelter[] smelters = null;

            if (_fillHotTubs.Value)
            {
                smelters =
                    Object.FindObjectsByType<Smelter>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None
                    );
            }

            Container[] containers = null;

            if (_useNearbyContainers.Value)
            {
                containers =
                    Object.FindObjectsByType<Container>(
                        FindObjectsInactive.Exclude,
                        FindObjectsSortMode.None
                    );
            }

            List<FireplaceCandidate> candidates =
                new List<FireplaceCandidate>();

            foreach (Fireplace fireplace in fireplaces)
            {
                if (fireplace == null)
                    continue;

                float distance = Vector3.Distance(
                    player.transform.position,
                    fireplace.transform.position
                );

                if (distance > _radius.Value)
                    continue;

                ZNetView nview =
                    fireplace.GetComponent<ZNetView>();

                if (nview == null)
                    nview = fireplace.GetComponentInParent<ZNetView>();

                if (nview == null || !nview.IsValid())
                    continue;

                ZDO zdo = nview.GetZDO();

                if (zdo == null)
                    continue;

                float maxFuel =
                    fireplace.m_maxFuel;

                if (maxFuel <= 0f)
                    continue;

                float currentFuel =
                    zdo.GetFloat(
                        ZDOVars.s_fuel,
                        0f
                    );

                float fuelPercent =
                    currentFuel / maxFuel;

                candidates.Add(
                    new FireplaceCandidate
                    {
                        Fireplace = fireplace,
                        Distance = distance,
                        FuelPercent = fuelPercent
                    }
                );
            }

            /*
             * Priority:
             *
             * 1. Lowest fuel percentage first
             * 2. If equal, nearest fireplace first
             */
            candidates.Sort(
                (a, b) =>
                {
                    int fuelCompare =
                        a.FuelPercent.CompareTo(
                            b.FuelPercent
                        );

                    if (fuelCompare != 0)
                        return fuelCompare;

                    return a.Distance.CompareTo(
                        b.Distance
                    );
                }
            );

            LogDebug(
                $"Scan start: " +
                $"loadedFireplaces={fireplaces.Length}, " +
                $"inRange={candidates.Count}, " +
                $"loadedContainers={(containers != null ? containers.Length : 0)}, " +
                $"radius={_radius.Value:0.0}m"
            );

            foreach (FireplaceCandidate candidate in candidates)
            {
                LogDebug(
                    $"Priority: " +
                    $"name='{candidate.Fireplace.gameObject.name}', " +
                    $"fuel={candidate.FuelPercent * 100f:0.0}%, " +
                    $"distance={candidate.Distance:0.0}m"
                );

                TryRefillFireplace(
                    player,
                    candidate.Fireplace,
                    containers
                );
            }

            if (_fillHotTubs.Value && smelters != null)
            {
                RefillNearbyHotTubs(
                    player,
                    smelters,
                    containers
                );
            }

            LogDebug("Scan end.");
        }

        private void RefillNearbyHotTubs(
            Player player,
            Smelter[] smelters,
            Container[] containers)
        {
            int hotTubsInRange = 0;

            foreach (Smelter smelter in smelters)
            {
                if (smelter == null)
                    continue;

                string objectName =
                    smelter.gameObject.name
                        .Replace("(Clone)", "")
                        .ToLowerInvariant();

                if (objectName != "piece_bathtub")
                    continue;

                float distance = Vector3.Distance(
                    player.transform.position,
                    smelter.transform.position
                );

                if (distance > _radius.Value)
                    continue;

                hotTubsInRange++;

                LogDebug(
                    $"Hot tub in range: " +
                    $"name='{smelter.gameObject.name}', " +
                    $"distance={distance:0.0}m"
                );

                TryRefillHotTub(
                    player,
                    smelter,
                    containers
                );
            }

            LogDebug(
                $"Hot tub scan end: inRange={hotTubsInRange}"
            );
        }

        private void TryRefillHotTub(
    Player player,
    Smelter smelter,
    Container[] containers)
        {
            if (smelter == null)
                return;

            ZNetView nview =
                smelter.GetComponent<ZNetView>();

            if (nview == null)
                nview = smelter.GetComponentInParent<ZNetView>();

            if (nview == null || !nview.IsValid())
            {
                LogDebug("Hot tub: invalid ZNetView.");
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                LogDebug("Hot tub: ZDO is null.");
                return;
            }

            if (
                smelter.m_fuelItem == null ||
                smelter.m_fuelItem.m_itemData == null ||
                smelter.m_fuelItem.m_itemData.m_shared == null
            )
            {
                LogDebug("Hot tub: fuel item is null.");
                return;
            }

            string fuelName =
                smelter.m_fuelItem.m_itemData.m_shared.m_name;

            float currentFuel =
                zdo.GetFloat(
                    ZDOVars.s_fuel,
                    0f
                );

            float maxFuel =
                smelter.m_maxFuel;

            if (maxFuel <= 0f)
                return;

            float fuelPercent =
                currentFuel / maxFuel;

            LogDebug(
                $"Hot tub fuel state: " +
                $"fuel='{fuelName}', " +
                $"current={currentFuel:0.00}, " +
                $"max={maxFuel:0.00}, " +
                $"percent={fuelPercent * 100f:0.0}%, " +
                $"threshold={_refillBelowPercent.Value * 100f:0.0}%"
            );

            if (fuelPercent >= _refillBelowPercent.Value)
            {
                LogDebug(
                    "Skipping hot tub - fuelPercent >= threshold."
                );

                return;
            }

            int missingFuel =
                Mathf.CeilToInt(
                    maxFuel - currentFuel
                );

            int requestedFuel =
                _refillToMax.Value
                    ? missingFuel
                    : Mathf.Min(
                        _refillAmount.Value,
                        missingFuel
                    );

            if (requestedFuel <= 0)
                return;

            LogDebug(
                $"Hot tub refill requested: " +
                $"missing={missingFuel}, " +
                $"requested={requestedFuel}"
            );

            int fuelConsumed =
                ConsumeFuel(
                    player,
                    smelter.transform,
                    fuelName,
                    requestedFuel,
                    containers
                );

            if (fuelConsumed <= 0)
            {
                LogDebug(
                    "Hot tub refill stopped: no usable fuel."
                );

                return;
            }

            for (int i = 0; i < fuelConsumed; i++)
            {
                nview.InvokeRPC(
                    "RPC_AddFuel",
                    new object[0]
                );
            }

            LogInfo(
                $"Refilled hot tub: " +
                $"{currentFuel:0.0}/{maxFuel:0.0}, " +
                $"fuel={fuelName}, " +
                $"added={fuelConsumed}, " +
                $"mode={(_refillToMax.Value ? "max" : "fixed")}"
            );
        }

        private bool IsOwnPiece(Player player, Fireplace fireplace)
        {
            Piece piece = fireplace.GetComponent<Piece>();

            if (piece == null)
                piece = fireplace.GetComponentInParent<Piece>();

            if (piece == null)
            {
                // Unknown/modded fireplace without a Piece component.
                // Don't block it.
                return true;
            }

            return piece.GetCreator() == player.GetPlayerID();
        }

        private void TryRefillFireplace(Player player, Fireplace fireplace, Container[] containers)
        {
            LogDebug($"TryRefillFireplace START: '{fireplace.gameObject.name}'");

            if (!ShouldRefillFireplace(fireplace))
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - type/config filter returned false."
                );
                return;
            }

            if (_onlyRefillOwnPieces.Value && !IsOwnPiece(player, fireplace))
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - not owned by local player."
                );
                return;
            }

            ZNetView nview = fireplace.GetComponent<ZNetView>();

            if (nview == null)
                nview = fireplace.GetComponentInParent<ZNetView>();

            if (nview == null)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - no ZNetView found."
                );
                return;
            }

            if (!nview.IsValid())
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - ZNetView is not valid."
                );
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - ZDO is null."
                );
                return;
            }

            if (fireplace.m_fuelItem == null)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - m_fuelItem is null."
                );
                return;
            }

            float currentFuel = zdo.GetFloat(ZDOVars.s_fuel, 0f);
            float maxFuel = fireplace.m_maxFuel;

            if (maxFuel <= 0f)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - maxFuel <= 0 ({maxFuel})."
                );
                return;
            }

            float fuelPercent = currentFuel / maxFuel;

            LogDebug(
                $"Fuel state for '{fireplace.gameObject.name}': " +
                $"currentFuel={currentFuel:0.0}, maxFuel={maxFuel:0.0}, " +
                $"fuelPercent={fuelPercent:0.00}, threshold={_refillBelowPercent.Value:0.00}"
            );

            if (fuelPercent >= _refillBelowPercent.Value)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - fuelPercent >= threshold."
                );
                return;
            }

            string fuelName =
                fireplace.m_fuelItem.m_itemData.m_shared.m_name;

            int missingFuel =
                Mathf.CeilToInt(maxFuel - currentFuel);

            if (missingFuel <= 0)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - missingFuel <= 0 ({missingFuel})."
                );
                return;
            }

            int requestedFuel;

            if (_refillToMax.Value)
            {
                requestedFuel = missingFuel;
            }
            else
            {
                requestedFuel = Mathf.Min(
                    Mathf.Max(_refillAmount.Value, 0),
                    missingFuel
                );
            }

            LogDebug(
                $"Refill request for '{fireplace.gameObject.name}': " +
                $"fuel='{fuelName}', missingFuel={missingFuel}, requestedFuel={requestedFuel}, " +
                $"refillMode={(_refillToMax.Value ? "max" : "fixed")}"
            );

            if (requestedFuel <= 0)
            {
                LogDebug(
                    $"Skipping '{fireplace.gameObject.name}' - requestedFuel <= 0."
                );
                return;
            }

            int fuelAdded = 0;

            int fuelConsumed = ConsumeFuel(
                player,
                fireplace.transform,
                fuelName,
                requestedFuel,
                containers
            );

            if (fuelConsumed <= 0)
            {
                LogDebug(
                    $"No usable fuel found for '{fireplace.gameObject.name}'."
                );

                return;
            }

            for (int i = 0; i < fuelConsumed; i++)
            {
                nview.InvokeRPC(
                    "RPC_AddFuel",
                    new object[0]
                );
            }

            LogInfo(
                $"Refilled {fireplace.name}: " +
                $"{currentFuel:0.0}/{maxFuel:0.0}, " +
                $"fuel={fuelName}, " +
                $"added={fuelConsumed}, " +
                $"mode={(_refillToMax.Value ? "max" : "fixed")}"
            );

            if (fuelAdded > 0)
            {
                LogInfo(
                    $"Refilled {fireplace.name}: " +
                    $"{currentFuel:0.0}/{maxFuel:0.0}, " +
                    $"fuel={fuelName}, " +
                    $"added={fuelAdded}, " +
                    $"mode={(_refillToMax.Value ? "max" : "fixed")}"
                );
            }
        }

        private int ConsumeFuelFromPlayer(Player player, string fuelName, int requestedAmount)
        {
            if (requestedAmount <= 0)
                return 0;

            Inventory inventory = player.GetInventory();

            if (inventory == null)
            {
                LogDebug("Player inventory is null.");
                return 0;
            }

            int availableFuel =
                inventory.CountItems(fuelName);

            int usableFuel =
                Mathf.Max(
                    availableFuel - _keepFuelReserve.Value,
                    0
                );

            int amountToTake =
                Mathf.Min(
                    requestedAmount,
                    usableFuel
                );

            if (amountToTake <= 0)
            {
                LogDebug(
                    $"Player inventory: fuel='{fuelName}', " +
                    $"available={availableFuel}, " +
                    $"reserve={_keepFuelReserve.Value}, " +
                    $"usable=0"
                );

                return 0;
            }

            inventory.RemoveItem(
                fuelName,
                amountToTake
            );

            LogDebug(
                $"Player inventory: consumed={amountToTake}x {fuelName}, " +
                $"before={availableFuel}, " +
                $"remaining={availableFuel - amountToTake}"
            );

            return amountToTake;
        }

        private int ConsumeFuel(
            Player player,
            Transform target,
            string fuelName,
            int requestedAmount,
            Container[] containers)
        {
            if (requestedAmount <= 0)
                return 0;

            LogDebug(
                $"Fuel request: " +
                $"fuel='{fuelName}', " +
                $"requested={requestedAmount}, " +
                $"priority={_fuelSourcePriority.Value}, " +
                $"containers={_useNearbyContainers.Value}, " +
                $"reserve={_keepFuelReserve.Value}"
            );

            int consumed = 0;

            if (_fuelSourcePriority.Value == FuelSourcePriority.ContainersFirst)
            {
                if (_useNearbyContainers.Value && containers != null)
                {
                    consumed += ConsumeFuelFromNearbyContainers(
                        target,
                        fuelName,
                        requestedAmount - consumed,
                        containers
                    );
                }

                if (consumed < requestedAmount)
                {
                    consumed += ConsumeFuelFromPlayer(
                        player,
                        fuelName,
                        requestedAmount - consumed
                    );
                }
            }
            else
            {
                consumed += ConsumeFuelFromPlayer(
                    player,
                    fuelName,
                    requestedAmount
                );

                if (
                    _useNearbyContainers.Value &&
                    containers != null &&
                    consumed < requestedAmount
                )
                {
                    consumed += ConsumeFuelFromNearbyContainers(
                        target,
                        fuelName,
                        requestedAmount - consumed,
                        containers
                    );
                }
            }

            LogDebug(
                $"Fuel request result: " +
                $"fuel='{fuelName}', " +
                $"requested={requestedAmount}, " +
                $"consumed={consumed}"
            );

            return consumed;
        }

        private int ConsumeFuelFromNearbyContainers(
            Transform target,
            string fuelName,
            int requestedAmount,
            Container[] containers)
        {
            if (requestedAmount <= 0)
                return 0;

            if (containers == null || containers.Length == 0)
                return 0;

            List<ContainerCandidate> candidates =
                new List<ContainerCandidate>();

            foreach (Container container in containers)
            {
                if (container == null)
                    continue;

                float distance = Vector3.Distance(
                    target.position,
                    container.transform.position
                );

                if (distance > _containerRadius.Value)
                    continue;

                Inventory inventory =
                    container.GetInventory();

                if (inventory == null)
                    continue;

                int availableFuel =
                    inventory.CountItems(fuelName);

                int usableFuel =
                    Mathf.Max(
                        availableFuel - _keepFuelReserve.Value,
                        0
                    );

                if (usableFuel <= 0)
                    continue;

                candidates.Add(
                    new ContainerCandidate
                    {
                        Container = container,
                        Distance = distance
                    }
                );
            }

            candidates.Sort(
                (a, b) => a.Distance.CompareTo(b.Distance)
            );

            int consumed = 0;

            foreach (ContainerCandidate candidate in candidates)
            {
                if (consumed >= requestedAmount)
                    break;

                Inventory inventory =
                    candidate.Container.GetInventory();

                if (inventory == null)
                    continue;

                int availableFuel =
                    inventory.CountItems(fuelName);

                int usableFuel =
                    Mathf.Max(
                        availableFuel - _keepFuelReserve.Value,
                        0
                    );

                if (usableFuel <= 0)
                    continue;

                int amountToTake =
                    Mathf.Min(
                        requestedAmount - consumed,
                        usableFuel
                    );

                inventory.RemoveItem(
                    fuelName,
                    amountToTake
                );

                consumed += amountToTake;

                LogDebug(
                    $"Container '{candidate.Container.name}' " +
                    $"({candidate.Distance:0.0}m): " +
                    $"consumed={amountToTake}x {fuelName}"
                );
            }

            return consumed;
        }

        private void LogDebug(string message)
        {
            if (_logLevel.Value >= ModLogLevel.Debug)
                Logger.LogInfo($"[DEBUG] {message}");
        }

        private void LogInfo(string message)
        {
            if (_logLevel.Value >= ModLogLevel.Info)
                Logger.LogInfo(message);
        }

        private void LogWarning(string message)
        {
            if (_logLevel.Value >= ModLogLevel.Warning)
                Logger.LogWarning(message);
        }

        private void LogError(string message)
        {
            if (_logLevel.Value >= ModLogLevel.Error)
                Logger.LogError(message);
        }
    }
}