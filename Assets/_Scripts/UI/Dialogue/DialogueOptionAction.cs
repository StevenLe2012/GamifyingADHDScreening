using UnityEngine;

namespace Dialogue
{
    public class DialogueOptionAction : MonoBehaviour
    {
        [SerializeField] private int optionIndex = 0;
        public int OptionIndex => optionIndex;

        public void SetIndex(int i) => optionIndex = i;

        public void Choose()
        {
            long choiceMs = (long)(Time.realtimeSinceStartup * 1000.0f);

            // Best-effort: selected option text from UI hierarchy (TMP under this option)
            string selectedText = "";
            var tmp = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null) selectedText = tmp.text ?? "";

            // Log choice
            MoxoCPT.LoggingDialogueChoices.LogChoice(optionIndex, selectedText, choiceMs);

            // ✅ NEW: close options UI immediately to avoid re-open flicker/resync
            var ui = FindObjectOfType<UIElements.DialogueUI>(true);
            if (ui != null) ui.CloseOptionsImmediate();

            // 1) Koala
            var koala = FindObjectOfType<KoalaDialogueHandler>(true);
            if (koala != null && koala.IsTalking)
            {
                koala.ChooseOptionByIndex(optionIndex);
                return;
            }

            // 2) Ekonn (TestHandler)
            var ekonn = Dialogue.TestHandler.I != null ? Dialogue.TestHandler.I : FindObjectOfType<Dialogue.TestHandler>(true);
            if (ekonn != null && ekonn.IsTalking)
            {
                ekonn.SelectOption(optionIndex);
                return;
            }

            // 3) DragonLord
            var dragon = FindObjectOfType<DragonLordDialogueHandler>(true);
            if (dragon != null && dragon.IsTalking)
            {
                dragon.SelectOption(optionIndex);
                return;
            }

            Debug.LogWarning($"[DialogueOptionAction] No active dialogue handler is talking. optionIndex={optionIndex}", this);
        }
    }
}
