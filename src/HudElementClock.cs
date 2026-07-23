using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace EssentialHUD
{
    public class HudElementClock : HudElement
    {
        private long lastUpdateMs = 0;
        private const long UPDATE_INTERVAL_MS = 1000; // Update every second

        public HudElementClock(ICoreClientAPI capi) : base(capi)
        {
            ComposeGUI();
        }

        private void ComposeGUI()
        {
            // Position at bottom right corner (bottom of the stack)
            ElementBounds dialogBounds = ElementBounds.Fixed(
                EnumDialogArea.RightBottom, 
                10,    // X offset from right edge
                10,    // Y offset from bottom edge
                300,   // Width
                25     // Height (reduced from 30)
            );

            ElementBounds textBounds = ElementBounds.Fixed(0, 0, 300, 25);

            SingleComposer = capi.Gui.CreateCompo("essentialhud-clock", dialogBounds)
                .AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "clockText") // Changed to SmallishText
                .Compose();

            SingleComposer.GetDynamicText("clockText").SetNewText(GetFormattedTime());
            
            TryOpen();
        }

        private string GetFormattedTime()
        {
            DateTime now = DateTime.Now;
            return $"Local Time: {now.ToString("h:mm:ss tt")}"; // Add prefix
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            
            long currentMs = capi.World.ElapsedMilliseconds;
            if (currentMs - lastUpdateMs >= UPDATE_INTERVAL_MS)
            {
                SingleComposer.GetDynamicText("clockText").SetNewText(GetFormattedTime());
                lastUpdateMs = currentMs;
            }
        }

        public override string ToggleKeyCombinationCode => null;
        
        public override bool ShouldReceiveKeyboardEvents() => false;
        
        public override bool TryClose() => false; // Always visible
        
        public override bool Focusable => false;
    }
}
