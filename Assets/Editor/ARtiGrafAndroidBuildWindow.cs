using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class ARtiGrafAndroidBuildWindow : EditorWindow
{
    readonly List<ARtiGrafAndroidBuildTools.AndroidDeviceInfo> devices = new List<ARtiGrafAndroidBuildTools.AndroidDeviceInfo>();

    Vector2 logScrollPosition;
    string logText = string.Empty;
    string adbPath = string.Empty;
    string deviceStatus = string.Empty;
    DateTime lastRefreshTimestamp;

    [MenuItem("Tools/BuhenAR/Build/Open Android Build Window")]
    public static void Open()
    {
        ARtiGrafAndroidBuildWindow window = GetWindow<ARtiGrafAndroidBuildWindow>("BuhenAR Android");
        window.minSize = new Vector2(760f, 520f);
        window.RefreshState();
    }

    void OnEnable()
    {
        RefreshState();
    }

    void OnFocus()
    {
        RefreshState();
    }

    void OnGUI()
    {
        DrawToolbar();
        DrawStatus();
        DrawDeviceSection();
        DrawActionButtons();
        DrawLogSection();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            RefreshState();
        }

        if (GUILayout.Button("Open Dashboard", EditorStyles.toolbarButton, GUILayout.Width(104f)))
        {
            ARtiGrafContentDashboardWindow.Open();
        }

        if (GUILayout.Button("Reveal APK", EditorStyles.toolbarButton, GUILayout.Width(82f)))
        {
            RevealApk();
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("Updated: " + FormatLastRefreshTime(), EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    void DrawStatus()
    {
        bool hasApk = File.Exists(ARtiGrafAndroidBuildTools.OutputApkPath);
        string apkInfo = hasApk
            ? FormatFileInfo(ARtiGrafAndroidBuildTools.OutputApkPath)
            : "APK belum ada. Jalankan build dulu.";

        string message =
            "Package: " + ARtiGrafAndroidBuildTools.ApplicationIdentifier + Environment.NewLine +
            "Product: " + ARtiGrafAndroidBuildTools.ProductName + Environment.NewLine +
            "ADB: " + (string.IsNullOrWhiteSpace(adbPath) ? "Tidak ditemukan" : adbPath) + Environment.NewLine +
            "APK: " + apkInfo + Environment.NewLine +
            "Output: " + ARtiGrafAndroidBuildTools.OutputApkPath + Environment.NewLine +
            "Device: " + deviceStatus;

        MessageType messageType = hasApk ? MessageType.Info : MessageType.Warning;
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            messageType = MessageType.Error;
        }

        EditorGUILayout.HelpBox(message, messageType);
    }

    void DrawDeviceSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Detected Devices", EditorStyles.boldLabel);

        if (devices.Count == 0)
        {
            EditorGUILayout.LabelField("Tidak ada device yang terbaca.");
        }
        else
        {
            for (int i = 0; i < devices.Count; i++)
            {
                ARtiGrafAndroidBuildTools.AndroidDeviceInfo device = devices[i];
                EditorGUILayout.LabelField("- " + device.Serial + " [" + device.State + "]");
            }
        }

        EditorGUILayout.EndVertical();
    }

    void DrawActionButtons()
    {
        bool hasApk = File.Exists(ARtiGrafAndroidBuildTools.OutputApkPath);
        bool hasAdb = !string.IsNullOrWhiteSpace(adbPath);
        bool hasSingleReadyDevice = CountReadyDevices() == 1;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build APK", GUILayout.Height(38f)))
        {
            ExecuteAction("Build APK", BuildApkOnly);
        }

        GUI.enabled = hasAdb && hasSingleReadyDevice;
        if (GUILayout.Button("Build + Install", GUILayout.Height(38f)))
        {
            ExecuteAction("Build + Install", BuildAndInstall);
        }

        if (GUILayout.Button("Build + Install + Launch", GUILayout.Height(38f)))
        {
            ExecuteAction("Build + Install + Launch", BuildInstallAndLaunch);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = hasApk && hasAdb && hasSingleReadyDevice;
        if (GUILayout.Button("Install Last APK", GUILayout.Height(32f)))
        {
            ExecuteAction("Install Last APK", InstallLastApk);
        }

        if (GUILayout.Button("Launch Installed App", GUILayout.Height(32f)))
        {
            ExecuteAction("Launch Installed App", LaunchInstalledApp);
        }

        GUI.enabled = true;
        if (GUILayout.Button("Clear Log", GUILayout.Height(32f)))
        {
            logText = string.Empty;
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawLogSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.MinHeight(180f));
        EditorGUILayout.TextArea(string.IsNullOrEmpty(logText) ? "Belum ada aktivitas." : logText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void BuildApkOnly()
    {
        BuildReport report = ARtiGrafAndroidBuildTools.PerformBuild();
        BuildSummary summary = report.summary;

        AppendLog("Build result: " + summary.result);
        AppendLog("Output path: " + ARtiGrafAndroidBuildTools.OutputApkPath);
        AppendLog("Warnings: " + summary.totalWarnings + " | Errors: " + summary.totalErrors);
        AppendLog("Size: " + FormatBuildSize(summary.totalSize) + " | Time: " + summary.totalTime);
    }

    void BuildAndInstall()
    {
        BuildApkOnly();
        InstallLastApk();
    }

    void BuildInstallAndLaunch()
    {
        BuildApkOnly();
        InstallLastApk();
        LaunchInstalledApp();
    }

    void InstallLastApk()
    {
        string output = ARtiGrafAndroidBuildTools.InstallLastBuild();
        AppendLog("Install output:");
        AppendMultilineLog(output);
    }

    void LaunchInstalledApp()
    {
        string output = ARtiGrafAndroidBuildTools.LaunchInstalledApp();
        AppendLog("Launch output:");
        AppendMultilineLog(output);
    }

    void ExecuteAction(string label, Action action)
    {
        try
        {
            AppendLog("== " + label + " ==");
            EditorUtility.DisplayProgressBar("BuhenAR Android Build", label + "...", 0.5f);
            action();
            AppendLog(label + " selesai.");
        }
        catch (Exception ex)
        {
            AppendLog(label + " gagal: " + ex.Message);
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("BuhenAR Android Build", ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            RefreshState();
        }
    }

    void RefreshState()
    {
        devices.Clear();
        devices.AddRange(ARtiGrafAndroidBuildTools.GetConnectedDevices());

        if (!ARtiGrafAndroidBuildTools.TryFindAdbPath(out adbPath))
        {
            adbPath = string.Empty;
        }

        int readyDeviceCount = CountReadyDevices();
        if (readyDeviceCount == 1)
        {
            deviceStatus = "Device siap: " + devices.Find(device => device.State == "device").Serial;
        }
        else if (readyDeviceCount > 1)
        {
            deviceStatus = "Terdeteksi lebih dari satu device: " + string.Join(", ", devices.FindAll(device => device.State == "device"));
        }
        else if (devices.Count > 0)
        {
            deviceStatus = string.Join(", ", devices);
        }
        else
        {
            deviceStatus = "Tidak ada device Android siap pakai yang terdeteksi.";
        }

        lastRefreshTimestamp = DateTime.Now;
        Repaint();
    }

    void RevealApk()
    {
        if (File.Exists(ARtiGrafAndroidBuildTools.OutputApkPath))
        {
            EditorUtility.RevealInFinder(ARtiGrafAndroidBuildTools.OutputApkPath);
            return;
        }

        EditorUtility.DisplayDialog("BuhenAR Android Build", "APK belum ada. Jalankan build dulu.", "OK");
    }

    void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        logText += "[" + timestamp + "] " + message.Trim() + Environment.NewLine;
        logScrollPosition.y = float.MaxValue;
    }

    void AppendMultilineLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            AppendLog("(tanpa output tambahan)");
            return;
        }

        string[] lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            AppendLog(lines[i]);
        }
    }

    string FormatLastRefreshTime()
    {
        if (lastRefreshTimestamp == default)
        {
            return "never";
        }

        return lastRefreshTimestamp.ToString("HH:mm:ss");
    }

    static string FormatFileInfo(string path)
    {
        FileInfo info = new FileInfo(path);
        return EditorUtility.FormatBytes(info.Length) + " | " + info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    static string FormatBuildSize(ulong totalSize)
    {
        long safeSize = totalSize > long.MaxValue ? long.MaxValue : (long)totalSize;
        return EditorUtility.FormatBytes(safeSize);
    }

    int CountReadyDevices()
    {
        int count = 0;

        for (int i = 0; i < devices.Count; i++)
        {
            if (string.Equals(devices[i].State, "device", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }
}
