using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace EssentialHUD
{
    public class HudElementDistanceToHome : HudElement
    {
        private long lastUpdateMs = 0;
        private const long UPDATE_INTERVAL_MS = 500; // Update twice per second

        public HudElementDistanceToHome(ICoreClientAPI capi) : base(capi)
        {
            ComposeGUI();
        }

        private void ComposeGUI()
        {
            // Position ABOVE elevation using more negative Y offset
            ElementBounds dialogBounds = ElementBounds.Fixed(
                EnumDialogArea.RightBottom,
                10,    // X offset from right edge
                -40,   // Closer spacing (was -70)
                300,   // Width
                25     // Height (reduced from 30)
            );

            ElementBounds textBounds = ElementBounds.Fixed(0, 0, 300, 25);

            SingleComposer = capi.Gui.CreateCompo("essentialhud-distancetohome", dialogBounds)
                .AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "distanceText") // Changed to SmallishText
                .Compose();

            SingleComposer.GetDynamicText("distanceText").SetNewText("Home: Not Set");
            
            if (TryOpen())
            {
                capi.Logger.Notification("Essential HUD: Distance to home display opened successfully");
            }
            else
            {
                capi.Logger.Error("Essential HUD: Failed to open distance to home display");
            }
        }

        private void UpdateDistance()
        {
            string displayText = "Home: Not Set";
            
            try
            {
                // Access the world map manager through the mod loader
                var mapManager = capi.ModLoader.GetModSystem("Vintagestory.GameContent.WorldMapManager");
                if (mapManager == null) return;

                // Use reflection to get waypoints
                var waypointMapLayerMethod = mapManager.GetType().GetMethod("WaypointMapLayer");
                if (waypointMapLayerMethod == null) return;

                var waypointLayer = waypointMapLayerMethod.Invoke(mapManager, null);
                if (waypointLayer == null) return;

                var ownWaypointsField = waypointLayer.GetType().GetField("ownWaypoints");
                if (ownWaypointsField == null) return;

                var waypoints = ownWaypointsField.GetValue(waypointLayer) as System.Collections.IEnumerable;
                if (waypoints == null) return;

                // DEBUG: Log all waypoint icons
                bool foundAny = false;
                foreach (var wp in waypoints)
                {
                    var iconProperty = wp.GetType().GetProperty("Icon");
                    string icon = iconProperty?.GetValue(wp) as string;
                    if (!foundAny)
                    {
                        capi.Logger.Notification($"[EssentialHUD] Found waypoint with icon: '{icon}'");
                        foundAny = true;
                    }
                }

                // Find home waypoint - check multiple possible icon names
                foreach (var wp in waypoints)
                {
                    var iconProperty = wp.GetType().GetProperty("Icon");
                    var positionProperty = wp.GetType().GetProperty("Position");
                    
                    string icon = iconProperty?.GetValue(wp) as string;
                    
                    // Check for various home icon names
                    if ((icon == "home" || icon == "home1" || icon == "home2" || icon?.Contains("home") == true) 
                        && positionProperty != null)
                    {
                        var homePos = positionProperty.GetValue(wp) as Vec3d;
                        if (homePos != null)
                        {
                            var playerPos = capi.World.Player.Entity.Pos.AsBlockPos;
                            
                            // Calculate 2D horizontal distance (ignoring Y)
                            double dx = homePos.X - playerPos.X;
                            double dz = homePos.Z - playerPos.Z;
                            int distance = (int)Math.Sqrt(dx * dx + dz * dz);
                            
                            displayText = $"Distance to Home: {distance} blocks";
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"[EssentialHUD] Error in UpdateDistance: {ex.Message}");
            }

            SingleComposer?.GetDynamicText("distanceText")?.SetNewText(displayText);
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            
            long currentMs = capi.World.ElapsedMilliseconds;
            if (currentMs - lastUpdateMs >= UPDATE_INTERVAL_MS)
            {
                UpdateDistance();
                lastUpdateMs = currentMs;
            }
        }

        public override string ToggleKeyCombinationCode => null;
        
        public override bool ShouldReceiveKeyboardEvents() => false;
        
        public override bool TryClose() => false; // Always visible
        
        public override bool Focusable => false;
    }
}
