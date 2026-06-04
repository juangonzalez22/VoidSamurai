// Bit-flag protocol (1 byte, matches vision_main.py):
//   Bit 0  (0x01)  MoveForward
//   Bit 1  (0x02)  MoveBackward
//   Bit 2  (0x04)  AttackRight
//   Bit 3  (0x08)  AttackLeft

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // =========================================================================
    // Inspector
    // =========================================================================

    [Header("Network")]
    [Tooltip("UDP port that Python sends packets to.")]
    public int port = 5005;

    [Header("Debug")]
    [Tooltip("Show incoming flag byte in the console.")]
    public bool logIncomingFlags = true;

    // =========================================================================
    // Bit-flag constants
    // =========================================================================

    private const byte FLAG_MOVE_FORWARD  = 0x01;
    private const byte FLAG_MOVE_BACKWARD = 0x02;
    private const byte FLAG_ATTACK_RIGHT  = 0x04;
    private const byte FLAG_ATTACK_LEFT   = 0x08;

    // =========================================================================
    // Public API  –  read by VisionController and VisionFighter
    // =========================================================================
    public bool MoveForward  { get; private set; }

    public bool MoveBackward { get; private set; }

    public bool AttackRight  { get; private set; }

    public bool AttackLeft   { get; private set; }

    public byte LastRawFlags { get; private set; }

    public void ConsumeAttackRight() => AttackRight = false;
    public void ConsumeAttackLeft()  => AttackLeft  = false;

    // =========================================================================
    // Private networking state
    // =========================================================================

    private UdpClient _client;
    private Thread    _receiveThread;

    private volatile int _pendingMovement;

    private int  _pendingAttacks;
    private readonly object _flagLock = new object();

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    private void Start()
    {
        _pendingMovement = 0;
        _pendingAttacks  = 0;
        StartReceiveThread();
    }

    private void Update()
    {
        int mov = Interlocked.Exchange(ref _pendingMovement, 0);
        MoveForward  = (mov & FLAG_MOVE_FORWARD)  != 0;
        MoveBackward = (mov & FLAG_MOVE_BACKWARD) != 0;

        int atk;
        lock (_flagLock) { atk = _pendingAttacks; }

        if ((atk & FLAG_ATTACK_RIGHT) != 0) AttackRight = true;
        if ((atk & FLAG_ATTACK_LEFT)  != 0) AttackLeft  = true;

        lock (_flagLock) { _pendingAttacks = 0; }

        HandleMouseInput();

        if (logIncomingFlags && (MoveForward || MoveBackward || AttackRight || AttackLeft))
        {
            Debug.Log($"[InputManager] MF={MoveForward} MB={MoveBackward} " +
                      $"AR={AttackRight} AL={AttackLeft} raw=0x{LastRawFlags:X2}");
        }
    }

    private void OnApplicationQuit() => StopReceiveThread();
    private void OnDestroy()         => StopReceiveThread();

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) AttackRight = true;
        if (Input.GetMouseButtonDown(1)) AttackLeft  = true;
    }

    // =========================================================================
    // UDP networking
    // =========================================================================

    private void StartReceiveThread()
    {
        Debug.Log($"Opening UDP port {port} on {gameObject.name}");
        _client = new UdpClient(port);
        _client.Client.ReceiveTimeout = 500; 

        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name         = "VisionUDPReceiver"
        };
        _receiveThread.Start();
    }

    private void StopReceiveThread()
    {
        try { _client?.Close(); } catch { /* ignore */ }

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join(1000);
            _receiveThread = null;
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                byte[] data = _client.Receive(ref remoteEP);
                if (data == null || data.Length == 0) continue;

                byte incoming = data[0];
                LastRawFlags  = incoming;

                if ((incoming & (FLAG_MOVE_FORWARD | FLAG_MOVE_BACKWARD)) != 0)
                {
                    int prev, next;
                    do
                    {
                        prev = _pendingMovement;
                        next = prev | incoming;
                    } while (Interlocked.CompareExchange(ref _pendingMovement, next, prev) != prev);
                }

                if ((incoming & (FLAG_ATTACK_RIGHT | FLAG_ATTACK_LEFT)) != 0)
                {
                    lock (_flagLock) { _pendingAttacks |= incoming; }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut) continue;
                break; 
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InputManager] UDP error: {ex.Message}");
            }
        }
    }
}