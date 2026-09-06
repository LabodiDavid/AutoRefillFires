using BepInEx;
using UnityEngine;

namespace AutoRefillFires
{
    [BepInPlugin(Plugin.PluginGuid, Plugin.PluginName, Plugin.PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "simplifydave.autorefillfires";
        public const string PluginName = "Auto Refill Fires";
        public const string PluginVersion = VersionInfo.Version;

        private ModConfig _config;
        private ModLogger _log;
        private RefillManager _refillManager;

        private bool _modEnabled = true;
        private float _nextCheckTime;

        private void Awake()
        {
            _config = new ModConfig(Config);
            _log = new ModLogger(_config, Logger);
            _refillManager = new RefillManager(_config, _log);

            Logger.LogInfo("Auto Refill Fires loaded!");
        }

        private void Update()
        {
            if (_config.ToggleHotkey.Value.IsDown())
            {
                _modEnabled = !_modEnabled;

                // Reload config when turning the mod OFF
                if (!_modEnabled)
                {
                    try
                    {
                        Config.Reload();
                        _log.Info("Configuration reloaded.");
                    }
                    catch (System.Exception ex)
                    {
                        _log.Error($"Failed to reload configuration: {ex}");
                    }
                }

                string status = _modEnabled
                    ? "Auto Refill Fires: ENABLED"
                    : "Auto Refill Fires: DISABLED";

                Player player = Player.m_localPlayer;

                if (player != null)
                    player.Message(MessageHud.MessageType.Center, status);

                _log.Info(status);
            }

            if (!_modEnabled)
                return;

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + _config.CheckInterval.Value;

            Player localPlayer = Player.m_localPlayer;

            if (localPlayer == null)
                return;

            _refillManager.Run(localPlayer);
        }
    }
}
