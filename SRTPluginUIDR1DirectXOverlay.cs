using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderDR1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
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

        private Font _consolasBold;
        private float _fontSize = 22f;
        private float _textXOffset = 22f;
        private float _text1YOffset = 5f;
        private float _textYOffset = 28f;
        private float _elementSize = 320f;

        private SolidBrush _darkblue;
        private SolidBrush _darkerblue;
        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _lightred;
        private SolidBrush _lightyellow;
        private SolidBrush _lightgreen;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;
        private SolidBrush _darkgreen;
        private SolidBrush _darkyellow;

        public PluginConfiguration config;

        private float _previousXCoordinates;
        private float _previousYCoordinates;
        private float _previousZCoordinates;

        private List<double> _previousSpeedValues;

        #region Methods

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
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

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont(config.StringFontName, 12, true);

            // Variant Gradient
            // Light
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183, 255);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 0, 255);
            _lightgreen = _graphics?.CreateSolidBrush(0, 255, 0, 255);

            // Dark
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);

            // Dead Rising blues
            _darkblue = _graphics?.CreateSolidBrush(0, 12, 64, 140);
            _darkerblue = _graphics?.CreateSolidBrush(0, 0, 48, 140);


            _black = _graphics?.CreateSolidBrush(0, 0, 0, 140);
            _white = _graphics?.CreateSolidBrush(255, 255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128, 140);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64, 140);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 140);

            _red = _graphics?.CreateSolidBrush(255, 0, 0, 140);

            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0, 140);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32, 140);

            _previousSpeedValues = new List<double>();

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

                if (_graphics != null && this.gameMemory.IsGamePaused == false && this.gameMemory.GameMenu == 3)
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

            _darkblue?.Dispose();
            _darkerblue?.Dispose();

            _black?.Dispose();
            _white?.Dispose();
            _grey?.Dispose();
            _greydark?.Dispose();
            _greydarker?.Dispose();
            _darkred?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _red?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();
            _lawngreen?.Dispose();
            _goldenrod?.Dispose();

            _consolasBold?.Dispose();

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
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;
            _elementSize = config.ElementWidth;

            // Player HP
            float statsXOffset;

            if (config.DockRight)
            {
                statsXOffset = _graphics.Width - baseXOffset - _elementSize;
            }
            else
            {
                statsXOffset = baseXOffset + 5f;
            }

            float statsYOffset = baseYOffset + 0f;

            if (config.ShowCampainInfo)
            {
                DrawCampainInfo(ref statsXOffset, ref statsYOffset, gameMemory.GameTime);
            }

            if (config.ShowCoordinatesInfo)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("X", gameMemory.PlayerXPosition.ToString());
                parameters.Add("Y", gameMemory.PlayerYPosition.ToString());
                parameters.Add("Z", gameMemory.PlayerZPosition.ToString());
                parameters.Add("Rotation1", gameMemory.PlayerRotation1.ToString());
                parameters.Add("Rotation2", gameMemory.PlayerRotation2.ToString());

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowStatusesInfo)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("Attack", gameMemory.Attack.ToString());
                parameters.Add("Speed", gameMemory.Speed.ToString());
                parameters.Add("Life", gameMemory.Life.ToString());
                parameters.Add("Stock", (gameMemory.ItemStock + 1).ToString());
                parameters.Add("ThrowDistance", gameMemory.ThrowDistance.ToString());

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowVelocityInfo)
            {
                var distX = _previousXCoordinates - gameMemory.PlayerXPosition;
                var distY = _previousYCoordinates - gameMemory.PlayerYPosition;
                var distZ = _previousZCoordinates - gameMemory.PlayerZPosition;

                var speedX = distX / (gameMemory.GameTime / 1000);
                var speedY = distY / (gameMemory.GameTime / 1000);
                var speedZ = distZ / (gameMemory.GameTime / 1000);

                var speed =  (Math.Sqrt(Math.Pow(speedX ,2) + Math.Pow(speedY, 2) + Math.Pow(speedZ, 2)) * 1000);

                _previousXCoordinates = gameMemory.PlayerXPosition;
                _previousYCoordinates = gameMemory.PlayerYPosition;
                _previousZCoordinates = gameMemory.PlayerZPosition;

                if (!(speed > (_previousSpeedValues[_previousSpeedValues.Count -1] * 3)))
                {
                    _previousSpeedValues.Add(speed);
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

                double averageSpeed = 0;

                foreach (var speedValues in _previousSpeedValues)
                {
                    averageSpeed += speedValues;
                }

                speed = averageSpeed / _previousSpeedValues.Count;

                //var speed = gameMemory.WalkedDistance - _previousWalkedDistance;
                //_previousWalkedDistance = gameMemory.WalkedDistance;

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("Speed", string.Format("{0:0.00}", speed));

                DrawBlocInfo(ref statsXOffset, ref statsYOffset, parameters);
            }

            if (config.ShowWeaponInfo && gameMemory.WeaponMaxAmmo == 1 && gameMemory.WeaponMaxDurability > 10)
            {
                DrawProgressBar(ref statsXOffset, ref statsYOffset, gameMemory.WeaponDurability, gameMemory.WeaponMaxDurability);
            }

            if (config.ShowBossInfo && gameMemory.BossMaxHealth > 900)
            {
                DrawProgressBar(ref statsXOffset, ref statsYOffset, gameMemory.BossCurrentHealth, gameMemory.BossMaxHealth);
            }
        }
        private void DrawCampainInfo(ref float xOffset, ref float yOffset, uint gametime)
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

            SolidBrush TextColor = _white;

            float elementHeight = 38f;

            _graphics.FillRectangle(_darkerblue, xOffset, yOffset, xOffset + _elementSize, yOffset + elementHeight);

            // Draw text
            _graphics.DrawText(_consolasBold, _fontSize, TextColor, xOffset + _textXOffset, yOffset += _text1YOffset, string.Format("Day {0} - {1}:{2}:{3} {4}", (int)day, hours.ToString("D2"), minutes.ToString("D2"), seconds.ToString("D2"), suffix));

            yOffset += elementHeight - _text1YOffset;

            yOffset += config.BlockOffset;
        }

        private void DrawBlocInfo(ref float xOffset, ref float yOffset, Dictionary<string, string> parameters)
        {
            SolidBrush TextColor = _white;

            float elementHeight = (30f * parameters.Count) + 8f;

            _graphics.FillRectangle(_darkerblue, xOffset, yOffset, xOffset + _elementSize, yOffset + elementHeight);

            for (int i = 0; i < parameters.Count; i++)
            {
                if (i == 0)
                {
                    _graphics.DrawText(_consolasBold, _fontSize, TextColor, xOffset + _textXOffset, yOffset += _text1YOffset, string.Format("{0}: {1}", parameters.ElementAt(i).Key, parameters.ElementAt(i).Value));
                }
                else
                {
                    _graphics.DrawText(_consolasBold, _fontSize, TextColor, xOffset + _textXOffset, yOffset += _textYOffset, string.Format("{0}: {1}", parameters.ElementAt(i).Key, parameters.ElementAt(i).Value));
                }
            }

            yOffset += (elementHeight - _text1YOffset - (_textYOffset * (parameters.Count - 1)));

            yOffset += (config.BlockOffset);
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, float currentValue, float maxValue)
        {
            // Define steps for color changement
            float step1 = maxValue / 3 * 2;
            float step2 = maxValue / 3;

            float percentage = currentValue / maxValue;

            // Define colors according to steps
            SolidBrush BarColor = (currentValue > step1) ? _darkgreen : (currentValue > step2) ? _darkyellow : (currentValue <= step2) ? _darkred : _greydarker;
            SolidBrush TextColor = (currentValue > step1) ? _lightgreen : (currentValue > step2) ? _lightyellow : (currentValue < step2) ? _lightred : _white;

            float elementHeight = 38f;

            // Draw the rectangle
            _graphics.FillRectangle(_darkerblue, xOffset, yOffset, xOffset + _elementSize - 2, yOffset + elementHeight);
            _graphics.FillRectangle(BarColor, xOffset, yOffset, xOffset + ((_elementSize - 2) * percentage), yOffset + elementHeight);

            // Define text to display
            string currentValueInfo = float.IsNaN(currentValue) ? string.Empty : string.Format("{0}", (int)currentValue);
            string percentInfo = float.IsNaN(percentage) ? "0%" : string.Format("({0:P1})", percentage);
            float endOfBar = (xOffset + _elementSize) - _textXOffset - GetStringSize(percentInfo, _textXOffset);

            // Draw text
            _graphics.DrawText(_consolasBold, _fontSize, TextColor, xOffset + _textXOffset, yOffset + _text1YOffset, currentValueInfo);
            _graphics.DrawText(_consolasBold, _fontSize, TextColor, endOfBar, yOffset + _text1YOffset, percentInfo);

            yOffset += elementHeight - _text1YOffset;

            yOffset += config.BlockOffset;
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        #endregion Functions
    }

}
