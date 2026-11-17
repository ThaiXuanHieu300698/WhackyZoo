using UnityEngine;
using UnityEngine.UI;

namespace WhackyZoo
{
    /// <summary>
    /// Giữ match 0.5 cho màn bình thường; màn hẹp hơn khung thiết kế (vd. máy gập, 21:9 trở lên)
    /// thì co theo chiều ngang để thanh trên và lưới hố không bị cắt hai bên.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float defaultMatch = 0.5f;

        CanvasScaler scaler;
        int lastW, lastH;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Apply();
        }

        void Apply()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            if (lastH <= 0) return;
            var reference = scaler.referenceResolution;
            float referenceAspect = reference.x / reference.y;
            float aspect = (float)lastW / lastH;
            scaler.matchWidthOrHeight = aspect < referenceAspect ? 0f : defaultMatch;
        }
    }
}
