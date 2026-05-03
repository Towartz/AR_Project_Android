using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;

namespace ARtiGraf.AR
{
    public sealed class LinuxEditorDirectCameraFeed : IDisposable
    {
        readonly string ffmpegPath;
        readonly string devicePath;
        readonly string inputFormat;
        readonly int width;
        readonly int height;
        readonly int frameRate;
        readonly int frameSizeBytes;
        readonly object frameLock = new object();

        Process process;
        Thread readThread;
        Thread stderrThread;
        byte[] latestFrame;
        bool hasNewFrame;
        volatile bool stopRequested;
        string lastError;
        bool hasAnyFrame;

        public LinuxEditorDirectCameraFeed(
            string ffmpegPath,
            string devicePath,
            string inputFormat,
            int width,
            int height,
            int frameRate)
        {
            this.ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "/usr/bin/ffmpeg" : ffmpegPath;
            this.devicePath = string.IsNullOrWhiteSpace(devicePath) ? "/dev/video0" : devicePath;
            this.inputFormat = string.IsNullOrWhiteSpace(inputFormat) ? "mjpeg" : inputFormat;
            this.width = Mathf.Max(16, width);
            this.height = Mathf.Max(16, height);
            this.frameRate = Mathf.Max(1, frameRate);
            frameSizeBytes = this.width * this.height * 4;
        }

        public bool HasAnyFrame => hasAnyFrame;
        public string LastError => lastError;
        public string DevicePath => devicePath;
        public int Width => width;
        public int Height => height;

        public bool Start(out string error)
        {
            error = null;
            if (process != null)
            {
                return true;
            }

            if (!File.Exists(ffmpegPath))
            {
                error = "ffmpeg tidak ditemukan di " + ffmpegPath;
                lastError = error;
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments =
                    "-hide_banner -loglevel error " +
                    "-fflags nobuffer -flags low_delay " +
                    "-f video4linux2 " +
                    "-input_format " + inputFormat + " " +
                    "-video_size " + width + "x" + height + " " +
                    "-framerate " + frameRate + " " +
                    "-i " + devicePath + " " +
                    "-vf format=rgba " +
                    "-f rawvideo -pix_fmt rgba -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };
                stopRequested = false;
                process.Start();

                readThread = new Thread(ReadLoop)
                {
                    IsBackground = true,
                    Name = "ARtiGrafDirectCameraRead"
                };
                stderrThread = new Thread(ReadStderrLoop)
                {
                    IsBackground = true,
                    Name = "ARtiGrafDirectCameraErr"
                };

                readThread.Start();
                stderrThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                lastError = ex.ToString();
                Dispose();
                return false;
            }
        }

        public bool TryUpdateTexture(ref Texture2D texture)
        {
            byte[] frame = null;
            lock (frameLock)
            {
                if (!hasNewFrame || latestFrame == null)
                {
                    return false;
                }

                frame = latestFrame;
                latestFrame = null;
                hasNewFrame = false;
            }

            if (texture == null || texture.width != width || texture.height != height)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            texture.LoadRawTextureData(frame);
            texture.Apply(false, false);
            return true;
        }

        void ReadLoop()
        {
            byte[] frameBuffer = new byte[frameSizeBytes];

            try
            {
                Stream stream = process?.StandardOutput?.BaseStream;
                if (stream == null)
                {
                    lastError = "stdout ffmpeg tidak tersedia.";
                    return;
                }

                while (!stopRequested)
                {
                    int bytesRead = ReadExact(stream, frameBuffer, frameSizeBytes);
                    if (bytesRead != frameSizeBytes)
                    {
                        if (!stopRequested && string.IsNullOrWhiteSpace(lastError))
                        {
                            lastError = "Stream ffmpeg berhenti sebelum satu frame penuh diterima.";
                        }

                        return;
                    }

                    var frameCopy = new byte[frameSizeBytes];
                    Buffer.BlockCopy(frameBuffer, 0, frameCopy, 0, frameSizeBytes);

                    lock (frameLock)
                    {
                        latestFrame = frameCopy;
                        hasNewFrame = true;
                    }

                    hasAnyFrame = true;
                }
            }
            catch (Exception ex)
            {
                if (!stopRequested)
                {
                    lastError = ex.ToString();
                }
            }
        }

        void ReadStderrLoop()
        {
            try
            {
                string errorText = process?.StandardError?.ReadToEnd();
                if (!stopRequested && !string.IsNullOrWhiteSpace(errorText))
                {
                    lastError = errorText.Trim();
                }
            }
            catch (Exception ex)
            {
                if (!stopRequested && string.IsNullOrWhiteSpace(lastError))
                {
                    lastError = ex.Message;
                }
            }
        }

        static int ReadExact(Stream stream, byte[] buffer, int targetLength)
        {
            int totalRead = 0;
            while (totalRead < targetLength)
            {
                int read = stream.Read(buffer, totalRead, targetLength - totalRead);
                if (read <= 0)
                {
                    break;
                }

                totalRead += read;
            }

            return totalRead;
        }

        public void Dispose()
        {
            stopRequested = true;

            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }

            JoinThread(readThread);
            JoinThread(stderrThread);

            readThread = null;
            stderrThread = null;

            if (process != null)
            {
                process.Dispose();
                process = null;
            }

            lock (frameLock)
            {
                latestFrame = null;
                hasNewFrame = false;
            }

            hasAnyFrame = false;
        }

        static void JoinThread(Thread thread)
        {
            if (thread == null)
            {
                return;
            }

            try
            {
                if (thread.IsAlive)
                {
                    thread.Join(300);
                }
            }
            catch
            {
            }
        }
    }
}
