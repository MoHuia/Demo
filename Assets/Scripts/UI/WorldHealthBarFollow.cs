using UnityEngine;

[DefaultExecutionOrder(100)]
public class WorldHealthBarFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private CanvasGroup visibilityGroup;
    [SerializeField] private Vector2 barOffset = new Vector2(56.6f, 75.4f);

    [SerializeField] private RectTransform targetLine;
    [SerializeField] private RectTransform targetLineEnd;
    [SerializeField, Min(0.01f)] private float pointerScale = 0.8f;
    [SerializeField, Min(0f)] private float lineEndGap = 4f;

    [Header("Distance layout")]
    [SerializeField, Min(0f)] private float nearScale;
    [SerializeField, Min(0.01f)] private float nearDepth = 8f;
    [SerializeField, Min(0.01f)] private float farDepth = 14f;
    [SerializeField] private Vector2 nearBarOffset = new Vector2(84f, 99f);

    private RectTransform m_panel;
    private RectTransform m_parentRect;
    private Vector3 m_farScale;

    private void Awake()
    {
        m_panel = (RectTransform)transform;
        m_parentRect = m_panel.parent as RectTransform;
        m_farScale = m_panel.localScale;
    }

    private void LateUpdate()
    {
        if (visibilityGroup == null)
        {
            return;
        }

        if (target == null || !target.gameObject.activeInHierarchy || viewCamera == null || m_parentRect == null)
        {
            visibilityGroup.alpha = 0f;
            return;
        }

        Vector3 pointer = viewCamera.WorldToScreenPoint(target.position);
        if (pointer.z <= 0f || !viewCamera.pixelRect.Contains(pointer) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_parentRect, pointer, null, out Vector2 pointerPoint))
        {
            visibilityGroup.alpha = 0f;
            return;
        }

        Vector2 displayOffset = barOffset;
        if (nearScale > 0f)
        {
            float distanceBlend = Mathf.InverseLerp(nearDepth, farDepth, pointer.z);
            float scale = Mathf.Lerp(nearScale, m_farScale.x, distanceBlend);
            m_panel.localScale = new Vector3(scale, scale, m_farScale.z);
            displayOffset = Vector2.Lerp(nearBarOffset, barOffset, distanceBlend);
        }

        Vector3 barPosition = m_parentRect.TransformPoint(pointerPoint + displayOffset);
        if (targetLineEnd != null)
        {
            m_panel.position += barPosition - targetLineEnd.position;
        }
        else
        {
            m_panel.position = barPosition;
        }
        visibilityGroup.alpha = 1f;

        if (targetLine != null && targetLineEnd != null)
        {
            Vector2 start = m_panel.InverseTransformPoint(m_parentRect.TransformPoint(pointerPoint));
            Vector2 end = m_panel.InverseTransformPoint(targetLineEnd.position);
            Vector2 direction = end - start;

            float panelScale = Mathf.Max(0.01f, m_panel.localScale.x);
            float lineScale = pointerScale / panelScale;
            targetLine.localPosition = start;
            targetLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            targetLine.localScale = new Vector3(lineScale, lineScale, 1f);
            targetLine.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(0f, (direction.magnitude * panelScale - lineEndGap) / pointerScale));
        }
    }
}
