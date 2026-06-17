using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Connects as a TCP client to the Python control server (default 127.0.0.1:8081).
    /// Receives 8-byte packets: [float steering, float acceleration] (little-endian).
    /// Threading: receive loop runs on a background thread; main thread reads via properties.
    /// </summary>
    public class ControlReceiver : MonoBehaviour
    {
        [Header("Python Control Server")]
        public string host = "127.0.0.1";
        public int port = 8081;
        public float reconnectDelay = 2f;

        // ── Public interface ───────────────────────────────────────────────
        /// <summary>Steering from Python: -1 (left) .. +1 (right)</summary>
        public float Steering    { get; private set; }
        /// <summary>Acceleration from Python: -1 (reverse) .. +1 (forward)</summary>
        public float Acceleration { get; private set; }
        /// <summary>True while a Python client is connected.</summary>
        public bool  IsConnected  { get; private set; }

        // ── Private state ──────────────────────────────────────────────────
        private TcpClient   _client;
        private NetworkStream _stream;
        private Thread       _receiveThread;
        private volatile bool _running;
        private readonly byte[] _buf = new byte[8];

        // Thread-safe raw values
        private volatile float _rawSteering    = 0f;
        private volatile float _rawAcceleration = 0f;

        // ── Unity lifecycle ────────────────────────────────────────────────
        private void OnEnable()
        {
            _running = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "ControlReceiver" };
            _receiveThread.Start();
        }

        private void OnDisable()
        {
            _running = false;
            _client?.Close();
        }

        private void Update()
        {
            // Copy thread-safe values onto main thread properties
            Steering     = _rawSteering;
            Acceleration = _rawAcceleration;
        }

        // ── Background thread ──────────────────────────────────────────────
        private void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    Debug.Log($"[ControlReceiver] Connecting to Python at {host}:{port}...");
                    _client = new TcpClient();
                    _client.Connect(host, port);
                    _stream = _client.GetStream();
                    IsConnected = true;
                    Debug.Log("[ControlReceiver] Connected.");

                    while (_running)
                    {
                        // Read exactly 8 bytes
                        int read = 0;
                        while (read < 8)
                        {
                            int n = _stream.Read(_buf, read, 8 - read);
                            if (n == 0) throw new Exception("Stream closed");
                            read += n;
                        }

                        float s = BitConverter.ToSingle(_buf, 0);
                        float a = BitConverter.ToSingle(_buf, 4);

                        // Clamp to valid range
                        _rawSteering     = Mathf.Clamp(s, -1f, 1f);
                        _rawAcceleration = Mathf.Clamp(a, -1f, 1f);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ControlReceiver] Disconnected: {ex.Message}");
                }
                finally
                {
                    IsConnected = false;
                    _rawSteering     = 0f;
                    _rawAcceleration = 0f;
                    _client?.Close();
                    _client = null;
                }

                if (_running)
                {
                    Debug.Log($"[ControlReceiver] Retrying in {reconnectDelay}s...");
                    Thread.Sleep((int)(reconnectDelay * 1000));
                }
            }
        }
    }
}
