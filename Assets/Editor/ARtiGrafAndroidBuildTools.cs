using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class ARtiGrafAndroidBuildTools
{
    public sealed class AndroidDeviceInfo
    {
        public string Serial { get; }
        public string State { get; }

        public AndroidDeviceInfo(string serial, string state)
        {
            Serial = serial ?? string.Empty;
            State = state ?? string.Empty;
        }

        public override string ToString()
        {
            return Serial + " (" + State + ")";
        }
    }

    sealed class ProcessResult
    {
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public ProcessResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
        }
    }

    const string DefaultApkName = "TestBuild.apk";
    const string FallbackSdkRoot = "/opt/android-sdk";

    public static string OutputDirectoryPath => Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
    public static string OutputApkPath => Path.Combine(OutputDirectoryPath, DefaultApkName);
    public static string ApplicationIdentifier => PlayerSettings.applicationIdentifier;
    public static string ProductName => PlayerSettings.productName;

    public static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    public static BuildReport PerformBuild()
    {
        string[] enabledScenes = GetEnabledScenes();
        if (enabledScenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        Directory.CreateDirectory(OutputDirectoryPath);

        EnsureAndroidBuildTarget();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = OutputApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Android build failed with result: " + summary.result);
        }

        return report;
    }

    public static string InstallLastBuild()
    {
        return InstallApk(OutputApkPath);
    }

    public static string InstallApk(string apkPath)
    {
        if (string.IsNullOrWhiteSpace(apkPath) || !File.Exists(apkPath))
        {
            throw new FileNotFoundException("APK not found for install.", apkPath);
        }

        AndroidDeviceInfo device = GetSingleReadyDevice();
        return RunAdbCommand("-s " + device.Serial + " install -r " + Quote(apkPath));
    }

    public static string LaunchInstalledApp()
    {
        if (string.IsNullOrWhiteSpace(ApplicationIdentifier))
        {
            throw new InvalidOperationException("Application identifier is empty. Set PlayerSettings.applicationIdentifier first.");
        }

        AndroidDeviceInfo device = GetSingleReadyDevice();
        return RunAdbCommand(
            "-s " + device.Serial +
            " shell monkey -p " + ApplicationIdentifier +
            " -c android.intent.category.LAUNCHER 1"
        );
    }

    public static List<AndroidDeviceInfo> GetConnectedDevices()
    {
        if (!TryFindAdbPath(out string adbPath))
        {
            return new List<AndroidDeviceInfo>();
        }

        ProcessResult result = RunProcess(adbPath, "devices", false);
        List<AndroidDeviceInfo> devices = new List<AndroidDeviceInfo>();

        if (result.ExitCode != 0)
        {
            return devices;
        }

        string[] lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("List of devices attached"))
            {
                continue;
            }

            string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            devices.Add(new AndroidDeviceInfo(parts[0], parts[1]));
        }

        return devices;
    }

    public static bool TryFindAdbPath(out string adbPath)
    {
        string executableName = GetAdbExecutableName();
        string[] candidateRoots =
        {
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            FallbackSdkRoot
        };

        for (int i = 0; i < candidateRoots.Length; i++)
        {
            string root = candidateRoots[i];
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            string candidate = Path.Combine(root, "platform-tools", executableName);
            if (File.Exists(candidate))
            {
                adbPath = candidate;
                return true;
            }
        }

        ProcessResult fallback = RunProcess(executableName, "version", false);
        if (fallback.ExitCode == 0)
        {
            adbPath = executableName;
            return true;
        }

        adbPath = string.Empty;
        return false;
    }

    public static bool HasSingleReadyDevice(out string message)
    {
        List<AndroidDeviceInfo> readyDevices = GetConnectedDevices()
            .Where(device => string.Equals(device.State, "device", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (readyDevices.Count == 0)
        {
            message = "Tidak ada device Android siap pakai yang terdeteksi.";
            return false;
        }

        if (readyDevices.Count > 1)
        {
            message = "Terdeteksi lebih dari satu device: " + string.Join(", ", readyDevices.Select(device => device.Serial));
            return false;
        }

        message = "Device siap: " + readyDevices[0].Serial;
        return true;
    }

    static void EnsureAndroidBuildTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            return;
        }

        bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android,
            BuildTarget.Android
        );

        if (!switched)
        {
            throw new InvalidOperationException("Failed to switch active build target to Android.");
        }
    }

    static AndroidDeviceInfo GetSingleReadyDevice()
    {
        List<AndroidDeviceInfo> readyDevices = GetConnectedDevices()
            .Where(device => string.Equals(device.State, "device", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (readyDevices.Count == 0)
        {
            throw new InvalidOperationException("Tidak ada device Android siap pakai yang terdeteksi.");
        }

        if (readyDevices.Count > 1)
        {
            throw new InvalidOperationException(
                "Terdeteksi lebih dari satu device Android. Sisakan satu device dulu: " +
                string.Join(", ", readyDevices.Select(device => device.Serial))
            );
        }

        return readyDevices[0];
    }

    static string RunAdbCommand(string arguments)
    {
        if (!TryFindAdbPath(out string adbPath))
        {
            throw new InvalidOperationException("ADB tidak ditemukan. Pastikan Android SDK platform-tools tersedia.");
        }

        ProcessResult result = RunProcess(adbPath, arguments, true);
        return CombineOutput(result);
    }

    static ProcessResult RunProcess(string fileName, string arguments, bool throwOnError)
    {
        try
        {
            using Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            ProcessResult result = new ProcessResult(process.ExitCode, standardOutput, standardError);
            if (throwOnError && result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "Command failed: " + fileName + " " + arguments + Environment.NewLine + CombineOutput(result)
                );
            }

            return result;
        }
        catch (Exception ex) when (!throwOnError)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }
    }

    static string CombineOutput(ProcessResult result)
    {
        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            builder.Append(result.StandardOutput.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(result.StandardError.Trim());
        }

        return builder.ToString().Trim();
    }

    static string Quote(string value)
    {
        return "\"" + value + "\"";
    }

    static string GetAdbExecutableName()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT ? "adb.exe" : "adb";
    }
}
