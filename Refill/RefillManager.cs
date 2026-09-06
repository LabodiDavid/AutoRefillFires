using System.Collections.Generic;
using UnityEngine;

namespace AutoRefillFires
{
    internal class RefillManager
    {
        private readonly ModConfig _config;
        private readonly FireplaceRefiller _fireplaceRefiller;
        private readonly HotTubRefiller _hotTubRefiller;
        private readonly ModLogger _log;

        public RefillManager(ModConfig config, ModLogger log)
        {
            _config = config;
            _log = log;

            FuelManager fuelManager = new FuelManager(_config, _log);
            _fireplaceRefiller = new FireplaceRefiller(_config, fuelManager, _log);
            _hotTubRefiller = new HotTubRefiller(_config, fuelManager, _log);
        }

        public void Run(Player player)
        {
            Fireplace[] fireplaces = Object.FindObjectsByType<Fireplace>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Smelter[] smelters = null;

            if (_config.FillHotTubs.Value)
                smelters = Object.FindObjectsByType<Smelter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            Container[] containers = null;

            if (_config.UseNearbyContainers.Value)
                containers = Object.FindObjectsByType<Container>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            List<FireplaceCandidate> candidates = new List<FireplaceCandidate>();

            foreach (Fireplace fireplace in fireplaces)
            {
                if (fireplace == null)
                    continue;

                float distance = Vector3.Distance(player.transform.position, fireplace.transform.position);

                if (distance > _config.Radius.Value)
                    continue;

                ZNetView nview = fireplace.GetComponent<ZNetView>();

                if (nview == null)
                    nview = fireplace.GetComponentInParent<ZNetView>();

                if (nview == null || !nview.IsValid())
                    continue;

                ZDO zdo = nview.GetZDO();

                if (zdo == null)
                    continue;

                float maxFuel = fireplace.m_maxFuel;

                if (maxFuel <= 0f)
                    continue;

                float currentFuel = zdo.GetFloat(ZDOVars.s_fuel, 0f);
                float fuelPercent = currentFuel / maxFuel;

                candidates.Add(new FireplaceCandidate
                {
                    Fireplace = fireplace,
                    Distance = distance,
                    FuelPercent = fuelPercent
                });
            }

            // Priority: lowest fuel percentage first, then nearest fireplace.
            candidates.Sort((a, b) =>
            {
                int fuelCompare = a.FuelPercent.CompareTo(b.FuelPercent);

                if (fuelCompare != 0)
                    return fuelCompare;

                return a.Distance.CompareTo(b.Distance);
            });

            _log.Debug(
                $"Scan start: loadedFireplaces={fireplaces.Length}, " +
                $"inRange={candidates.Count}, " +
                $"loadedContainers={(containers != null ? containers.Length : 0)}, " +
                $"radius={_config.Radius.Value:0.0}m"
            );

            foreach (FireplaceCandidate candidate in candidates)
            {
                _log.Debug(
                    $"Priority: name='{candidate.Fireplace.gameObject.name}', " +
                    $"fuel={candidate.FuelPercent * 100f:0.0}%, " +
                    $"distance={candidate.Distance:0.0}m"
                );

                _fireplaceRefiller.TryRefill(player, candidate.Fireplace, containers);
            }

            if (_config.FillHotTubs.Value && smelters != null)
                _hotTubRefiller.RefillNearby(player, smelters, containers);

            _log.Debug("Scan end.");
        }
    }
}
