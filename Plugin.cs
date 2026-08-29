using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace AutoRefillFires
{
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
        private ConfigEntry<bool> _fillOtherFireplaces;
        private ConfigEntry<int> _keepFuelReserve;
        private ConfigEntry<FuelSourcePriority> _fuelSourcePriority;
        private ConfigEntry<bool> _onlyRefillOwnPieces;
        private ConfigEntry<KeyboardShortcut> _toggleHotkey;
        private bool _modEnabled = true;

        private float _nextCheckTime;

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

            Logger.LogInfo("Auto Refill Fires loaded!");
        }

        private void Update()
        {
            if (_toggleHotkey.Value.IsDown())
            {
                _modEnabled = !_modEnabled;

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

                Logger.LogInfo(status);
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

            // Campfire
            if (objectName.Contains("firepit"))
                return _fillCampfires.Value;

            // Hearth
            if (objectName.Contains("hearth"))
                return _fillHearths.Value;

            // All standing / ground torches, including colored variants
            if (objectName.Contains("groundtorch"))
                return _fillStandingTorches.Value;

            // All wall torches, including colored variants
            if (objectName.Contains("walltorch"))
                return _fillWallTorches.Value;

            // Braziers
            if (objectName.Contains("brazier"))
                return _fillBraziers.Value;

            // Unknown / modded Fireplace-based objects
            return _fillOtherFireplaces.Value;
        }

        private void RefillNearbyFireplaces(Player player)
        {
            Fireplace[] fireplaces =
                Object.FindObjectsByType<Fireplace>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

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

                TryRefillFireplace(player, fireplace);
            }
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

        private void TryRefillFireplace(Player player, Fireplace fireplace)
        {
            if (!ShouldRefillFireplace(fireplace))
                return;

            if (_onlyRefillOwnPieces.Value && !IsOwnPiece(player, fireplace))
                return;

            ZNetView nview = fireplace.GetComponent<ZNetView>();

            if (nview == null)
                nview = fireplace.GetComponentInParent<ZNetView>();

            if (nview == null)
                return;

            if (!nview.IsValid())
                return;

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
                return;

            if (fireplace.m_fuelItem == null)
                return;

            float currentFuel = zdo.GetFloat(ZDOVars.s_fuel, 0f);
            float maxFuel = fireplace.m_maxFuel;

            if (maxFuel <= 0f)
                return;

            float fuelPercent = currentFuel / maxFuel;

            if (fuelPercent >= _refillBelowPercent.Value)
                return;

            string fuelName =
                fireplace.m_fuelItem.m_itemData.m_shared.m_name;

            int missingFuel =
                Mathf.CeilToInt(maxFuel - currentFuel);

            if (missingFuel <= 0)
                return;

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

            if (requestedFuel <= 0)
                return;

            int fuelAdded = 0;

            for (int i = 0; i < requestedFuel; i++)
            {
                if (!TryConsumeFuel(
                    player,
                    fireplace,
                    fuelName
                ))
                {
                    break;
                }

                nview.InvokeRPC(
                    "RPC_AddFuel",
                    new object[0]
                );

                fuelAdded++;
            }

            if (fuelAdded > 0)
            {
                Logger.LogInfo(
                    $"Refilled {fireplace.name}: " +
                    $"{currentFuel:0.0}/{maxFuel:0.0}, " +
                    $"fuel={fuelName}, " +
                    $"added={fuelAdded}, " +
                    $"mode={(_refillToMax.Value ? "max" : "fixed")}"
                );
            }
        }

        private bool TryConsumeFuelFromPlayer(Player player, string fuelName)
        {
            Inventory inventory = player.GetInventory();

            if (inventory == null)
                return false;

            int availableFuel =
                inventory.CountItems(fuelName);

            if (availableFuel <= _keepFuelReserve.Value)
                return false;

            inventory.RemoveItem(
                fuelName,
                1
            );

            Logger.LogDebug(
                $"Fuel {fuelName} taken from player inventory. " +
                $"Remaining: {availableFuel - 1}"
            );

            return true;
        }

        private bool TryConsumeFuel(Player player, Fireplace fireplace, string fuelName)
        {
            if (_fuelSourcePriority.Value == FuelSourcePriority.ContainersFirst)
            {
                if (_useNearbyContainers.Value &&
                    TryConsumeFuelFromNearbyContainer(
                        fireplace,
                        fuelName))
                {
                    return true;
                }

                return TryConsumeFuelFromPlayer(
                    player,
                    fuelName
                );
            }

            // PlayerFirst
            if (TryConsumeFuelFromPlayer(
                player,
                fuelName))
            {
                return true;
            }

            if (!_useNearbyContainers.Value)
                return false;

            return TryConsumeFuelFromNearbyContainer(
                fireplace,
                fuelName
            );
        }

        private bool TryConsumeFuelFromNearbyContainer(Fireplace fireplace, string fuelName)
        {
            Container[] containers =
                Object.FindObjectsByType<Container>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            Container closestContainer = null;
            float closestDistance = float.MaxValue;

            foreach (Container container in containers)
            {
                if (container == null)
                    continue;

                float distance = Vector3.Distance(
                    fireplace.transform.position,
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

                /*
                 * Never use the configured reserve.
                 *
                 * Example:
                 * reserve = 10
                 * chest contains 10 -> skip
                 * chest contains 11 -> usable
                 */
                if (availableFuel <= _keepFuelReserve.Value)
                    continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestContainer = container;
                }
            }

            if (closestContainer == null)
                return false;

            Inventory chestInventory =
                closestContainer.GetInventory();

            if (chestInventory == null)
                return false;

            int chestFuel =
                chestInventory.CountItems(fuelName);

            if (chestFuel <= _keepFuelReserve.Value)
                return false;

            chestInventory.RemoveItem(
                fuelName,
                1
            );

            Logger.LogDebug(
                $"Fuel {fuelName} taken from container " +
                $"{closestContainer.name} " +
                $"({closestDistance:0.0}m). " +
                $"Remaining: {chestFuel - 1}"
            );

            return true;
        }
    }
}