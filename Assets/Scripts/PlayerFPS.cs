using UnityEngine;
using UnityEngine.InputSystem;

/// Управление от первого лица: WASD/стрелки + мышь, Shift — бег (Input System).
[RequireComponent(typeof(CharacterController))]
public class PlayerFPS : MonoBehaviour
{
    public float WalkSpeed = 4.2f;
    public float RunSpeed = 7.5f;
    public float Sensitivity = 0.12f;

    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }

    CharacterController _cc;
    Transform _cam;
    float _pitch;
    float _vy;

    public void Init(Transform cameraTransform) { _cam = cameraTransform; }

    void Awake() { _cc = GetComponent<CharacterController>(); }

    void Update()
    {
        if (SafetyGame.I == null || SafetyGame.I.InputLocked)
        {
            IsMoving = IsRunning = false;
            return;
        }
        var kb = Keyboard.current;
        if (kb == null) return;

        var ms = Mouse.current;
        if (ms != null && _cam != null)
        {
            Vector2 d = ms.delta.ReadValue() * Sensitivity;
            transform.Rotate(0f, d.x, 0f);
            _pitch = Mathf.Clamp(_pitch - d.y, -83f, 83f);
            _cam.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        float f = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        float r = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        bool run = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

        Vector3 move = transform.forward * f + transform.right * r;
        IsMoving = move.sqrMagnitude > 0.01f;
        IsRunning = run && IsMoving;
        if (IsMoving) move = move.normalized * (IsRunning ? RunSpeed : WalkSpeed);
        else move = Vector3.zero;

        _vy = _cc.isGrounded ? -1f : _vy - 20f * Time.deltaTime;
        _cc.Move((move + Vector3.up * _vy) * Time.deltaTime);
    }
}
