using SRTPluginUIDR1DirectXOverlay.Structs;
using System.ComponentModel.DataAnnotations;

namespace SRTPluginUIDR1DirectXOverlay
{
    public class PluginConfiguration
    {
        public LayoutConfiguration Layout { get; set; }
        public bool Debug { get; set; }
        public float ScalingFactor { get; set; }

        public bool ShowCampainInfo { get; set; }
        public bool ShowCoordinatesInfo { get; set; }
        public bool ShowStatusesInfo { get; set; }
        public bool ShowWeaponInfo { get; set; }
        public bool ShowBossInfo { get; set; }
        public bool ShowVelocityInfo { get; set; }

        public bool ShowCarHealthInfo { get; set; }

        public int SpeedAverageFactor { get; set; }

        public PluginConfiguration()
        {
            Debug = false;
            ScalingFactor = 1f;

            Layout = new LayoutConfiguration();
            Layout.FontName = "Lucida Sans";
            Layout.FontSize = 22;
            Layout.IsRightDocked = true;
            Layout.XPosition = 150f;
            Layout.YPosition = 320f;
            Layout.ElementWidth = 320f;
            Layout.ElementOffset = 10f;
            Layout.ColumnOffset = 10f;

            ShowCampainInfo = true;
            ShowCoordinatesInfo = true;
            ShowStatusesInfo = true;
            ShowWeaponInfo = true;
            ShowBossInfo = true;
            ShowVelocityInfo = true;
            ShowCarHealthInfo = true;


            SpeedAverageFactor = 20;
        }
    }
}
