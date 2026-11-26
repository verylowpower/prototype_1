using System;
using UnityEngine;

public class Drone : SimulateObject
{
    public new static Drone Instance { get; private set; }

    [Header("Drone Tilt & Flight")]
    [SerializeField] private float _maxTiltAngle = 30f;   // Góc nghiêng tối đa (độ) khi bấm WASD
    [SerializeField] private float _tiltSpeed = 8f;       // Tốc độ nghiêng thân drone
    [SerializeField] private float _climbForce = 12f;     // Lực tăng/giảm độ cao khi bấm Space/Ctrl
    [SerializeField] private bool _autoHover = true;      // Tự bù lực nâng theo góc nghiêng để giữ độ cao

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        if (_objectRigibody != null)
        {
            // Khóa xoay vật lý tự do để MoveRotation kiểm soát góc nghiêng ổn định
            _objectRigibody.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    protected void Update()
    {
        ObjectControl();
    }

    private void FixedUpdate()
    {
        if (_objectRigibody == null) return;

        // W/S (z) -> nghiêng quanh trục X (Pitch); A/D (x) -> nghiêng quanh trục Z (Roll)
        float targetPitch = _inputMovement.z * _maxTiltAngle;
        float targetRoll = -_inputMovement.x * _maxTiltAngle;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, transform.eulerAngles.y, targetRoll);
        Quaternion nextRotation = Quaternion.Slerp(_objectRigibody.rotation, targetRotation, _tiltSpeed * Time.fixedDeltaTime);
        _objectRigibody.MoveRotation(nextRotation);
    }

    public override Vector3 GetThrustForce()
    {
        float thrust = _objectForce;

        // Khi nghiêng thân, bù thêm lực theo cos(góc nghiêng) để không bị rớt độ cao
        if (_autoHover && _objectRigibody != null)
        {
            float upAlignment = Mathf.Max(0.5f, Vector3.Dot(transform.up, Vector3.up));
            float hoverThrust = (_objectRigibody.mass * Physics.gravity.magnitude) / upAlignment;
            thrust = hoverThrust;
        }

        thrust += _inputMovement.y * _climbForce;
        return transform.up * Mathf.Max(0f, thrust);
    }

    // Drone di chuyển ngang bằng cách nghiêng thân + lực nâng (transform.up), không đẩy lực ngang trực tiếp
    public override Vector3 GetMoveForce() => Vector3.zero;
}
