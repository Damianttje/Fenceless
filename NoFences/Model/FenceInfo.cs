using System;
using System.Collections.Generic;

namespace Fenceless.Model
{
    public class FenceInfo
    {
        /* 
         * DO NOT RENAME PROPERTIES. Used for XML serialization.
         */

        public Guid Id { get; set; }

        public string Name { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window height.
        /// </summary>
        public int Height { get; set; }

        public bool Locked { get; set; }

        public bool CanMinify { get; set; }

        /// <summary>
        /// Gets or sets the logical window title height.
        /// </summary>
        public int TitleHeight { get; set; } = 35;

        /// <summary>
        /// Gets or sets the transparency level (0-100, where 100 is fully opaque)
        /// </summary>
        public int Transparency { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether the fence should auto-hide when not in use
        /// </summary>
        public bool AutoHide { get; set; } = false;

        /// <summary>
        /// Gets or sets the auto-hide delay in milliseconds
        /// </summary>
        public int AutoHideDelay { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the background color of the fence (ARGB format)
        /// </summary>
        public int BackgroundColor { get; set; } = unchecked((int)0xFF000000); // Black with full alpha

        /// <summary>
        /// Gets or sets the title background color of the fence (ARGB format)
        /// </summary>
        public int TitleBackgroundColor { get; set; } = unchecked((int)0x80000000); // Semi-transparent black

        /// <summary>
        /// Gets or sets the text color of the fence (ARGB format)
        /// </summary>
        public int TextColor { get; set; } = unchecked((int)0xFFFFFFFF); // White

        /// <summary>
        /// Gets or sets the border color of the fence (ARGB format)
        /// </summary>
        public int BorderColor { get; set; } = unchecked((int)0xFF808080); // Gray

        /// <summary>
        /// Gets or sets the border width in pixels
        /// </summary>
        public int BorderWidth { get; set; } = 0;

        /// <summary>
        /// Gets or sets the corner radius for rounded corners
        /// </summary>
        public int CornerRadius { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to show shadow effect
        /// </summary>
        public bool ShowShadow { get; set; } = true;

        /// <summary>
        /// Gets or sets the icon size (16, 24, 32, 48, 64)
        /// </summary>
        public int IconSize { get; set; } = 32;

        /// <summary>
        /// Gets or sets the spacing between items
        /// </summary>
        public int ItemSpacing { get; set; } = 15;

        /// <summary>
        /// Gets or sets the background color transparency (0-100, where 100 is fully opaque)
        /// </summary>
        public int BackgroundTransparency { get; set; } = 100;

        /// <summary>
        /// Gets or sets the title background color transparency (0-100, where 100 is fully opaque)
        /// </summary>
        public int TitleBackgroundTransparency { get; set; } = 80;

        /// <summary>
        /// Gets or sets the text color transparency (0-100, where 100 is fully opaque)
        /// </summary>
        public int TextTransparency { get; set; } = 100;

        /// <summary>
        /// Gets or sets the border color transparency (0-100, where 100 is fully opaque)
        /// </summary>
        public int BorderTransparency { get; set; } = 100;

        public List<string> Files { get; set; } = new List<string>();

        public string SortColumn { get; set; } = "";

        public bool SortAscending { get; set; } = true;

        public string SearchFilter { get; set; } = "";

        public FenceInfo()
        {

        }

        public FenceInfo(Guid id)
        {
            Id = id;
        }
    }
}
