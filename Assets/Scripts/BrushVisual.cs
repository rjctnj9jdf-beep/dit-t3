using UnityEngine;

public sealed class BrushVisual : MonoBehaviour
{
    [SerializeField] private BrushController brush;
    [SerializeField] private Transform visual;

    private void LateUpdate()
    {
        float d = brush.CurrentRadius * 2f;
        visual.localScale = new Vector3(d, d, d);
    }
}