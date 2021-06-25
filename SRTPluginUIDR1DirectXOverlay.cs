using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderDR1;
using SRTPluginProviderDR1.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SRTPluginUIRE2DirectXOverlay
{
    public class SRTPluginUIRE2DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderDR1";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryDR1 gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

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
        private Process GetProcess() => Process.GetProcessesByName("re8")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        private Dictionary<float, string> Bosses = new Dictionary<float, string>() {
            { 2900f, "Bella"},
            { 3400f, "Cassandra"},
            { 3700f, "Daniella"},
            { 9000f, "Lady D"},
            { 15000f, "Sturm"},
            { 25000f, "Urias"},
            { 26000f, "Moreau"},
            { 30000f, "Miranda"},
            { 100000f, "Heisenberg"},
        };

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
            _graphics?.Setup();

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont(config.StringFontName, 12, true);

            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 0);
            _lightgreen = _graphics?.CreateSolidBrush(0, 255, 0);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

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

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryDR1)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();
                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
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

        private void DrawOverlay()
        {
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            var Percent = gameMemory.PlayerCurrentHealth / gameMemory.PlayerMaxHealth;
            if (config.ShowHPBars)
            {
                DrawHealthBar(ref statsXOffset, ref statsYOffset, gameMemory.PlayerCurrentHealth, gameMemory.PlayerMaxHealth, Percent);
            }
            else
            {
                SolidBrush TextColor = (gameMemory.PlayerCurrentHealth > 600) ? _lightgreen : (gameMemory.PlayerCurrentHealth > 300) ? _lightyellow : (gameMemory.PlayerCurrentHealth > 0) ? _lightred : _white;
                string perc = float.IsNaN(Percent) ? "0%" : string.Format("{0:P1}", Percent);
                _graphics?.DrawText(_consolasBold, 20f, _red, statsXOffset, statsYOffset += 24, "Player HP");
                string playerName = gameMemory.PlayerStatus.IsEthan ? "Ethan: " : gameMemory.PlayerStatus.IsChris ? "Chris: " : "";
                _graphics?.DrawText(_consolasBold, 20f, TextColor, statsXOffset + 10f, statsYOffset += 24, string.Format("{0}{1} / {2} {3:P1}", playerName, gameMemory.PlayerCurrentHealth, gameMemory.PlayerMaxHealth, perc));
            }

            float textOffsetX = 0f;
            // Position Stats
            if (config.ShowPlayerPosition)
            {
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, config.StringPositionX);
                textOffsetX = config.PositionX + 15f + GetStringSize(config.StringPositionX) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.PlayerPositionX.ToString("F3"));
                textOffsetX += GetStringSize(gameMemory.PlayerPositionX.ToString("F3")) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, config.StringPositionY);
                textOffsetX += GetStringSize(config.StringPositionY) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.PlayerPositionY.ToString("F3"));
                textOffsetX += GetStringSize(gameMemory.PlayerPositionY.ToString("F3")) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, config.StringPositionZ);
                textOffsetX += GetStringSize(config.StringPositionZ) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.PlayerPositionZ.ToString("F3"));
            }

            // DA Stats
            if (config.ShowDifficultyAdjustment)
            {
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, config.ScoreString);
                textOffsetX = config.PositionX + 15f + GetStringSize(config.ScoreString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.RankScore.ToString()); //110f
                textOffsetX += GetStringSize(gameMemory.RankScore.ToString()) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, config.RankString); //178f
                textOffsetX += GetStringSize(config.RankString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.Rank.ToString()); //261f
                textOffsetX += GetStringSize(gameMemory.Rank.ToString()) + 10f;
            }
            if (config.ShowLeiCount)
            {
                if (!config.ShowDifficultyAdjustment)
                {
                    textOffsetX = config.PositionX + 15f;
                    _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset += 24, config.LeiString);
                }
                else
                {
                    _graphics?.DrawText(_consolasBold, 20f, _grey, textOffsetX, statsYOffset, config.LeiString);
                }

                textOffsetX += GetStringSize(config.LeiString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, gameMemory.Lei.ToString()); //57f
            }

            // EventActiomTask
            if (config.ShowCurrentEvent)
            {
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, config.EventString);
                var eventName = gameMemory.CurrentEvent;
                textOffsetX = config.PositionX + 15f + GetStringSize(config.EventString) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, eventName); //153f
            }

            if (config.Debug)
            {
                var title = "Event Type: ";
                var objValue = (EventType)gameMemory.EventType;
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, title);
                textOffsetX = config.PositionX + 15f + GetStringSize(title) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, objValue.ToString()); //153f

                var title2 = "Is Cutscene: ";
                var objValue2 = gameMemory.IsMotionPlay == 1 ? "True" : "False";
                _graphics?.DrawText(_consolasBold, 20f, _grey, config.PositionX + 15f, statsYOffset += 24, title2);
                textOffsetX = config.PositionX + 15f + GetStringSize(title2) + 10f;
                _graphics?.DrawText(_consolasBold, 20f, _lawngreen, textOffsetX, statsYOffset, objValue2); //153f
            }

            // Enemy HP
            var xOffset = config.EnemyHPPositionX == -1 ? statsXOffset : config.EnemyHPPositionX;
            var yOffset = config.EnemyHPPositionY == -1 ? statsYOffset : config.EnemyHPPositionY;
            _graphics?.DrawText(_consolasBold, 20f, _red, xOffset, yOffset += 24f, config.EnemyString);
            foreach (EnemyHP enemyHP in gameMemory.EnemyHealth.Where(a => a.IsAlive).OrderBy(a => a.IsTrigger).ThenBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP))
                if (config.ShowHPBars)
                {
                    DrawProgressBar(ref xOffset, ref yOffset, enemyHP.CurrentHP, enemyHP.MaximumHP, enemyHP.Percentage);
                }
                else
                {
                    if (Bosses.ContainsKey(enemyHP.MaximumHP))
                    {
                        _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset += 28f, string.Format("{0}: {1} / {2} {3:P1}", Bosses[enemyHP.MaximumHP].ToUpper(), enemyHP.CurrentHP, enemyHP.MaximumHP, enemyHP.Percentage));
                    }
                    else if (!config.ShowBossesOnly)
                    {
                        _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset += 28f, string.Format("{0} / {1} {2:P1}", enemyHP.CurrentHP, enemyHP.MaximumHP, enemyHP.Percentage));
                    }
                }
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            //float endOfBar = 420f - (perc.Length * 12);
            float endOfBar = config.PositionX + 420f - GetStringSize(perc);
            if (Bosses.ContainsKey(mhealth))
            {
                _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 420f, yOffset + 22f, 4f);
                _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 418f, yOffset + 20f);
                _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (418f * percentage), yOffset + 20f);
                _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset - 2f, string.Format("{0}: {1} / {2}", Bosses[mhealth].ToUpper(), chealth, mhealth));
                _graphics.DrawText(_consolasBold, 20f, _white, endOfBar, yOffset - 2f, perc);
            }
            else if (!config.ShowBossesOnly)
            {
                _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 420f, yOffset + 22f, 4f);
                _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 418f, yOffset + 20f);
                _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (418f * percentage), yOffset + 20f);
                _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset - 2f, string.Format("{0} / {1}", chealth, mhealth));
                _graphics.DrawText(_consolasBold, 20f, _white, endOfBar, yOffset - 2f, perc);
            }

        }

        private void DrawHealthBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            SolidBrush HPBarColor = (chealth > 600) ? _darkgreen : (chealth > 300) ? _darkyellow : (chealth > 0) ? _darkred : _greydarker;
            SolidBrush TextColor = (chealth > 600) ? _lightgreen : (chealth > 300) ? _lightyellow : (chealth > 0) ? _lightred : _white;
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 420f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 418f, yOffset + 20f);
            _graphics.FillRectangle(HPBarColor, xOffset + 1f, yOffset + 1f, xOffset + (418f * percentage), yOffset + 20f);
            string playerName = gameMemory.PlayerStatus.IsEthan ? "Ethan: " : gameMemory.PlayerStatus.IsChris ? "Chris: " : "";
            _graphics.DrawText(_consolasBold, 20f, TextColor, xOffset + 10f, yOffset - 2f, string.Format("{0}{1} / {2}", playerName, chealth, mhealth));
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            //float endOfBar = 420f - (perc.Length * 12);
            float endOfBar = config.PositionX + 420f - GetStringSize(perc);
            _graphics.DrawText(_consolasBold, 20f, TextColor, endOfBar, yOffset - 2f, perc);
        }

        public enum EventType : int
        {
            None = 0,
            SkipCutscene = 1,
            Cutscene = 2,
            Interactable = 3,
        }
    }

}
