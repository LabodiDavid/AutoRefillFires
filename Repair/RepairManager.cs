using UnityEngine;

namespace AutoRefillFires
{
    internal class RepairManager
    {
        private readonly ModConfig _config;
        private readonly ModLogger _log;

        public RepairManager(ModConfig config, ModLogger log)
        {
            _config = config;
            _log = log;
        }

        public void TryRepair(Player player, Component target, string label)
        {
            if (!_config.AutoRepair.Value || target == null)
                return;

            if (_config.OnlyRepairOwnPieces.Value && !IsOwnPiece(player, target))
            {
                _log.Debug($"Skipping repair for '{target.gameObject.name}' - not owned by local player.");
                return;
            }

            WearNTear wearNTear = target.GetComponent<WearNTear>();

            if (wearNTear == null)
                wearNTear = target.GetComponentInParent<WearNTear>();

            if (wearNTear == null)
            {
                _log.Debug($"Skipping repair for '{target.gameObject.name}' - no WearNTear found.");
                return;
            }

            ZNetView nview = wearNTear.GetComponent<ZNetView>();

            if (nview == null)
                nview = wearNTear.GetComponentInParent<ZNetView>();

            if (nview == null || !nview.IsValid())
            {
                _log.Debug($"Skipping repair for '{target.gameObject.name}' - invalid ZNetView.");
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo == null)
            {
                _log.Debug($"Skipping repair for '{target.gameObject.name}' - ZDO is null.");
                return;
            }

            float maxHealth = wearNTear.m_health;

            if (maxHealth <= 0f)
            {
                _log.Debug($"Skipping repair for '{target.gameObject.name}' - max health <= 0 ({maxHealth}).");
                return;
            }

            float currentHealth = zdo.GetFloat(ZDOVars.s_health, maxHealth);
            float healthPercent = currentHealth / maxHealth;
            float threshold = Mathf.Clamp01(_config.RepairBelowPercent.Value);

            _log.Debug(
                $"Repair state: type={label}, name='{target.gameObject.name}', " +
                $"health={currentHealth:0.0}/{maxHealth:0.0} ({healthPercent * 100f:0.0}%), " +
                $"threshold={threshold * 100f:0.0}%"
            );

            if (healthPercent >= threshold)
                return;

            if (wearNTear.Repair())
            {
                _log.Info(
                    $"Repaired {label}: {target.gameObject.name}, " +
                    $"{currentHealth:0.0}/{maxHealth:0.0} ({healthPercent * 100f:0.0}% -> 100%)"
                );
            }
            else
            {
                _log.Debug($"Repair request for '{target.gameObject.name}' returned false.");
            }
        }

        private static bool IsOwnPiece(Player player, Component target)
        {
            Piece piece = target.GetComponent<Piece>();

            if (piece == null)
                piece = target.GetComponentInParent<Piece>();

            if (piece == null)
                return true;

            return piece.GetCreator() == player.GetPlayerID();
        }
    }
}
