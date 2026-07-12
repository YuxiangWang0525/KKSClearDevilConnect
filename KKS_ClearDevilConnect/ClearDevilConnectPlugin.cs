using System;
using System.IO;
using System.Net;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using RuntimeUnityEditor.Core;
using UnityEngine;

namespace ClearDevilConnect
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(RuntimeUnityEditorCore.GUID, RuntimeUnityEditorCore.Version)]
    public sealed class ClearDevilConnectPlugin : BaseUnityPlugin
    {
        public const string GUID = "ClearDevilConnect";
        public const string PluginName = "Clear Devil Connect";
        public const string Version = "1.0.0";

        internal static new ManualLogSource Logger;
        internal static ClearDevilConnectWindow ConnectWindow;

        private HFlag _hFlag;
        private string _lastSentCommand = "";
        private static int _requestCounter;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("ClearDevilConnect plugin loaded");
        }

        private void Start()
        {
            var editor = RuntimeUnityEditorCore.Instance;
            if (editor == null)
            {
                Logger.LogError("Failed to get RuntimeUnityEditor instance!");
                enabled = false;
                return;
            }

            ConnectWindow = new ClearDevilConnectWindow();
            editor.AddFeature(ConnectWindow);
        }

        private void Update()
        {
            if (ConnectWindow == null || !ConnectWindow.Enabled)
                return;

            _hFlag = FindObjectOfType<HFlag>();
            if (_hFlag == null)
            {
                if (!string.IsNullOrEmpty(_lastSentCommand) && _lastSentCommand != "stop")
                {
                    SendCommandAsync("stop");
                    _lastSentCommand = "stop";
                    ConnectWindow.CurrentMode = "Stopped (no H scene)";
                }
                return;
            }

            var stateName = _hFlag.nowAnimStateName ?? "";
            var speed = _hFlag.speedCalc;

            ConnectWindow.CurrentStateName = stateName;
            ConnectWindow.CurrentSpeed = speed;

            string command;
            string modeDisplay;

            if (stateName.Contains("WLoop") || stateName.Contains("SLoop"))
            {
                var intensity = (int)Math.Round(speed * 20.0);
                intensity = Mathf.Clamp(intensity, 0, 20);
                command = $"manualmode_A_{intensity}";
                modeDisplay = $"Rotation (A) | Intensity: {intensity}/20 | Speed: {speed:F2} | {stateName}";
            }
            else if (stateName.Contains("OLoop"))
            {
                command = "classicmode_1";
                modeDisplay = "Classic Mode (type=1)";
            }
            else
            {
                command = "stop";
                modeDisplay = $"Idle | State: {stateName}";
            }

            ConnectWindow.CurrentMode = modeDisplay;

            if (command != _lastSentCommand)
            {
                SendCommandAsync(command);
                _lastSentCommand = command;
            }
        }

        internal static void SendManualCommand(string command)
        {
            if (ConnectWindow != null)
                SendCommandAsyncInternal(command, ConnectWindow.ServerAddress, ConnectWindow.ServerPort);
        }

        private void SendCommandAsync(string command)
        {
            SendCommandAsyncInternal(command, ConnectWindow.ServerAddress, ConnectWindow.ServerPort);
        }

        private static void SendCommandAsyncInternal(string command, string serverAddress, string port)
        {
            var requestId = Interlocked.Increment(ref _requestCounter);

            if (ConnectWindow != null)
            {
                ConnectWindow.LastStatus = "Sending...";
                ConnectWindow.LastResponse = "";
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string url;
                    switch (command)
                    {
                        case "stop":
                            url = $"http://{serverAddress}:{port}/stop";
                            break;
                        case "classicmode_1":
                            url = $"http://{serverAddress}:{port}/classicmode?type=1";
                            break;
                        default:
                            if (command.StartsWith("manualmode_A_"))
                            {
                                var intensity = command.Substring("manualmode_A_".Length);
                                url = $"http://{serverAddress}:{port}/manualmode?type=A&intensity={intensity}";
                            }
                            else
                            {
                                Logger.LogError($"Unknown command: {command}");
                                return;
                            }
                            break;
                    }

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.Timeout = 3000;
                    request.ReadWriteTimeout = 3000;

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseBody = reader.ReadToEnd();
                        if (ConnectWindow != null)
                        {
                            ConnectWindow.LastStatus = $"OK ({response.StatusCode})";
                            ConnectWindow.LastResponse = responseBody;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (ConnectWindow != null)
                    {
                        ConnectWindow.LastStatus = $"Error: {e.Message}";
                        ConnectWindow.LastResponse = "";
                    }
                    Logger.LogWarning($"HTTP request failed (#{requestId}): {e.Message}");
                }
            });
        }
    }
}
