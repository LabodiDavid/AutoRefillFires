using UnityEngine;

namespace AutoRefillFires
{
    internal class FireplaceRefiller
    {
        private readonly ModConfig _config;
        private readonly FuelManager _fuelManager;
        private readonly ModLogger _log;

        public FireplaceRefiller(ModConfig config, FuelManager fuelManager, ModLogger log)
        {
            _config = config;
            _fuelManager = fuelManager;
            _log = log;
        }

        public void TryRefill(Player player, Fireplace fireplace, Container[] containers)
        {
            _log.Debug($"TryRefillFireplace START: '{fireplace.gameObject.name}'");

            if (!ShouldRefillFireplace(fireplace))
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - type/config filter returned false.");
                return;
            }

            if (_config.OnlyRefillOwnPieces.Value && !IsOwnPiece(player, fireplace))
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - not owned by local player.");
                return;
            }

            ZNetView nview = fireplace.GetComponent<ZNetView>();

            if (nview == null)
                nview = fireplace.GetComponentInParent<ZNetView>();

            if (nview == null)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - no ZNetView found.");
                return;
            }

            if (!nview.IsValid())
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - ZNetView is not valid.");
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - ZDO is null.");
                return;
            }

            if (fireplace.m_fuelItem == null || fireplace.m_fuelItem.m_itemData == null || fireplace.m_fuelItem.m_itemData.m_shared == null)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - fuel item is null.");
                return;
            }

            float currentFuel = zdo.GetFloat(ZDOVars.s_fuel, 0f);
            float maxFuel = fireplace.m_maxFuel;

            if (maxFuel <= 0f)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - maxFuel <= 0 ({maxFuel}).");
                return;
            }

            float fuelPercent = currentFuel / maxFuel;

            _log.Debug(
                $"Fuel state for '{fireplace.gameObject.name}': " +
                $"currentFuel={currentFuel:0.0}, maxFuel={maxFuel:0.0}, " +
                $"fuelPercent={fuelPercent:0.00}, threshold={_config.RefillBelowPercent.Value:0.00}"
            );

            if (fuelPercent >= _config.RefillBelowPercent.Value)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - fuelPercent >= threshold.");
                return;
            }

            string fuelName = fireplace.m_fuelItem.m_itemData.m_shared.m_name;
            int missingFuel = Mathf.CeilToInt(maxFuel - currentFuel);

            if (missingFuel <= 0)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - missingFuel <= 0 ({missingFuel}).");
                return;
            }

            int requestedFuel = _config.RefillToMax.Value
                ? missingFuel
                : Mathf.Min(Mathf.Max(_config.RefillAmount.Value, 0), missingFuel);

            _log.Debug(
                $"Refill request for '{fireplace.gameObject.name}': " +
                $"fuel='{fuelName}', missingFuel={missingFuel}, requestedFuel={requestedFuel}, " +
                $"refillMode={(_config.RefillToMax.Value ? "max" : "fixed")}"
            );

            if (requestedFuel <= 0)
            {
                _log.Debug($"Skipping '{fireplace.gameObject.name}' - requestedFuel <= 0.");
                return;
            }

            int fuelConsumed = _fuelManager.ConsumeFuel(player, fireplace.transform, fuelName, requestedFuel, containers);

            if (fuelConsumed <= 0)
            {
                _log.Debug($"No usable fuel found for '{fireplace.gameObject.name}'.");
                return;
            }

            for (int i = 0; i < fuelConsumed; i++)
                nview.InvokeRPC("RPC_AddFuel", new object[0]);

            _log.Info(
                $"Refilled {fireplace.name}: {currentFuel:0.0}/{maxFuel:0.0}, " +
                $"fuel={fuelName}, added={fuelConsumed}, " +
                $"mode={(_config.RefillToMax.Value ? "max" : "fixed")}"
            );
        }

        private bool ShouldRefillFireplace(Fireplace fireplace)
        {
            string objectName = fireplace.gameObject.name.Replace("(Clone)", "").ToLowerInvariant();

            bool result;
            string matchedRule;

            if (objectName.Contains("candle"))
            {
                result = false;
                matchedRule = "IgnoredCandle";
            }
            if (objectName.Contains("fire_pit") || objectName.Contains("firepit"))
            {
                result = _config.FillCampfires.Value;
                matchedRule = "FillCampfires";
            }
            else if (objectName.Contains("hearth"))
            {
                result = _config.FillHearths.Value;
                matchedRule = "FillHearths";
            }
            else if (objectName.Contains("groundtorch"))
            {
                result = _config.FillStandingTorches.Value;
                matchedRule = "FillStandingTorches";
            }
            else if (objectName.Contains("walltorch"))
            {
                result = _config.FillWallTorches.Value;
                matchedRule = "FillWallTorches";
            }
            else if (objectName.Contains("brazier"))
            {
                result = _config.FillBraziers.Value;
                matchedRule = "FillBraziers";
            }
            else if (objectName.Contains("bonfire"))
            {
                result = _config.FillBonfires.Value;
                matchedRule = "FillBonfires";
            }
            else
            {
                result = _config.FillOtherFireplaces.Value;
                matchedRule = "FillOtherFireplaces";
            }

            _log.Debug(
                $"ShouldRefillFireplace: name='{fireplace.gameObject.name}', " +
                $"normalized='{objectName}', matchedRule={matchedRule}, result={result}"
            );

            return result;
        }

        private bool IsOwnPiece(Player player, Fireplace fireplace)
        {
            Piece piece = fireplace.GetComponent<Piece>();

            if (piece == null)
                piece = fireplace.GetComponentInParent<Piece>();

            if (piece == null)
                return true;

            return piece.GetCreator() == player.GetPlayerID();
        }
    }
}
