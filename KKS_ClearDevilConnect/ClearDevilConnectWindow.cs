using RuntimeUnityEditor.Core;
using RuntimeUnityEditor.Core.Utils.Abstractions;
using UnityEngine;

namespace ClearDevilConnect
{
    public sealed class ClearDevilConnectWindow : Window<ClearDevilConnectWindow>
    {
        // Server configuration
        public string ServerAddress = "127.0.0.1";
        public string ServerPort = "39807";

        // Runtime status (written by plugin, read by window)
        public string CurrentStateName = "";
        public float CurrentSpeed;
        public string CurrentMode = "Idle";
        public string LastStatus = "Not connected";
        public string LastResponse = "";

        // UI state
        private Vector2 _scrollPos;
        private string _portInput;

        protected override void Initialize(InitSettings initSettings)
        {
            _portInput = ServerPort;
            Enabled = true;
        }

        protected override Rect GetDefaultWindowRect(Rect screenRect)
        {
            const int width = 320;
            const int height = 420;
            return new Rect(screenRect.xMax - width - 10, screenRect.yMax - height - 10, width, height);
        }

        protected override void DrawContents()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            {
                // ---- Connection Settings ----
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Server Configuration");

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Address:", GUILayout.Width(60));
                        ServerAddress = GUILayout.TextField(ServerAddress);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Port:", GUILayout.Width(60));
                        _portInput = GUILayout.TextField(_portInput);
                        if (GUILayout.Button("OK", GUILayout.Width(30)))
                        {
                            if (int.TryParse(_portInput, out var port) && port > 0 && port <= 65535)
                                ServerPort = _portInput;
                            else
                                ClearDevilConnectPlugin.Logger.LogMessage("Invalid port number");
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label($"Base URL: http://{ServerAddress}:{ServerPort}");
                }
                GUILayout.EndVertical();

                GUILayout.Space(4);

                // ---- Status Display ----
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("H Scene Status");

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("State:", GUILayout.Width(80));
                        GUILayout.Label(string.IsNullOrEmpty(CurrentStateName) ? "(none)" : CurrentStateName);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Speed:", GUILayout.Width(80));
                        GUILayout.Label($"{CurrentSpeed:F2} ({(int)(CurrentSpeed * 100)}%)");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Mode:", GUILayout.Width(80));
                        GUILayout.Label(CurrentMode);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.Space(4);

                // ---- HTTP Status ----
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Connection Status");

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Last:", GUILayout.Width(80));
                        GUILayout.Label(LastStatus);
                    }
                    GUILayout.EndHorizontal();

                    if (!string.IsNullOrEmpty(LastResponse))
                    {
                        GUILayout.Label("Response:");
                        GUILayout.TextArea(LastResponse);
                    }
                }
                GUILayout.EndVertical();

                GUILayout.Space(4);

                // ---- Manual Controls ----
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Manual Controls");

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Stop"))
                        {
                            SendManualCommand("stop");
                        }
                        if (GUILayout.Button("Manual A (10)"))
                        {
                            SendManualCommand("manualmode_A_10");
                        }
                        if (GUILayout.Button("Classic (1)"))
                        {
                            SendManualCommand("classicmode_1");
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Intensity slider for manual A mode
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Test Intensity A:", GUILayout.ExpandWidth(false));
                        var testIntensity = GUILayout.HorizontalSlider(_testIntensity, 0, 20);
                        if (Mathf.Abs(testIntensity - _testIntensity) > 0.1f)
                        {
                            _testIntensity = testIntensity;
                        }
                        GUILayout.Label(((int)_testIntensity).ToString(), GUILayout.Width(25));
                    }
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Send Test Intensity A"))
                    {
                        SendManualCommand($"manualmode_A_{(int)_testIntensity}");
                    }
                }
                GUILayout.EndVertical();

                GUILayout.Space(4);

                // ---- Mapping Info ----
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Mapping Rules");
                    GUILayout.Label("WLoop/SLoop -> /manualmode?type=A&intensity=[0-20]");
                    GUILayout.Label("  speedCalc (0-1) * 20 = intensity");
                    GUILayout.Label("OLoop -> /classicmode?type=1");
                    GUILayout.Label("Other -> /stop");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        private float _testIntensity = 10;

        private void SendManualCommand(string command)
        {
            ClearDevilConnectPlugin.SendManualCommand(command);
        }
    }
}
