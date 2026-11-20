using UnityEngine;

namespace TimeTheft
{
    [DefaultExecutionOrder(-50)]
    public sealed class TimeTheft2DFocusComposition : MonoBehaviour
    {
        [System.Serializable]
        private struct Composition
        {
            public Vector2 opponentPosition;
            [Min(.01f)] public float opponentScale;
            [Range(0f, 1f)] public float opponentOpacity;
            public Vector2 watchPosition;
            [Min(.01f)] public float watchScale;
        }

        [SerializeField] private CanvasGroup opponentRig;
        [SerializeField] private RectTransform watchFocus;
        [SerializeField] private GameObject watchActions;
        [SerializeField] private GameObject committedAction;
        [SerializeField] private GameObject transfers;
        [SerializeField] private Composition selection;
        [SerializeField] private Composition settlement;

        private RectTransform opponentTransform;
        private Composition workspace;

        private void Awake()
        {
            opponentTransform = (RectTransform)opponentRig.transform;
            workspace = new Composition
            {
                opponentPosition = opponentTransform.anchoredPosition,
                opponentScale = opponentTransform.localScale.x,
                opponentOpacity = opponentRig.alpha,
                watchPosition = watchFocus.anchoredPosition,
                watchScale = watchFocus.localScale.x
            };
        }

        private void LateUpdate()
        {
            // The stage study already controls which phase-specific views are visible.
            Composition target = workspace;
            if (transfers.activeSelf)
                target = settlement;
            else if (watchActions.activeSelf || committedAction.activeSelf)
                target = selection;

            float blend = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
            opponentTransform.anchoredPosition = Vector2.Lerp(opponentTransform.anchoredPosition, target.opponentPosition, blend);
            opponentTransform.localScale = Vector3.Lerp(opponentTransform.localScale, Vector3.one * target.opponentScale, blend);
            opponentRig.alpha = Mathf.Lerp(opponentRig.alpha, target.opponentOpacity, blend);

            // The stage study moves the actual watch and its effect to this target.
            watchFocus.anchoredPosition = target.watchPosition;
            watchFocus.localScale = new Vector3(target.watchScale, target.watchScale, 1f);
        }
    }
}
