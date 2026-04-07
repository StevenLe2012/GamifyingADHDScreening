using UnityEngine;
using TMPro;

namespace MoxoCPT
{
    /// <summary>
    /// Displays a "Progress: n/total cards" label in the upper-left corner
    /// during the real CPT game. Hidden during training.
    ///
    /// Setup:
    ///   1. Add a Canvas (Screen Space – Overlay) to your Main scene if one doesn't exist.
    ///   2. Inside it, add UI → Text - TextMeshPro, anchor it to the top-left corner.
    ///   3. Attach this script to that Text GameObject (or a parent Panel).
    ///   4. Assign the Label field in the Inspector.
    ///   5. Drag this GameObject into the ChangeShapes → Progress Display slot.
    /// </summary>
    public class CPTProgressDisplay : MonoBehaviour
    {
        [Tooltip("The TMP label that shows 'Progress: n/total cards'.")]
        [SerializeField] private TextMeshProUGUI label;

        [Tooltip("Format string. {0} = current card number, {1} = total cards.")]
        [SerializeField] private string format = "Progress: {0}/{1} cards";

        private int _total;

        void Awake()
        {
            // Stay hidden until the real game starts.
            gameObject.SetActive(false);
        }

        /// <summary>Called by ChangeShapes when the real game begins.</summary>
        public void Show(int total)
        {
            _total = Mathf.Max(1, total);
            gameObject.SetActive(true);
            Refresh(0);
        }

        /// <summary>Called by ChangeShapes each time a new card appears.</summary>
        public void UpdateCount(int current)
        {
            if (!gameObject.activeSelf) return;
            Refresh(current);
        }

        /// <summary>Called by ChangeShapes when the game ends.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Refresh(int current)
        {
            if (label)
                label.text = string.Format(format, current, _total);
        }
    }
}
