// TrainRunner.cs
// Core controller for an on-rails train obstacle runner (Unity, PC + iPad).
//
// The train moves forward at a controlled speed on a 3-lane track. The
// player switches lanes to dodge obstacles and can brake/accelerate. The
// same script handles WASD/arrow keys on desktop and lane-swipes on iPad.
//
// Drop on the train GameObject; assign laneXPositions and the obstacle
// pool. The script is intentionally framework-light so the mechanic stays
// inspectable.

using UnityEngine;

public class TrainRunner : MonoBehaviour
{
    [Header("Track")]
    public float[] laneXPositions = { -2f, 0f, 2f };
    public int startLane = 1;
    public float laneSwitchSpeed = 8f;     // units / second of lateral travel

    [Header("Speed")]
    public float baseForwardSpeed = 12f;
    public float boostMultiplier = 1.6f;
    public float brakeMultiplier = 0.55f;
    public float acceleration = 6f;        // ramp-up from base to target

    [Header("Crash")]
    public float collisionRadius = 0.9f;
    public LayerMask obstacleMask;

    [Header("Touch")]
    public float swipeThreshold = 60f;     // px

    int _lane;
    float _currentSpeed;
    bool _crashed;

    // touch tracking
    Vector2 _touchStart;
    bool _tracking;

    void Awake()
    {
        _lane = Mathf.Clamp(startLane, 0, laneXPositions.Length - 1);
        _currentSpeed = baseForwardSpeed;
        var p = transform.position;
        p.x = laneXPositions[_lane];
        transform.position = p;
    }

    void Update()
    {
        if (_crashed) return;

        // --- input ---
        int laneDelta = 0;
        float speedTarget = baseForwardSpeed;

        // keyboard
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))  laneDelta = -1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) laneDelta = +1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))   speedTarget = baseForwardSpeed * boostMultiplier;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) speedTarget = baseForwardSpeed * brakeMultiplier;

        // touch (iPad)
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { _touchStart = t.position; _tracking = true; }
            else if (_tracking && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Ended))
            {
                Vector2 delta = t.position - _touchStart;
                if (Mathf.Abs(delta.x) > swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    laneDelta = delta.x > 0 ? +1 : -1;
                    _tracking = false;
                }
                else if (Mathf.Abs(delta.y) > swipeThreshold)
                {
                    speedTarget = delta.y > 0
                        ? baseForwardSpeed * boostMultiplier
                        : baseForwardSpeed * brakeMultiplier;
                    if (t.phase == TouchPhase.Ended) _tracking = false;
                }
            }
            if (t.phase == TouchPhase.Ended) _tracking = false;
        }

        if (laneDelta != 0)
            _lane = Mathf.Clamp(_lane + laneDelta, 0, laneXPositions.Length - 1);

        // --- motion ---
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, speedTarget, acceleration * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.z += _currentSpeed * Time.deltaTime;
        pos.x = Mathf.MoveTowards(pos.x, laneXPositions[_lane], laneSwitchSpeed * Time.deltaTime);
        transform.position = pos;

        // --- collision ---
        if (Physics.CheckSphere(transform.position, collisionRadius, obstacleMask))
            Crash();
    }

    void Crash()
    {
        _crashed = true;
        _currentSpeed = 0f;
        // notify the score/UI system — left abstract so the script stays small
        SendMessageUpwards("OnTrainCrashed", SendMessageOptions.DontRequireReceiver);
    }

    public float CurrentSpeed => _currentSpeed;
    public bool IsCrashed => _crashed;
}
