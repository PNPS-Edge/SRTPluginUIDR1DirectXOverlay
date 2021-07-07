using System;
using System.Collections.Generic;
using System.Text;

namespace SRTPluginUIDR1DirectXOverlay.Structs
{
    /// <summary>
    /// Class for layout definition in the configuration file
    /// </summary>
    [Serializable]
    public class LayoutConfiguration
    {
        /// <summary>
        /// Gets or sets the font used
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// Gets or sets the font size
        /// </summary>
        public float FontSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the overlay is displayed on the right of the screen
        /// </summary>
        public bool IsRightDocked { get; set; }

        /// <summary>
        /// Gets or sets the X position of the overlay.
        /// If the overlay is right docked. X position is from the right limit of the game
        /// </summary>
        public float XPosition { get; set; }

        /// <summary>
        /// Gets or sets the Y position of the overlay.
        /// </summary>
        public float YPosition { get; set; }

        /// <summary>
        /// Gets or sets the width of the elements to display
        /// </summary>
        public float ElementWidth { get; set; }

        /// <summary>
        /// Gets or sets the bottom offset betmeen elements
        /// </summary>
        public float ElementOffset { get; set; }

        public float ColumnOffset { get; set; }
    }
}
