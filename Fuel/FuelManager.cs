using System.Collections.Generic;
using UnityEngine;

namespace AutoRefillFires
{
    internal class FuelManager
    {
        private readonly ModConfig _config;
        private readonly ModLogger _log;

        public FuelManager(ModConfig config, ModLogger log)
        {
            _config = config;
            _log = log;
        }

        public int ConsumeFuel(Player player, Transform target, string fuelName, int requestedAmount, Container[] containers)
        {
            if (requestedAmount <= 0)
                return 0;

            _log.Debug(
                $"Fuel request: fuel='{fuelName}', " +
                $"requested={requestedAmount}, " +
                $"priority={_config.FuelSourcePriority.Value}, " +
                $"containers={_config.UseNearbyContainers.Value}, " +
                $"reserve={_config.KeepFuelReserve.Value}"
            );

            int consumed = 0;

            if (_config.FuelSourcePriority.Value == FuelSourcePriority.ContainersFirst)
            {
                if (_config.UseNearbyContainers.Value && containers != null)
                    consumed += ConsumeFuelFromNearbyContainers(target, fuelName, requestedAmount - consumed, containers);

                if (consumed < requestedAmount)
                    consumed += ConsumeFuelFromPlayer(player, fuelName, requestedAmount - consumed);
            }
            else
            {
                consumed += ConsumeFuelFromPlayer(player, fuelName, requestedAmount);

                if (_config.UseNearbyContainers.Value && containers != null && consumed < requestedAmount)
                    consumed += ConsumeFuelFromNearbyContainers(target, fuelName, requestedAmount - consumed, containers);
            }

            _log.Debug(
                $"Fuel request result: fuel='{fuelName}', " +
                $"requested={requestedAmount}, " +
                $"consumed={consumed}"
            );

            return consumed;
        }

        private int ConsumeFuelFromPlayer(Player player, string fuelName, int requestedAmount)
        {
            if (requestedAmount <= 0)
                return 0;

            Inventory inventory = player.GetInventory();

            if (inventory == null)
            {
                _log.Debug("Player inventory is null.");
                return 0;
            }

            int availableFuel = inventory.CountItems(fuelName);
            int usableFuel = Mathf.Max(availableFuel - _config.KeepFuelReserve.Value, 0);
            int amountToTake = Mathf.Min(requestedAmount, usableFuel);

            if (amountToTake <= 0)
            {
                _log.Debug(
                    $"Player inventory: fuel='{fuelName}', " +
                    $"available={availableFuel}, " +
                    $"reserve={_config.KeepFuelReserve.Value}, " +
                    $"usable=0"
                );

                return 0;
            }

            inventory.RemoveItem(fuelName, amountToTake);

            _log.Debug(
                $"Player inventory: consumed={amountToTake}x {fuelName}, " +
                $"before={availableFuel}, " +
                $"remaining={availableFuel - amountToTake}"
            );

            return amountToTake;
        }

        private int ConsumeFuelFromNearbyContainers(Transform target, string fuelName, int requestedAmount, Container[] containers)
        {
            if (requestedAmount <= 0)
                return 0;

            if (containers == null || containers.Length == 0)
                return 0;

            List<ContainerCandidate> candidates = new List<ContainerCandidate>();

            foreach (Container container in containers)
            {
                if (container == null)
                    continue;

                float distance = Vector3.Distance(target.position, container.transform.position);

                if (distance > _config.ContainerRadius.Value)
                    continue;

                Inventory inventory = container.GetInventory();

                if (inventory == null)
                    continue;

                int availableFuel = inventory.CountItems(fuelName);
                int usableFuel = Mathf.Max(availableFuel - _config.KeepFuelReserve.Value, 0);

                if (usableFuel <= 0)
                    continue;

                candidates.Add(new ContainerCandidate
                {
                    Container = container,
                    Distance = distance
                });
            }

            candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            int consumed = 0;

            foreach (ContainerCandidate candidate in candidates)
            {
                if (consumed >= requestedAmount)
                    break;

                Inventory inventory = candidate.Container.GetInventory();

                if (inventory == null)
                    continue;

                int availableFuel = inventory.CountItems(fuelName);
                int usableFuel = Mathf.Max(availableFuel - _config.KeepFuelReserve.Value, 0);

                if (usableFuel <= 0)
                    continue;

                int amountToTake = Mathf.Min(requestedAmount - consumed, usableFuel);

                inventory.RemoveItem(fuelName, amountToTake);
                consumed += amountToTake;

                _log.Debug(
                    $"Container '{candidate.Container.name}' ({candidate.Distance:0.0}m): " +
                    $"consumed={amountToTake}x {fuelName}"
                );
            }

            return consumed;
        }
    }
}
