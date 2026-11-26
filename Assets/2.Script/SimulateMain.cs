using System;
using TMPro;
using UnityEngine;

public class SimulateMain : MonoBehaviour
{
    private Rigidbody _rb;

    private readonly float airDensity = 1.225f;
    private Vector3 _windForceVector = new();


    [Header("Wind Force")]
    [SerializeField] private float _windForce;
    [SerializeField] private Vector3 _windDirection = new(1, 0, 0);
    [SerializeField] private float _windSpeed = 5f;
    [SerializeField] private float _windCheckDistance;
    [SerializeField] private LayerMask _obstacleLayer;

    private SimulateObject _target;

    private void Start()
    {
        _target = SimulateObject.Instance;

        _rb = _target.ObjectRigibody;
    }

    private void WindForceFormula(Rigidbody rb, float dragCoeff, float area)
    {
        if (rb == null) return;

        Vector3 _windVeclocity = _windDirection.normalized * _windSpeed;

        Vector3 effectiveWind = IsBlockFromWind() ? Vector3.zero : _windVeclocity;

        Vector3 relativeVelocity = rb.linearVelocity - effectiveWind;
        float relativeSpeed = relativeVelocity.magnitude;

        if (relativeSpeed <= 0.000001f)
        {
            _windForce = 0f;
            _windForceVector = Vector3.zero;
            return;
        }

        _windForce = 0.5f * airDensity * (relativeSpeed * relativeSpeed) * dragCoeff * area;

        _windForceVector = -relativeVelocity.normalized * _windForce;
    }

    private void FixedUpdate()
    {
        if (_target == null || _target.ObjectRigibody == null) return;

        _windDirection = transform.forward;

        WindForceFormula(_rb, _target.ObjectCoefficient, _target.CrossSelectionArea);

        Vector3 objectThrustVector = _target.GetThrustForce();
        Vector3 moveForceVector = _target.GetMoveForce();

        Vector3 totalForce = _windForceVector + objectThrustVector + moveForceVector;

        _rb.AddForce(totalForce, ForceMode.Force);

        Debug.DrawRay(_rb.position, -_windDirection.normalized * _windCheckDistance, Color.red);
    }

    private bool IsBlockFromWind()
    {
        if (_windDirection.sqrMagnitude <= 0.000001f) return false;

        Vector3 toWindSourceDir = -_windDirection.normalized;

        return Physics.Raycast(_rb.position, toWindSourceDir, _windCheckDistance, _obstacleLayer);
    }

    private void OnDrawGizmos()
    {
        _windDirection = transform.forward;
    }

}
