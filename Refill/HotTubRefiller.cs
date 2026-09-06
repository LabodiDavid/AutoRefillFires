using UnityEngine;

namespace AutoRefillFires
{
    internal class HotTubRefiller
    {
        private readonly ModConfig _config;
        private readonly FuelManager _fuelManager;
        private readonly ModLogger _log;

        public HotTubRefiller(ModConfig config, FuelManager fuelManager, ModLogger log)
        {
            _config = config;
            _fuelManager = fuelManager;
            _log = log;
        }

        public void RefillNearby(Player player, Smelter[] smelters, Container[] containers)
        {
            int hotTubsInRange = 0;

            foreach (Smelter smelter in smelters)
            {
                if (smelter == null)
                    continue;

                string objectName = smelter.gameObject.name.Replace("(Clone)", "").ToLowerInvariant();

                if (objectName != "piece_bathtub")
                    continue;

                float distance = Vector3.Distance(player.transform.position, smelter.transform.position);

                if (distance > _config.Radius.Value)
                    continue;

                hotTubsInRange++;

                _log.Debug(
                    $"Hot tub in range: name='{smelter.gameObject.name}', " +
                    $"distance={distance:0.0}m"
                );

                TryRefillHotTub(player, smelter, containers);
            }

            _log.Debug($"Hot tub scan end: inRange={hotTubsInRange}");
        }

        private void TryRefillHotTub(Player player, Smelter smelter, Container[] containers)
        {
            if (smelter == null)
                return;

            ZNetView nview = smelter.GetComponent<ZNetView>();

            if (nview == null)
                nview = smelter.GetComponentInParent<ZNetView>();

            if (nview == null || !nview.IsValid())
            {
                _log.Debug("Hot tub: invalid ZNetView.");
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                _log.Debug("Hot tub: ZDO is null.");
                return;
            }

            if (smelter.m_fuelItem == null || smelter.m_fuelItem.m_itemData == null || smelter.m_fuelItem.m_itemData.m_shared == null)
            {
                _log.Debug("Hot tub: fuel item is null.");
                return;
            }

            string fuelName = smelter.m_fuelItem.m_itemData.m_shared.m_name;
            float currentFuel = zdo.GetFloat(ZDOVars.s_fuel, 0f);
            float maxFuel = smelter.m_maxFuel;

            if (maxFuel <= 0f)
                return;

            float fuelPercent = currentFuel / maxFuel;

            _log.Debug(
                $"Hot tub fuel state: fuel='{fuelName}', " +
                $"current={currentFuel:0.00}, max={maxFuel:0.00}, " +
                $"percent={fuelPercent * 100f:0.0}%, threshold={_config.RefillBelowPercent.Value * 100f:0.0}%"
            );

            if (fuelPercent >= _config.RefillBelowPercent.Value)
            {
                _log.Debug("Skipping hot tub - fuelPercent >= threshold.");
                return;
            }

            int missingFuel = Mathf.CeilToInt(maxFuel - currentFuel);
            int requestedFuel = _config.RefillToMax.Value ? missingFuel : Mathf.Min(_config.RefillAmount.Value, missingFuel);

            if (requestedFuel <= 0)
                return;

            _log.Debug($"Hot tub refill requested: missing={missingFuel}, requested={requestedFuel}");

            int fuelConsumed = _fuelManager.ConsumeFuel(player, smelter.transform, fuelName, requestedFuel, containers);

            if (fuelConsumed <= 0)
            {
                _log.Debug("Hot tub refill stopped: no usable fuel.");
                return;
            }

            for (int i = 0; i < fuelConsumed; i++)
                nview.InvokeRPC("RPC_AddFuel", new object[0]);

            _log.Info(
                $"Refilled hot tub: {currentFuel:0.0}/{maxFuel:0.0}, " +
                $"fuel={fuelName}, added={fuelConsumed}, " +
                $"mode={(_config.RefillToMax.Value ? "max" : "fixed")}"
            );
        }
    }
}
