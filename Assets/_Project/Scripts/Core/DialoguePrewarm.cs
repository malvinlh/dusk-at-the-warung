using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuskWarung.Core
{
    /// <summary>
    /// Warms the dialogue fonts and text material once at scene load, so the first spoken line does
    /// not hitch while glyphs are rasterised and uploaded to the GPU (most visible in a build, and on
    /// the gated grove route where that hint is the first dialogue shown). It renders a throwaway,
    /// fully transparent label using the same fonts the SayDialog uses, then destroys it. It never
    /// touches the SayDialog itself, so it cannot affect the dialogue.
    /// </summary>
    public class DialoguePrewarm : MonoBehaviour
    {
        [SerializeField, Tooltip("The scene's Fungus SayDialog to read fonts from. Auto-found if left empty.")]
        private Fungus.SayDialog sayDialog;

        [SerializeField, Tooltip("Frames to render the warm-up label before destroying it.")]
        private int warmFrames = 3;

        // Every glyph the dialogue is likely to use, so each is rasterised into the atlas up front.
        private const string WarmText =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?'\"()-:;…";

        private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

        private IEnumerator Start()
        {
            if (sayDialog == null)
            {
                sayDialog = Object.FindFirstObjectByType<Fungus.SayDialog>(FindObjectsInactive.Include);
            }

            if (sayDialog == null)
            {
                yield break;
            }

            var root = new GameObject("DialoguePrewarm (temp)");
            root.transform.SetParent(transform, false);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            int warmed = 0;

            foreach (Text source in sayDialog.GetComponentsInChildren<Text>(true))
            {
                if (source.font == null)
                {
                    continue;
                }

                Text label = NewLabel<Text>(root.transform);
                label.font = source.font;
                label.fontSize = 32;
                label.text = WarmText;
                label.color = Transparent;
                label.raycastTarget = false;
                warmed++;
            }

            foreach (TMP_Text source in sayDialog.GetComponentsInChildren<TMP_Text>(true))
            {
                if (source.font == null)
                {
                    continue;
                }

                TextMeshProUGUI label = NewLabel<TextMeshProUGUI>(root.transform);
                label.font = source.font;
                if (source.fontSharedMaterial != null)
                {
                    label.fontSharedMaterial = source.fontSharedMaterial;
                }
                label.fontSize = 32;
                label.text = WarmText;
                label.color = Transparent;
                label.raycastTarget = false;
                warmed++;
            }

            if (warmed == 0)
            {
                Destroy(root);
                yield break;
            }

            for (int i = 0; i < Mathf.Max(1, warmFrames); i++)
            {
                yield return null; // let the canvas rebuild, rasterise the glyphs, and draw once
            }

            Destroy(root);
        }

        /// <summary>Creates a child UI label of type <typeparamref name="T"/> with room to lay out.</summary>
        private static T NewLabel<T>(Transform parent) where T : Component
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(2000f, 200f);
            return go.AddComponent<T>();
        }
    }
}
