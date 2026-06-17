using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Streams a Unity Camera's render output as JPEG frames over TCP.
    /// Python client connects and receives: [4-byte little-endian length][JPEG bytes]
    /// Attach two instances: one for the front camera (port 8080), one for the back camera (port 8082).
    /// </summary>
    public class CameraStreamer : MonoBehaviour
    {
        [Header("TCP Server")]
        public int port = 8080;

        [Header("Camera")]
        public Camera sourceCamera;
        public int captureWidth  = 320;
        public int captureHeight = 240;
        [Range(1, 100)]
        public int jpegQuality = 75;

        [Header("Corruption Effect")]
        public bool corruptedCamera = false; // set by YellowEffect

        // ── Private state ──────────────────────────────────────────────────
        private TcpListener  _listener;
        private TcpClient    _client;
        private NetworkStream _stream;
        private Thread        _acceptThread;
        private volatile bool _running;
        private volatile bool _clientConnected;

        private RenderTexture _rt;
        private Texture2D     _tex;

        // ── Unity lifecycle ────────────────────────────────────────────────
        private void OnEnable()
        {
            _rt  = new RenderTexture(captureWidth, captureHeight, 24);
            _tex = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

            _running = true;
            _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            _listener.Start();

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = $"CamStream_{port}" };
            _acceptThread.Start();

            Debug.Log($"[CameraStreamer] Listening on port {port}");
        }

        private void OnDisable()
        {
            _running = false;
            _client?.Close();
            _listener?.Stop();
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_tex != null) Destroy(_tex);
        }

        private void LateUpdate()
        {
            if (!_clientConnected || _client == null) return;

            try
            {
                // Render camera to texture
                RenderTexture prev = sourceCamera.targetTexture;
                sourceCamera.targetTexture = _rt;
                sourceCamera.Render();
                sourceCamera.targetTexture = prev;

                RenderTexture.active = _rt;
                _tex.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);

                // Apply corruption: black out top 50% of the frame
                if (corruptedCamera)
                {
                    int halfHeight = captureHeight / 2;
                    Color[] black = new Color[captureWidth * halfHeight];
                    for (int i = 0; i < black.Length; i++) black[i] = Color.black;
                    _tex.SetPixels(0, halfHeight, captureWidth, halfHeight, black);
                }

                _tex.Apply();
                RenderTexture.active = null;

                byte[] jpg = _tex.EncodeToJPG(jpegQuality);

                // Send length-prefixed JPEG
                byte[] lenBytes = BitConverter.GetBytes(jpg.Length); // 4 bytes, little-endian
                _stream.Write(lenBytes, 0, 4);
                _stream.Write(jpg, 0, jpg.Length);
                _stream.Flush();
            }
            catch (Exception)
            {
                // Client disconnected during send
                _clientConnected = false;
                _client?.Close();
                _client = null;
            }
        }

        // ── Background thread ──────────────────────────────────────────────
        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    Debug.Log($"[CameraStreamer:{port}] Waiting for Python client...");
                    _client = _listener.AcceptTcpClient();
                    _stream = _client.GetStream();
                    _clientConnected = true;
                    Debug.Log($"[CameraStreamer:{port}] Client connected.");

                    // Block until client disconnects
                    while (_running && _clientConnected)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (SocketException)
                {
                    // Listener stopped (OnDisable)
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CameraStreamer:{port}] {ex.Message}");
                }
                finally
                {
                    _clientConnected = false;
                    _client?.Close();
                    _client = null;
                }
            }
        }
    }
}
