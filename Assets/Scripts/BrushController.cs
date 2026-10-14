using UnityEngine;

public sealed class BrushController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MaterialSurface surface;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;

    [Header("Radius")]
    [SerializeField, Min(0.05f)] private float baseRadius = 1f;
    [SerializeField, Min(0f)] private float radiusAmplitude = 0.35f;
    [SerializeField, Min(0f)] private float radiusFrequency = 0.25f;

    [SerializeField]
    private AnimationCurve radiusCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] private bool drawBrush = true;

    private Vector2 previousPosition;
    private float previousRadius;
    private bool initialized;

    private Vector2 currentPosition;
    private float currentRadius;
    public float CurrentRadius => currentRadius;

    private void Start()
    {
        if (surface == null)
        {
            Debug.LogError(
                "BrushController: Surface is not assigned.",
                this);

            enabled = false;
            return;
        }

        Vector3 local = surface.transform.InverseTransformPoint(
            transform.position);

        currentPosition = new Vector2(local.x, local.z);

        ClampPosition();

        currentRadius = EvaluateRadius();
        previousPosition = currentPosition;
        previousRadius = currentRadius;

        UpdateTransform();

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        float dt = Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.R))
            surface.ResetSurface();

        previousPosition = currentPosition;
        previousRadius = currentRadius;

        Vector2 input = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) ? 1f : 0f),

            (Input.GetKey(KeyCode.W) ? 1f : 0f) -
            (Input.GetKey(KeyCode.S) ? 1f : 0f)
        );

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        currentPosition += input * moveSpeed * dt;

        ClampPosition();

        currentRadius = EvaluateRadius();

        UpdateTransform();

        if (Input.GetKey(KeyCode.Space) && dt > 0f)
        {
            AccumulateAlongPath(dt);
            surface.ApplyMeshChanges();
        }
    }

    private float EvaluateRadius()
    {
        float phase = Mathf.Repeat(
            Time.time * radiusFrequency,
            1f);

        float curveValue = radiusCurve.Evaluate(phase);

        return Mathf.Max(
            0.05f,
            baseRadius + radiusAmplitude *
            (2f * curveValue - 1f));
    }

    private void AccumulateAlongPath(float dt)
    {
        Vector2 movement = currentPosition - previousPosition;
        float distance = movement.magnitude;

        float sampleSpacing = surface.CellSize * 0.5f;

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / sampleSpacing));

        float sampleDeltaTime = dt / steps;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;

            Vector2 samplePosition = Vector2.Lerp(
                previousPosition,
                currentPosition,
                t);

            float sampleRadius = Mathf.Lerp(
                previousRadius,
                currentRadius,
                t);

            surface.AddMaterialAt(
                samplePosition,
                sampleRadius,
                sampleDeltaTime);
        }
    }

    private void ClampPosition()
    {
        float halfSize = surface.Size * 0.5f;

        currentPosition.x = Mathf.Clamp(
            currentPosition.x,
            -halfSize,
            halfSize);

        currentPosition.y = Mathf.Clamp(
            currentPosition.y,
            -halfSize,
            halfSize);
    }

    private void UpdateTransform()
    {
        Vector3 localPosition = new Vector3(
            currentPosition.x,
            0.03f,
            currentPosition.y);

        transform.position =
            surface.transform.TransformPoint(localPosition);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawBrush)
            return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            currentRadius);
    }
}
