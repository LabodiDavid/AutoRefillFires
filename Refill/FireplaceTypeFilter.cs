namespace AutoRefillFires
{
    internal static class FireplaceTypeFilter
    {
        public static bool IsEnabled(ModConfig config, Fireplace fireplace, out string matchedRule)
        {
            string objectName = fireplace.gameObject.name.Replace("(Clone)", "").ToLowerInvariant();

            if (objectName.Contains("candle"))
            {
                matchedRule = "IgnoredCandle";
                return false;
            }

            if (objectName.Contains("fire_pit") || objectName.Contains("firepit"))
            {
                matchedRule = "FillCampfires";
                return config.FillCampfires.Value;
            }

            if (objectName.Contains("hearth"))
            {
                matchedRule = "FillHearths";
                return config.FillHearths.Value;
            }

            if (objectName.Contains("groundtorch"))
            {
                matchedRule = "FillStandingTorches";
                return config.FillStandingTorches.Value;
            }

            if (objectName.Contains("walltorch"))
            {
                matchedRule = "FillWallTorches";
                return config.FillWallTorches.Value;
            }

            if (objectName.Contains("brazier"))
            {
                matchedRule = "FillBraziers";
                return config.FillBraziers.Value;
            }

            if (objectName.Contains("bonfire"))
            {
                matchedRule = "FillBonfires";
                return config.FillBonfires.Value;
            }

            matchedRule = "FillOtherFireplaces";
            return config.FillOtherFireplaces.Value;
        }
    }
}
