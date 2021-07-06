using SRTPluginBase;
using System;

namespace SRTPluginUIDR1DirectXOverlay
{
    internal class PluginInfo : IPluginInfo
    {
        public string Name => "DirectX Overlay UI (Dead Rising (2016))";

        public string Description => "A DirectX-based Overlay User Interface for displaying Dead Rising (2016) game memory values.";

        public string Author => "Pix N Pick Studio & VideoGameRoulette";

        public Uri MoreInfoURL => new Uri("https://github.com/VideoGameRoulette/SRTPluginUIDR1DirectXOverlay");

        public int VersionMajor => assemblyFileVersion.ProductMajorPart;

        public int VersionMinor => assemblyFileVersion.ProductMinorPart;

        public int VersionBuild => assemblyFileVersion.ProductBuildPart;

        public int VersionRevision => assemblyFileVersion.ProductPrivatePart;

        private System.Diagnostics.FileVersionInfo assemblyFileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
    }
}
