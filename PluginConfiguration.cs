using System.ComponentModel.DataAnnotations;

namespace SRTPluginUIDR1DirectXOverlay
{
    public class PluginConfiguration
    {
        public bool Debug { get; set; }
        public float ScalingFactor { get; set; }
        
        public bool DockRight { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float ElementWidth { get; set; }
        public string StringFontName { get; set; }
        public float BlockOffset { get; set; }

        public bool ShowCampainInfo { get; set; }
        public bool ShowCoordinatesInfo { get; set; }
        public bool ShowStatusesInfo { get; set; }
        public bool ShowWeaponInfo { get; set; }
        public bool ShowBossInfo { get; set; }
        public bool ShowVelocityInfo { get; set; }

        [Range(1,50)]
        public int SpeedAverageFactor { get; set; }

        public PluginConfiguration()
        {
            Debug = false;
            ScalingFactor = 1f;

            DockRight = true;
            PositionX = 150f;
            PositionY = 320f;
            ElementWidth = 320f;
            StringFontName = "Impact";
            BlockOffset = 25f;

            ShowCampainInfo = true;
            ShowCoordinatesInfo = true;
            ShowStatusesInfo = true;
            ShowWeaponInfo = true;
            ShowBossInfo = true;
            ShowVelocityInfo = true;
        }
    }
}
