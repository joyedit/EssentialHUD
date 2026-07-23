using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace EssentialHUD
{
    public class HudElementElevation : HudElement
    {
        private long lastUpdateMs = 0;
        private const long UPDATE_INTERVAL_MS = 500; // Update twice per second
        private const int SEA_LEVEL = 110; // Vintage Story's default sea level

        public HudElementElevation(ICoreClientAPI capi) : base(capi)
        {
            ComposeGUI();
        }

        private void ComposeGUI()
        {
            // Position ABOVE the clock using negative Y offset
            ElementBounds dialogBounds = ElementBounds.Fixed(
                EnumDialogArea.RightBottom,
                10,    // X offset from right edge
                -20,   // Closer to clock (was -35)
                300,   // Width
                25     // Height (reduced from 30)
            );

            ElementBounds textBounds = ElementBounds.Fixed(0, 0, 300, 25);

            SingleComposer = capi.Gui.CreateCompo("essentialhud-elevation", dialogBounds)
                .AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "elevationText") // Changed to SmallishText
                .Compose();

            SingleComposer.GetDynamicText("elevationText").SetNewText("Elevation: Loading...");
            
            if (TryOpen())
            {
                capi.Logger.Notification("Essential HUD: Elevation display opened successfully");
            }
            else
            {
                capi.Logger.Error("Essential HUD: Failed to open elevation display");
            }
        }

        private void UpdateElevation()
        {
            var player = capi.World.Player?.Entity;
            if (player == null) return;

            int yPos = (int)player.Pos.Y;
            int relativeToSeaLevel = yPos - SEA_LEVEL;
            
            string displayText;
            
            if (relativeToSeaLevel > 0)
            {
                displayText = $"Elevation: +{relativeToSeaLevel} (Y: {yPos})";
            }
            else if (relativeToSeaLevel < 0)
            {
                displayText = $"Depth: {Math.Abs(relativeToSeaLevel)} (Y: {yPos})";
            }
            else
            {
                displayText = $"Sea Level (Y: {yPos})";
            }

            SingleComposer?.GetDynamicText("elevationText")?.SetNewText(displayText);
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            
            long currentMs = capi.World.ElapsedMilliseconds;
            if (currentMs - lastUpdateMs >= UPDATE_INTERVAL_MS)
            {
                UpdateElevation();
                lastUpdateMs = currentMs;
            }
        }

        public override string ToggleKeyCombinationCode => null;
        
        public override bool ShouldReceiveKeyboardEvents() => false;
        
        public override bool TryClose() => false; // Always visible
        
        public override bool Focusable => false;
    }
}
