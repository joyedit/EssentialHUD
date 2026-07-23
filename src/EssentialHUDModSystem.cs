using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace EssentialHUD
{
    public class EssentialHUDModSystem : ModSystem
    {
        private ICoreClientAPI capi;
        private HudElementEssentialInfo essentialInfoHud;

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;

            // Initialize the unified HUD element
            essentialInfoHud = new HudElementEssentialInfo(capi);

            // Register the HUD element
            capi.Gui.RegisterDialog(essentialInfoHud);
        }

        public override void Dispose()
        {
            essentialInfoHud?.Dispose();
            base.Dispose();
        }
    }
}
