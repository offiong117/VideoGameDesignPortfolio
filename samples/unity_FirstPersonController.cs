// FirstPersonController.cs
// A compact first-person controller written while learning Unity.
// Drop this on a GameObject with a CharacterController and a child Camera.
//
// What this does:
//   - WASD walk with smooth acceleration/deceleration
//   - mouse-look with pitch clamping
//   - jump and gravity using CharacterController.Move
//   - a tiny head-bob so movement feels less floaty
//
// I kept it under ~120 lines on purpose so the systems stay legible while
// I'm still learning Unity's component model.

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Move")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.0f;
    public float acceleration = 12f;
    public float jumpHeight = 1.4f;
    public float gravity = -18f;

    [Header("Look")]
    public Transform cameraPivot;
    public float mouseSensitivity = 120f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Head-bob")]
    public float bobFrequency = 8f;
    public float bobAmplitude = 0.04f;

    CharacterController _cc;
    Vector3 _velocity;       // y is vertical velocity, xz tracked separately
    Vector2 _horizontalVel;
    float _pitch;
    float _bobTime;
    Vector3 _cameraStartLocalPos;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
        if (cameraPivot != null)
            _cameraStartLocalPos = cameraPivot.localPosition;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
        HandleHeadBob();
    }

    void HandleLook()
    {
        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(0f, mx, 0f);
        _pitch = Mathf.Clamp(_pitch - my, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void HandleMove()
    {
        // input -> desired local-space velocity
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector2 target = input * targetSpeed;

        // smooth horizontal acceleration in world space
        Vector3 worldTarget = transform.TransformDirection(new Vector3(target.x, 0f, target.y));
        Vector2 worldTarget2D = new Vector2(worldTarget.x, worldTarget.z);
        _horizontalVel = Vector2.MoveTowards(_horizontalVel, worldTarget2D, acceleration * Time.deltaTime);

        // gravity / jump
        if (_cc.isGrounded)
        {
            if (_velocity.y < 0f) _velocity.y = -2f;
            if (Input.GetButtonDown("Jump"))
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        _velocity.y += gravity * Time.deltaTime;

        Vector3 motion = new Vector3(_horizontalVel.x, _velocity.y, _horizontalVel.y);
        _cc.Move(motion * Time.deltaTime);
    }

    void HandleHeadBob()
    {
        if (cameraPivot == null) return;
        float speed = new Vector2(_horizontalVel.x, _horizontalVel.y).magnitude;
        if (_cc.isGrounded && speed > 0.1f)
        {
            _bobTime += Time.deltaTime * bobFrequency * (speed / walkSpeed);
            float offset = Mathf.Sin(_bobTime) * bobAmplitude;
            cameraPivot.localPosition = _cameraStartLocalPos + new Vector3(0f, offset, 0f);
        }
        else
        {
            // ease back to rest
            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition, _cameraStartLocalPos, Time.deltaTime * 6f);
        }
    }
}
