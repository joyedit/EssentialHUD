using System;
using System.IO;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace EssentialHUD
{
    public class HudElementEssentialInfo : HudElement
    {
        private long lastClockUpdateMs = 0;
        private long lastDataUpdateMs = 0;
        private const long CLOCK_UPDATE_INTERVAL_MS = 1000;
        private const long DATA_UPDATE_INTERVAL_MS = 500;
        private const int SEA_LEVEL = 110;
        
        private Vec3d homePosition = null;
        
        private string HomeDataPath => Path.Combine(GamePaths.DataPath, "ModData", "essentialhud", "home.json");

        public HudElementEssentialInfo(ICoreClientAPI capi) : base(capi)
        {
            ComposeGUI();
            RegisterCommands();
            LoadHomePosition();
        }
        
        private void RegisterCommands()
        {
            capi.ChatCommands.Create("sethome")
                .WithDescription("Set your home location for the Essential HUD")
                .HandleWith(OnSetHomeCommand);
                
            capi.ChatCommands.Create("clearhome")
                .WithDescription("Clear your home location")
                .HandleWith(OnClearHomeCommand);
        }
        
        private TextCommandResult OnSetHomeCommand(TextCommandCallingArgs args)
        {
            var playerPos = capi.World.Player.Entity.Pos;
            homePosition = new Vec3d(playerPos.X, playerPos.Y, playerPos.Z);
            SaveHomePosition();
            
            return TextCommandResult.Success($"Home set at X: {(int)playerPos.X}, Z: {(int)playerPos.Z}");
        }
        
        private TextCommandResult OnClearHomeCommand(TextCommandCallingArgs args)
        {
            homePosition = null;
            DeleteHomePosition();
            return TextCommandResult.Success("Home location cleared");
        }
        
        private void SaveHomePosition()
        {
            try
            {
                string dir = Path.GetDirectoryName(HomeDataPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                if (homePosition != null)
                {
                    string json = $"{{\"x\":{homePosition.X},\"y\":{homePosition.Y},\"z\":{homePosition.Z}}}";
                    File.WriteAllText(HomeDataPath, json);
                    capi.Logger.Notification($"[EssentialHUD] Home position saved");
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"[EssentialHUD] Failed to save home: {ex.Message}");
            }
        }
        
        private void LoadHomePosition()
        {
            try
            {
                if (File.Exists(HomeDataPath))
                {
                    string json = File.ReadAllText(HomeDataPath);
                    var x = double.Parse(Regex.Match(json, "\"x\":([\\d.-]+)").Groups[1].Value);
                    var y = double.Parse(Regex.Match(json, "\"y\":([\\d.-]+)").Groups[1].Value);
                    var z = double.Parse(Regex.Match(json, "\"z\":([\\d.-]+)").Groups[1].Value);
                    homePosition = new Vec3d(x, y, z);
                    capi.Logger.Notification($"[EssentialHUD] Loaded home position: {(int)x}, {(int)y}, {(int)z}");
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"[EssentialHUD] Failed to load home: {ex.Message}");
            }
        }
        
        private void DeleteHomePosition()
        {
            try
            {
                if (File.Exists(HomeDataPath))
                {
                    File.Delete(HomeDataPath);
                    capi.Logger.Notification($"[EssentialHUD] Home position deleted");
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"[EssentialHUD] Failed to delete home: {ex.Message}");
            }
        }

        private void ComposeGUI()
        {
            ElementBounds bgBounds = ElementBounds.Fixed(
                EnumDialogArea.LeftTop,
                10,
                10,
                320,
                110
            );

            ElementBounds textBounds = ElementBounds.Fixed(10, 8, 300, 95);

            SingleComposer = capi.Gui.CreateCompo("essentialhud-info", bgBounds)
                .AddShadedDialogBG(bgBounds.FlatCopy(), false)
                .AddDynamicText("", CairoFont.WhiteDetailText().WithFontSize(12), textBounds, "infoText")
                .Compose();

            UpdateDisplay();
            
            TryOpen();
        }

        private void UpdateDisplay()
        {
            string seasonTempText = GetSeasonAndTemperature();
            string gameTimeText = GetGameTime();
            string windText = GetWindInfo();
            string elevationText = GetElevation();
            string distanceText = GetDistanceToHome();

            string displayText = $"{seasonTempText}\n{gameTimeText}\n{windText}\n{elevationText}\n{distanceText}";

            SingleComposer?.GetDynamicText("infoText")?.SetNewText(displayText);
        }

        private string GetSeasonAndTemperature()
        {
            try
            {
                var blockPos = capi.World.Player.Entity.Pos.AsBlockPos;
                var climate = capi.World.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.NowValues);
                if (climate != null)
                {
                    // Convert to Fahrenheit
                    double tempF = (climate.Temperature * 9.0 / 5.0) + 32.0;
                    
                    // Get season and convert enum to string
                    var calendar = capi.World.Calendar;
                    string season = calendar.GetSeason(blockPos).ToString();
                    
                    return $"{season}, {tempF:F1}°F";
                }
            }
            catch { }
            
            return "Temperature: Unknown";
        }

        private string GetGameTime()
        {
            try
            {
                var calendar = capi.World.Calendar;
                int year = (int)(calendar.TotalDays / calendar.DaysPerYear) + 1;
                int dayOfYear = (int)(calendar.TotalDays % calendar.DaysPerYear);
                int month = (dayOfYear / calendar.DaysPerMonth) + 1;
                int day = (dayOfYear % calendar.DaysPerMonth) + 1;
                
                double hourOfDay = calendar.HourOfDay;
                int hours = (int)hourOfDay;
                int minutes = (int)((hourOfDay - hours) * 60);
                
                // Simple month names array
                string[] monthNames = { "January", "February", "March", "April", "May", "June", 
                                       "July", "August", "September", "October", "November", "December" };
                string monthName = monthNames[(month - 1) % 12];
                
                return $"{day}. {monthName}, Year {year}, {hours:D2}:{minutes:D2}";
            }
            catch { }
            
            return "Game Time: Unknown";
        }

        private string GetWindInfo()
        {
            try
            {
                // Try to get weather data from the world
                var blockPos = capi.World.Player.Entity.Pos.AsBlockPos;
                var windVec = capi.World.BlockAccessor.GetWindSpeedAt(blockPos);
                
                // Get magnitude of wind vector
                double windSpeed = Math.Sqrt(windVec.X * windVec.X + windVec.Y * windVec.Y + windVec.Z * windVec.Z);
                
                // Simple wind description based on intensity
                string windDescription = "Calm";
                if (windSpeed > 0.8) windDescription = "Strong Breeze";
                else if (windSpeed > 0.6) windDescription = "Fresh Breeze";
                else if (windSpeed > 0.4) windDescription = "Moderate Breeze";
                else if (windSpeed > 0.2) windDescription = "Gentle Breeze";
                else if (windSpeed > 0.05) windDescription = "Light Breeze";
                
                return $"Wind: {windDescription}";
            }
            catch { }
            
            return "Wind: Calm";
        }

        private string GetElevation()
        {
            var player = capi.World.Player?.Entity;
            if (player == null) return "Elevation: Unknown";

            int yPos = (int)player.Pos.Y;
            int relativeToSeaLevel = yPos - SEA_LEVEL;
            
            if (relativeToSeaLevel > 0)
            {
                return $"Elevation: +{relativeToSeaLevel} (Y: {yPos})";
            }
            else if (relativeToSeaLevel < 0)
            {
                return $"Depth: {Math.Abs(relativeToSeaLevel)} (Y: {yPos})";
            }
            else
            {
                return $"Sea Level (Y: {yPos})";
            }
        }

        private string GetDistanceToHome()
        {
            if (homePosition == null)
            {
                return "Distance to Home: Not Set";
            }
            
            try
            {
                var playerPos = capi.World.Player.Entity.Pos.AsBlockPos;
                
                double dx = homePosition.X - playerPos.X;
                double dz = homePosition.Z - playerPos.Z;
                int distance = (int)Math.Sqrt(dx * dx + dz * dz);
                
                return $"Distance to Home: {distance} blocks";
            }
            catch
            {
                return "Distance to Home: Error";
            }
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            
            long currentMs = capi.World.ElapsedMilliseconds;
            
            bool updateClock = currentMs - lastClockUpdateMs >= CLOCK_UPDATE_INTERVAL_MS;
            if (updateClock) lastClockUpdateMs = currentMs;

            bool updateData = currentMs - lastDataUpdateMs >= DATA_UPDATE_INTERVAL_MS;
            if (updateData) lastDataUpdateMs = currentMs;

            if (updateClock || updateData)
            {
                UpdateDisplay();
            }
        }

        public override string ToggleKeyCombinationCode => null;
        
        public override bool ShouldReceiveKeyboardEvents() => false;
        
        public override bool TryClose() => false;
        
        public override bool Focusable => false;
    }
}
