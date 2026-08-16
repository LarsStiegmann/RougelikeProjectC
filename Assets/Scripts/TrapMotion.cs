using UnityEngine;

/// <summary>
/// Drives the movement of a static trap prop. One component covers the three
/// patterns needed: a spinning saw that also slides along its rail, a pendulum
/// blade, and spears that thrust in and out on a timer.
///
/// Everything is driven from the object's own starting transform, so nothing about
/// the existing props has to be edited.
/// </summary>
public class TrapMotion : MonoBehaviour
{
    public enum Mode { SpinAndSlide, Pendulum, Thrust }

    [SerializeField] private Mode mode = Mode.SpinAndSlide;

    [Header("Spin (saw blade)")]
    [Tooltip("Degrees per second. The saw meshes are flat discs facing local X.")]
    [SerializeField] private float spinSpeed = 720f;

    [SerializeField] private Vector3 spinAxis = Vector3.right;

    [Header("Slide (along the rail)")]
    [Tooltip("Direction of travel in local space.")]
    [SerializeField] private Vector3 slideAxis = Vector3.forward;

    [Tooltip("How far it travels from the start point, in each direction.")]
    [SerializeField] private float slideDistance = 2f;

    [SerializeField] private float slideSpeed = 2f;

    [Header("Pendulum")]
    [Tooltip("Maximum swing angle from vertical, in degrees.")]
    [SerializeField] private float swingAngle = 60f;

    [SerializeField] private float swingSpeed = 1.4f;

    [Tooltip("Axis the blade swings around, in local space.")]
    [SerializeField] private Vector3 swingAxis = Vector3.forward;

    [Tooltip("Pivot point relative to the object, usually up at the housing.")]
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 2f, 0f);

    [Header("Thrust (spears)")]
    [SerializeField] private Vector3 thrustAxis = Vector3.up;

    [SerializeField] private float thrustDistance = 1.2f;

    [Tooltip("Time spent retracted before firing.")]
    [SerializeField] private float restTime = 1.6f;

    [Tooltip("Time taken to shoot out.")]
    [SerializeField] private float extendTime = 0.12f;

    [Tooltip("Time held out at full extension.")]
    [SerializeField] private float holdTime = 0.5f;

    [Tooltip("Time taken to pull back in.")]
    [SerializeField] private float retractTime = 0.5f;

    [Header("Timing")]
    [Tooltip("Offset so several traps do not move in perfect unison.")]
    [SerializeField] private float phaseOffset = 0f;

    [Tooltip("Damage component to arm only while the trap is dangerous. Optional.")]
    [SerializeField] private TrapDamage damageToGate;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private float timer;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
        timer = phaseOffset;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        switch (mode)
        {
            case Mode.SpinAndSlide: UpdateSpinAndSlide(); break;
            case Mode.Pendulum:     UpdatePendulum();     break;
            case Mode.Thrust:       UpdateThrust();       break;
        }
    }

    private void UpdateSpinAndSlide()
    {
        transform.Rotate(spinAxis.normalized, spinSpeed * Time.deltaTime, Space.Self);

        if (slideDistance > 0f && slideSpeed > 0f)
        {
            // ping-pong along the rail
            float t = Mathf.Sin(timer * slideSpeed);
            transform.localPosition = startLocalPosition + slideAxis.normalized * (t * slideDistance);
        }
    }

    private void UpdatePendulum()
    {
        float angle = Mathf.Sin(timer * swingSpeed) * swingAngle;

        // rotate about the housing rather than the blade's own centre
        transform.localRotation = startLocalRotation;
        transform.localPosition = startLocalPosition;

        Vector3 pivot = transform.TransformPoint(pivotOffset);
        transform.RotateAround(pivot, transform.TransformDirection(swingAxis.normalized), angle);
    }

    private void UpdateThrust()
    {
        float cycle = restTime + extendTime + holdTime + retractTime;
        float t = Mathf.Repeat(timer, cycle);

        float amount;
        bool dangerous;

        if (t < restTime)
        {
            amount = 0f;
            dangerous = false;
        }
        else if (t < restTime + extendTime)
        {
            amount = Mathf.InverseLerp(restTime, restTime + extendTime, t);
            dangerous = true;
        }
        else if (t < restTime + extendTime + holdTime)
        {
            amount = 1f;
            dangerous = true;
        }
        else
        {
            amount = 1f - Mathf.InverseLerp(restTime + extendTime + holdTime, cycle, t);
            dangerous = true;
        }

        transform.localPosition = startLocalPosition + thrustAxis.normalized * (amount * thrustDistance);

        if (damageToGate != null)
        {
            damageToGate.SetArmed(dangerous);
        }
    }
}
