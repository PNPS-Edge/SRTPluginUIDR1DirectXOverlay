using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderDR1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SRTPluginUIDR1DirectXOverlay
{
    public class SRTPluginUIDR1DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();

        public string RequiredProvider => "SRTPluginProviderDR1";
        private Process GetProcess() => Process.GetProcessesByName("deadrising")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        public override IPluginInfo Info => _Info;
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryDR1 gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _font;
        private float _textXOffset = 22f;
        private float _text1YOffset = 5f;
        private float _textYOffset;

        // Dead Rising color
        private SolidBrush _darkerblue;

        // Font
        private SolidBrush _white;

        // Gradient Progress Bar
        private SolidBrush _darkred;
        private SolidBrush _lightred;

        private SolidBrush _darkyellow;
        private SolidBrush _lightyellow;

        private SolidBrush _darkgreen;
        private SolidBrush _lightgreen;

        private SolidBrush _greydarker;

        public PluginConfiguration config;

        private float _previousXCoordinates;
        private float _previousYCoordinates;
        private float _previousZCoordinates;

        private Vector3 _previousPosition;

        private List<double> _previousSpeedValues;

        #region Methods

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            // Get the game process
            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;

            // Injects the host delegates to communicate with host
            this.hostDelegates = hostDelegates;

            // Load config file
            config = LoadConfiguration<PluginConfiguration>();

            // Get game window
            gameWindowHandle = gameProcess.MainWindowHandle;

            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            PInvoke.EnumDisplaySettings(null, -1, ref devMode);

            // Create and initialize the overlay window.
            _window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
            _window?.Create();

            // Create and initialize the graphics object.
            _graphics = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = false,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = false,
                Width = _window.Width,
                Height = _window.Height,
                WindowHandle = _window.Handle
            };

            _graphics.Setup();

            // Get a reference to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            // Font creation
            _font = _graphics.CreateFont(config.Layout.FontName, 12, false);

            // Progress Bar Gradient
            // Font color
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183, 255);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 0, 255);
            _lightgreen = _graphics?.CreateSolidBrush(0, 255, 0, 255);

            // Background colors
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);

            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 140);

            // Dead Rising blue
            _darkerblue = _graphics?.CreateSolidBrush(0, 0, 48, 140);

            // General Font Color
            _white = _graphics?.CreateSolidBrush(255, 255, 255, 255);

            _previousSpeedValues = new List<double>();
            _previousPosition = new Vector3();

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryDR1)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics.BeginScene();
                _graphics.ClearScene();

                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);

                if (_graphics != null && this.gameMemory.Game.IsGamePaused == false && this.gameMemory.Game.GameMenu == 3)
                    DrawOverlay();

                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            _darkerblue?.Dispose();

            _white?.Dispose();
            _greydarker?.Dispose();
            _darkred?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();

            _font?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        #endregion Methods

        #region Functions

        private void DrawOverlay()
        {
            _textYOffset = GetLineHeight();

            float statsXOffset;

            if (config.Layout.IsRightDocked)
            {
                statsXOffset = _graphics.Width - config.Layout.XPosition - config.Layout.ElementWidth;
            }
            else
            {
                statsXOffset = config.Layout.XPosition;
            }

            float statsYOffset = config.Layout.YPosition;


            if (config.ShowCampainInfo)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add(string.Empty, GetGameTimeToString(gameMemory.Campain.GameTime));
                parameters.Add("Campain", string.Format("{0} ({1})", GetCaseNameFromCampainProgress(gameMemory.Campain.CampaignProgress), gameMemory.Campain.CampaignProgress.ToString()));

                if (config.Debug)
                {
                    parameters.Add("Room", gameMemory.Campain.RoomId.ToString());
                    parameters.Add("Room From", gameMemory.Campain.LoadingRoom1Id.ToString());
                    parameters.Add("Room To", gameMemory.Campain.LoadingRoom1Id.ToString());
                }

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowCoordinatesInfo)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("X", gameMemory.Player.XPosition.ToString());
                parameters.Add("Y", gameMemory.Player.YPosition.ToString());
                parameters.Add("Z", gameMemory.Player.ZPosition.ToString());
                parameters.Add("Rot.1", gameMemory.Player.Rotation1.ToString());
                parameters.Add("Rot.2", gameMemory.Player.Rotation2.ToString());
                
                if (config.Debug)
                {
                    parameters.Add("Cam X", gameMemory.CameraXPosition.ToString());
                    parameters.Add("Cam Y", gameMemory.CameraYPosition.ToString());
                    parameters.Add("Cam Z", gameMemory.CameraZPosition.ToString());
                }

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowStatusesInfo)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("Life", string.Format("{0} / {1}", gameMemory.Player.CurrentHealth, gameMemory.Player.MaxHealth));
                parameters.Add("Stock", (gameMemory.Player.StatusItemStock + 1).ToString());

                if (config.Debug)
                {
                    parameters.Add("Level", gameMemory.Player.Level.ToString());
                    parameters.Add("PP Counter", gameMemory.Player.PPCounter.ToString());
                    parameters.Add("Attack", gameMemory.Player.StatusAttack.ToString());
                    parameters.Add("Speed", gameMemory.Player.StatusSpeed.ToString());
                    parameters.Add("ThrowDistance", gameMemory.Player.StatusThrowDistance.ToString()); 
                }

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowVelocityInfo)
            {
                ComputeVelocity();

                double averageSpeed = 0;

                foreach (var speedValues in _previousSpeedValues)
                {
                    averageSpeed += speedValues;
                }

                var speed = averageSpeed / _previousSpeedValues.Count;

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("Speed", string.Format("{0:0.00}", speed));

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowWeaponInfo && gameMemory.WeaponMaxAmmo == 1 && gameMemory.WeaponMaxDurability > 10)
            {
                DrawProgressBar(ref statsXOffset, ref statsYOffset, gameMemory.WeaponDurability, gameMemory.WeaponMaxDurability, "Item");
            }

            if (config.ShowCarHealthInfo && gameMemory.Campain.RoomId == 1536 && gameMemory.Campain.CampaignProgress > 340 && gameMemory.Campain.CampaignProgress < 400)
            {
                DrawProgressBar(ref statsXOffset, ref statsYOffset, gameMemory.TunnelCarCurrentHealth, gameMemory.TunnelCarMaxHealth, "Car");
            }

            if (config.ShowBossInfo && gameMemory.BossMaxHealth > 900 && gameMemory.BossMaxHealth < 10001)
            {
                DrawProgressBar(ref statsXOffset, ref statsYOffset, gameMemory.BossCurrentHealth, gameMemory.BossMaxHealth, "Boss");
            }
        }

        private void DrawBlocInfo(ref float xOffset, ref float yOffset, Dictionary<string, string> parameters)
        {
            SolidBrush TextColor = _white;

            float elementHeight = (GetLineHeight() * parameters.Count) + 8f;

            NewColumnNeeded(ref xOffset, ref yOffset, elementHeight);

            _graphics.FillRectangle(_darkerblue, xOffset, yOffset, xOffset + config.Layout.ElementWidth, yOffset + elementHeight);

            for (int i = 0; i < parameters.Count; i++)
            {
                var yoffset = i == 0 ? _text1YOffset : _textYOffset;

                _graphics.DrawText(
                        _font,
                        config.Layout.FontSize,
                        TextColor,
                        xOffset + _textXOffset,
                        yOffset += yoffset,
                        string.Format("{0}{1}{2}", parameters.ElementAt(i).Key, parameters.ElementAt(i).Key == string.Empty ? string.Empty : ": ", parameters.ElementAt(i).Value)
                        );
            }

            yOffset += (elementHeight - _text1YOffset - (_textYOffset * (parameters.Count - 1)));

            yOffset += config.Layout.ElementOffset;
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, float currentValue, float maxValue, string title)
        {
            // Define steps for color changement
            float step1 = maxValue / 3 * 2;
            float step2 = maxValue / 3;

            float percentage = currentValue / maxValue;

            // Define colors according to steps
            SolidBrush BarColor = (currentValue > step1) ? _darkgreen : (currentValue > step2) ? _darkyellow : (currentValue <= step2) ? _darkred : _greydarker;
            SolidBrush TextColor = (currentValue > step1) ? _lightgreen : (currentValue > step2) ? _lightyellow : (currentValue < step2) ? _lightred : _white;

            float elementHeight = (GetLineHeight() * 1) + 8f;

            NewColumnNeeded(ref xOffset, ref yOffset, elementHeight);

            // Draw the rectangle
            _graphics.FillRectangle(_darkerblue, xOffset, yOffset, xOffset + config.Layout.ElementWidth, yOffset + elementHeight);
            _graphics.FillRectangle(BarColor, xOffset, yOffset, xOffset + ((config.Layout.ElementWidth) * percentage), yOffset + elementHeight);

            // Define text to display
            string currentValueInfo = float.IsNaN(currentValue) ? string.Empty : string.Format("{0}: {1}", title, (int)currentValue);
            string percentInfo = float.IsNaN(percentage) ? "0%" : string.Format("({0:P1})", percentage);
            float endOfBar = (xOffset + config.Layout.ElementWidth) - _textXOffset - GetStringSize(percentInfo, config.Layout.FontSize);

            // Draw text
            _graphics.DrawText(_font, config.Layout.FontSize, TextColor, xOffset + _textXOffset, yOffset + _text1YOffset, currentValueInfo);
            _graphics.DrawText(_font, config.Layout.FontSize, TextColor, endOfBar, yOffset + _text1YOffset, percentInfo);

            yOffset += elementHeight;

            yOffset += config.Layout.ElementOffset;
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_font, size, str).X;
        }

        /// <summary>
        /// Gets the height of the line according the choosen font size
        /// </summary>
        /// <param name="offset">Offset to add to the line</param>
        /// <returns>Height of the line</returns>
        private float GetLineHeight(float offset = 0f)
        {
            // fontSize / 72 = to height of the font in inches. 
            // height * 96 = conversion to inches to pixels
            return (config.Layout.FontSize / 72 * 96) + offset;
        }

        private void NewColumnNeeded(ref float xOffset, ref float yOffset, float elementHeight)
        {
            if (yOffset + elementHeight > _graphics.Height - 15f)
            {
                if (config.Layout.IsRightDocked)
                {
                    xOffset = xOffset - (config.Layout.ElementWidth + config.Layout.ColumnOffset);
                }
                else
                {
                    xOffset = xOffset + config.Layout.ElementWidth + config.Layout.ColumnOffset;
                }

                yOffset = config.Layout.YPosition;
            }
        }

        private string GetGameTimeToString(uint gametime)
        {
            uint day = gametime / (108000) / 24,
                hours = gametime / (108000) % 24,
                minutes = gametime / (108000 / 60) % 60,
                seconds = gametime / (108000 / 60 / 60) % 60;

            string suffix = "AM";
            if (hours >= 12)
            {
                suffix = "PM";
                hours %= 12;
            }
            if (hours == 0) { hours = 12; }

            return string.Format("Day {0} - {1}:{2}:{3} {4}", (int)day, hours.ToString("D2"), minutes.ToString("D2"), seconds.ToString("D2"), suffix);
        }

        private string GetCaseNameFromCampainProgress(int campainStatus)
        {
            switch (campainStatus)
            {
                case < 80:
                    return "Prologue";
                case < 110:
                    return "Case 1.1";
                case < 130:
                    return "Case 1.2";
                case < 140:
                    return "Case 1.3";
                case < 150:
                    return "Case 1.4";
                case < 205:
                    return "Case 2.1";
                case < 215:
                    return "Case 2.2";
                case < 220:
                    return "Case 2.3";
                case < 230:
                    return "Case 3.1";
                case < 250:
                    return "Case 4.1";
                case < 280:
                    return "Case 4.2";
                case < 290:
                    return "Case 5.1";
                case < 300:
                    return "Case 5.2";
                case < 320:
                    return "Case 6.1";
                case < 340:
                    return "Case 7.1";
                case < 350:
                    return "Case 7.2";
                case < 360:
                    return "Case 8.1";
                case < 370:
                    return "Case 8.2";
                case < 390:
                    return "Case 8.3";
                case < 400:
                    return "Case 8.4";
                case < 500:
                    return "THE FACTS";
                case < 650:
                    return "Overtime 1";
                case < 999:
                    return "Overtime 2";
                default:
                    return "Truth vanished!!";
            }
        }

        private void ComputeVelocity()
        {
            var distX = _previousXCoordinates - gameMemory.Player.XPosition;
            var distY = _previousYCoordinates - gameMemory.Player.YPosition;
            var distZ = _previousZCoordinates - gameMemory.Player.ZPosition;

            var speedX = distX / (gameMemory.Campain.GameTime / 1000);
            var speedY = distY / (gameMemory.Campain.GameTime / 1000);
            var speedZ = distZ / (gameMemory.Campain.GameTime / 1000);

            var speed = (Math.Sqrt(Math.Pow(speedX, 2) + Math.Pow(speedY, 2) + Math.Pow(speedZ, 2)) * 1000);

            _previousXCoordinates = gameMemory.Player.XPosition;
            _previousYCoordinates = gameMemory.Player.YPosition;
            _previousZCoordinates = gameMemory.Player.ZPosition;

            _previousSpeedValues.Add(speed);

            if (speed > (_previousSpeedValues[_previousSpeedValues.Count - 1] * 3))
            {
                _previousSpeedValues.RemoveAt(_previousSpeedValues.Count - 1);
            }

            if (_previousSpeedValues.Count > config.SpeedAverageFactor)
            {
                _previousSpeedValues.RemoveAt(0);
            }

            if (speed == 0)
            {
                for (int i = 0; i < _previousSpeedValues.Count; i++)
                {
                    _previousSpeedValues[i] = 0;
                }
            }
        }

        #endregion Functions
    }

}
