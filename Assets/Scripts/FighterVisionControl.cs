using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class FighterVisionControl : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public float stepDuration = 2f; // time in each step

    [Header("Python Networking")]
    public int port = 5005;
    private Thread receiveThread;
    private UdpClient client;
    private string lastPythonEvent = "";
    private float pythonInputTimer = 0f;

    [Header("References")]
    public Transform opponent;

    private Rigidbody2D rb;
    private FighterCombat combat;

    private float moveInput;
    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<FighterCombat>();
        originalScale = transform.localScale;

        // Inicializar hilos para UDP
        InitUDP();
    }

    private void InitUDP()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                
                // Save the event
                lock (this) { lastPythonEvent = text; }
            }
            catch (Exception e) { Debug.Log("UDP Error: " + e.Message); }
        }
    }

    void Update()
    {
        HandleMovementInput();
        FaceOpponent();
    }

    void FixedUpdate()
    {
        Move();
    }

    void HandleMovementInput()
    {
        moveInput = 0f;

        if (combat != null && (combat.IsDead || combat.MatchEnded))
            return;

        lock (this)
        {
            if (lastPythonEvent != "")
            {
                if (lastPythonEvent == "STEP_FORWARD") moveInput = 1f;
                if (lastPythonEvent == "STEP_BACKWARD") moveInput = -1f;
                
                pythonInputTimer = stepDuration; 
                lastPythonEvent = ""; // clean the event
            }
        }

        // If the timer is active, we maintain the movement
        if (pythonInputTimer > 0)
        {
            pythonInputTimer -= Time.deltaTime;
        }
        else
        {
            // Keyboard operation
            if (gameObject.name == "Player1")
            {
                if (Input.GetKey(KeyCode.A)) moveInput = -1f;
                if (Input.GetKey(KeyCode.D)) moveInput = 1f;
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;
                if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
            }
        }
    }

    void Move()
    {

        if (combat == null || combat.IsDead || combat.MatchEnded)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Motion lock
        if (combat.currentState == FighterCombat.FighterState.AttackHigh ||
            combat.currentState == FighterCombat.FighterState.AttackLow ||
            combat.currentState == FighterCombat.FighterState.BlockHigh ||
            combat.currentState == FighterCombat.FighterState.BlockLow)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (combat.currentState == FighterCombat.FighterState.Hitstun ||
            combat.currentState == FighterCombat.FighterState.Clash)
        {
            return;
        }

        // Use of Stamina 
        if (Mathf.Abs(moveInput) > 0f)
        {
            bool hasStamina = combat.TryConsumeMovementStamina(Time.fixedDeltaTime);
            if (!hasStamina)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void FaceOpponent()
    {
        if (opponent == null) return;
        if (combat != null && (combat.IsDead || combat.MatchEnded)) return;

        if (transform.position.x < opponent.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    void OnApplicationQuit()
    {
        if (client != null) client.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
}