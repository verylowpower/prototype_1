using UnityEngine;
using UnityEngine.InputSystem;

public class SimulateObject : MonoBehaviour
{
    public static SimulateObject Instance { get; private set; }

    [Header("Database")]
    [SerializeField] protected string _objectId;           // ID trong SimulateObjectDatabase.json (để trống sẽ tự lấy tên class)
    [SerializeField] protected bool _autoLoadFromDatabase = true;

    [Header("Physics & Control")]
    [SerializeField] protected float _objectCoefficient;   // Hệ số cản gió (Cd), thường 0.8 - 1.2 với drone
    [SerializeField] protected float _objectForce;         // Lực nâng theo hướng lên (N)
    [SerializeField] protected float _moveForce = 20f;     // Lực đẩy di chuyển ngang theo WASD (N)
    [SerializeField] protected Rigidbody _objectRigibody;  // Rigidbody của vật thể để tác động lực
    [SerializeField] protected float _crossSelectionArea;  // Diện tích mặt cắt đón gió (m²)

    public float ObjectCoefficient => _objectCoefficient;
    public float CrossSelectionArea => _crossSelectionArea;
    public float ObjectForce => _objectForce;
    public float MoveForce => _moveForce;
    public Rigidbody ObjectRigibody => _objectRigibody;
    public Vector3 InputMovement => _inputMovement;

    public virtual Vector3 GetThrustForce() => transform.up * _objectForce;
    public virtual Vector3 GetMoveForce() => _inputMovement * _moveForce;

    protected Vector3 _inputMovement;                      // Hướng di chuyển chuẩn hóa từ bàn phím (WASD)

    protected virtual void Awake()
    {
        Instance = this;

        if (_autoLoadFromDatabase)
        {
            ApplyFromDatabase();
        }
    }

    [ContextMenu("Apply From Database")]
    public void ApplyFromDatabase()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SimulateObjectDatabase.LoadDatabase();
        }
#endif
        string lookupId = string.IsNullOrEmpty(_objectId) ? GetType().Name : _objectId;

        if (!SimulateObjectDatabase.TryGetData(lookupId, out SimulateObjectData data))
        {
            Debug.LogWarning($"[{name}] Không tìm thấy id '{lookupId}' trong SimulateObjectDatabase.json", this);
            return;
        }

        _objectCoefficient = data.objectCoefficient;
        _objectForce = data.objectForce;
        _moveForce = data.moveForce;
        _crossSelectionArea = data.crossSelectionArea;

        if (_objectRigibody != null && data.mass > 0f)
        {
            _objectRigibody.mass = data.mass;
        }
    }

    protected void ObjectControl()
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;

        var currentKeyboard = Keyboard.current;
        if (currentKeyboard != null)
        {
            // WASD
            if (currentKeyboard.wKey.isPressed) z += 1;
            if (currentKeyboard.sKey.isPressed) z -= 1;
            if (currentKeyboard.aKey.isPressed) x -= 1;
            if (currentKeyboard.dKey.isPressed) x += 1;

            // Fly
            if (currentKeyboard.spaceKey.isPressed) y += 1;
            if (currentKeyboard.ctrlKey.isPressed) y -= 1;

            Vector3 horizontal = new Vector3(x, 0f, z).normalized;
            _inputMovement = new Vector3(horizontal.x, y, horizontal.z);
        }
        else _inputMovement = Vector3.zero;

    }
}