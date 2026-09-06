using BepInEx.Logging;

namespace AutoRefillFires
{
    internal class ModLogger
    {
        private readonly ModConfig _config;
        private readonly ManualLogSource _logger;

        public ModLogger(ModConfig config, ManualLogSource logger)
        {
            _config = config;
            _logger = logger;
        }

        public void Debug(string message)
        {
            if (_config.LogLevel.Value >= ModLogLevel.Debug)
                _logger.LogInfo($"[DEBUG] {message}");
        }

        public void Info(string message)
        {
            if (_config.LogLevel.Value >= ModLogLevel.Info)
                _logger.LogInfo(message);
        }

        public void Warning(string message)
        {
            if (_config.LogLevel.Value >= ModLogLevel.Warning)
                _logger.LogWarning(message);
        }

        public void Error(string message)
        {
            if (_config.LogLevel.Value >= ModLogLevel.Error)
                _logger.LogError(message);
        }
    }
}
