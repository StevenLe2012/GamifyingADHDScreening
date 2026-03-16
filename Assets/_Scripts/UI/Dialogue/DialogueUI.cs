using System.Collections.Generic;
using Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UIElements
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Subtitle")]
        [SerializeField] private TextMeshProUGUI sentenceText;

        [Header("Colors")]
        [SerializeField] private Color npcColor = Color.white;
        [SerializeField] private Color playerColor = Color.yellow;
        [SerializeField] private Color systemColor = Color.gray;
        [SerializeField] private string playerCharacterName = "You";

        [Header("Options UI (Non-Button Mode)")]
        [Tooltip("Turn this ON because your options are NOT Unity UI Buttons.")]
        [SerializeField] private bool useNonButtonOptions = true;

        [Tooltip("Drag UI Manager/Dialogue/DialogueOptions here (parent of DialogueOption1..5).")]
        [SerializeField] private Transform dialogueOptionsRoot;

        [Tooltip("If you use StickOptionSelector, drag it here so it can resync when options appear.")]
        [SerializeField] private StickOptionSelector stickSelector;

        [Header("Optional (Button Mode)")]
        [Tooltip("Only used if useNonButtonOptions = false. If left empty, we will auto-find.")]
        [SerializeField] private Button[] optionButtons;

        private Queue<AudioObjects> _audioObjects;
        private DialogueOption[] _dialogueOptions;
        private AudioObjects _curAudioObject;
        private Vocals _speaker;
        private UnityEvent _nextEvent;

        private void Start()
        {
            if (!useNonButtonOptions)
            {
                if (optionButtons == null || optionButtons.Length == 0)
                    optionButtons = GetComponentsInChildren<Button>(true);
            }

            _audioObjects = new Queue<AudioObjects>();
            _speaker = GetComponent<Vocals>();

            HideNonButtonOptions();
            DisableAllButtons();
            gameObject.SetActive(false);
        }

        // ✅ NEW: external hard stop (handlers call this before advancing)
        public void StopAutoAdvance()
        {
            CancelInvoke(nameof(ContinueDialogue));
            CancelInvoke(nameof(PlayNextEvent));
        }

        public void BindDialogueUnit(DialogueUnit unit)
        {
            SetAudioObjects(unit.audioObjects);
            SetDialogueOptions(unit.options);
            SetNextEvent(unit.nextEventWithoutButton);
        }

        public void SetAudioObjects(IEnumerable<AudioObjects> audioObjects)
        {
            _audioObjects.Clear();
            if (audioObjects == null) return;
            foreach (var audioObject in audioObjects)
                _audioObjects.Enqueue(audioObject);
        }

        public void SetDialogueOptions(DialogueOption[] dialogueOptions)
        {
            _dialogueOptions = dialogueOptions ?? new DialogueOption[0];
        }

        public void SetNextEvent(UnityEvent nextEvent)
        {
            _nextEvent = nextEvent;
        }

        public void ContinueDialogue()
        {
            gameObject.SetActive(true);

            // ✅ Always clear old timers before doing anything
            StopAutoAdvance();

            // While lines are playing, options should be hidden
            HideNonButtonOptions();
            DisableAllButtons();

            if (GetNextAudioObject())
            {
                SayCurDialogue();
                Invoke(nameof(ContinueDialogue), _curAudioObject.clip.length);
                return;
            }

            // NOTE: when GetNextAudioObject() returns false, _curAudioObject is the LAST one we displayed
            // We still want to speak it once.
            SayCurDialogue();

            if (_dialogueOptions != null && _dialogueOptions.Length > 0)
            {
                // ✅ OPTIONS MODE: do NOT schedule any more ContinueDialogue
                StopAutoAdvance();
                ShowOptions(_dialogueOptions);
                return;
            }

            if (_nextEvent != null)
            {
                Invoke(nameof(PlayNextEvent), _curAudioObject.clip.length);
                return;
            }

            EndDialogue();
        }

        private void SayCurDialogue()
        {
            if (_speaker != null && _curAudioObject != null)
                _speaker.Say(_curAudioObject);
        }

        public void EndDialogue()
        {
            StopAutoAdvance();
            HideNonButtonOptions();
            DisableAllButtons();
            gameObject.SetActive(false);
        }

        private bool GetNextAudioObject()
        {
            if (_audioObjects.Count == 0)
                return false;

            _curAudioObject = _audioObjects.Dequeue();
            DisplaySentence(_curAudioObject);

            // return true if there's still more after this one
            return _audioObjects.Count > 0;
        }

        private void DisplaySentence(AudioObjects audioObject)
        {
            var who = audioObject != null ? (audioObject.character ?? "").Trim() : "";
            bool isPlayer = !string.IsNullOrEmpty(who) &&
                            who.Equals(playerCharacterName, System.StringComparison.OrdinalIgnoreCase);

            if (isPlayer)
            {
                KoalaAnimBus.EnsureStandingIdleAll();
                EkonnAnimBus.EnsureStandingIdle();
                sentenceText.color = playerColor;
            }
            else if (!string.IsNullOrEmpty(who))
            {
                if (who.Equals("Koala", System.StringComparison.OrdinalIgnoreCase))
                {
                    KoalaAnimBus.StandUpAndTalkAll();
                    EkonnAnimBus.EnsureStandingIdle();
                }
                else if (who.Equals("Ekonn", System.StringComparison.OrdinalIgnoreCase))
                {
                    EkonnAnimBus.StandUpAndTalk();
                    KoalaAnimBus.EnsureStandingIdleAll();
                }
                else
                {
                    KoalaAnimBus.EnsureStandingIdleAll();
                    EkonnAnimBus.EnsureStandingIdle();
                }

                sentenceText.color = npcColor;
            }
            else
            {
                KoalaAnimBus.EnsureStandingIdleAll();
                EkonnAnimBus.EnsureStandingIdle();
                sentenceText.color = systemColor;
            }

            sentenceText.text = audioObject != null ? (audioObject.subtitle ?? "") : "";
        }

        // ---------------- OPTIONS ----------------

        private void ShowOptions(DialogueOption[] options)
        {
            if (useNonButtonOptions) DisplayNonButtonOptions(options);
            else DisplayButtonOptions(options);
        }

        private void DisplayButtonOptions(DialogueOption[] options)
        {
            if (optionButtons == null || optionButtons.Length == 0)
            {
                Debug.LogWarning("[DialogueUI] Button mode is ON but no optionButtons exist.");
                return;
            }

            int optionsCount = options.Length;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < optionsCount)
                {
                    var text = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (text) { text.text = options[i].buttonText; text.color = playerColor; }

                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(options[i].actionToTrigger.Invoke);

                    optionButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            if (stickSelector != null) stickSelector.SyncButtons();
        }

        // private void DisplayNonButtonOptions(DialogueOption[] options)
        // {
        //     if (dialogueOptionsRoot == null)
        //     {
        //         Debug.LogWarning("[DialogueUI] dialogueOptionsRoot is not assigned (Non-Button Options).");
        //         return;
        //     }

        //     dialogueOptionsRoot.gameObject.SetActive(true);

        //     int optionsCount = options.Length;

        //     for (int i = 0; i < dialogueOptionsRoot.childCount; i++)
        //     {
        //         var optionGO = dialogueOptionsRoot.GetChild(i).gameObject;

        //         if (i < optionsCount)
        //         {
        //             optionGO.SetActive(true);

        //             var tmp = optionGO.GetComponentInChildren<TextMeshProUGUI>(true);
        //             if (tmp != null)
        //             {
        //                 tmp.text = options[i].buttonText;
        //                 tmp.color = playerColor;
        //             }

        //             var action = optionGO.GetComponent<DialogueOptionAction>();
        //             if (action != null) action.SetIndex(i);
        //         }
        //         else
        //         {
        //             optionGO.SetActive(false);
        //         }
        //     }

        //     if (stickSelector != null)
        //     {
        //         stickSelector.SyncButtons();
        //         stickSelector.NotifyOptionsOpened();   // ✅ NEW: ignore same-frame confirm
        //     }

        //     // ---- LOG: options became visible (snapshot) ----
        //     long shownMs = (long)(Time.realtimeSinceStartup * 1000.0f);
        //     string presented = JoinOptionsText(options);
        //     int count = options != null ? options.Length : 0;

        //     // selectionChangedCountAtOpen = 0 by default (we'll increment via StickOptionSelector)
        //     MoxoCPT.LoggingDialogueChoices.OptionsShown(shownMs, presented, count, 0);


        // }

        // ✅ NEW: allow option scripts to close options instantly (prevents re-open flicker)
        public void CloseOptionsImmediate()
        {
            StopAutoAdvance();
            HideNonButtonOptions();
            DisableAllButtons();
        }


        private void DisplayNonButtonOptions(DialogueOption[] options)
        {
            if (dialogueOptionsRoot == null)
            {
                Debug.LogWarning("[DialogueUI] dialogueOptionsRoot is not assigned (Non-Button Options).");
                return;
            }

            dialogueOptionsRoot.gameObject.SetActive(true);

            // Build lookup: option number -> GO (DialogueOption1..5 -> 1..5)
            var byNumber = new Dictionary<int, GameObject>();
            for (int i = 0; i < dialogueOptionsRoot.childCount; i++)
            {
                var go = dialogueOptionsRoot.GetChild(i).gameObject;
                int n = ExtractOptionNumber(go.name); // DialogueOption1 -> 1
                if (n > 0) byNumber[n] = go;
            }

            // First disable all children
            for (int i = 0; i < dialogueOptionsRoot.childCount; i++)
                dialogueOptionsRoot.GetChild(i).gameObject.SetActive(false);

            int optionsCount = options != null ? options.Length : 0;

            // Mapping from logical option index (1..5) to visual slot number.
            // For 5 options: 1→4, 2→2, 3→1, 4→3, 5→5 (symmetric around DialogueOption1 center)
            // For 3 options: 1→2, 2→1, 3→3  (use middle three slots 2,1,3)
            // For 2 options: 1→2, 2→1      (left/right of center)
            // For 1 option : 1→1           (center)
            int[] slots5 = new int[] { 4, 2, 1, 3, 5 };
            int[] slots3 = new int[] { 2, 1, 3 };
            int[] slots2 = new int[] { 2, 1 };
            int[] slots1 = new int[] { 1 };

            for (int k = 0; k < optionsCount; k++)
            {
                int logicalOneBased = k + 1;
                int slotNumber;

                if (optionsCount >= 4)
                {
                    if (logicalOneBased > slots5.Length) break;
                    slotNumber = slots5[logicalOneBased - 1];
                }
                else if (optionsCount == 3)
                {
                    if (logicalOneBased > slots3.Length) break;
                    slotNumber = slots3[logicalOneBased - 1];
                }
                else if (optionsCount == 2)
                {
                    if (logicalOneBased > slots2.Length) break;
                    slotNumber = slots2[logicalOneBased - 1];
                }
                else // optionsCount == 1
                {
                    slotNumber = slots1[0];
                }

                if (!byNumber.TryGetValue(slotNumber, out var optionGO) || optionGO == null)
                    continue;

                optionGO.SetActive(true);

                var tmp = optionGO.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = options[k].buttonText;
                    tmp.color = playerColor;
                }

                // Set logical option index so navigation can be based on 1..N
                var action = optionGO.GetComponent<DialogueOptionAction>();
                if (action != null) action.SetIndex(k);
            }

            if (stickSelector != null)
            {
                stickSelector.SyncButtons();
                // StickOptionSelector will impose its own navigation order over the active slots.
            }

            // ---- LOG: options became visible (snapshot) ----
            long shownMs = (long)(Time.realtimeSinceStartup * 1000.0f);
            string presented = JoinOptionsText(options);
            int count = optionsCount;
            MoxoCPT.LoggingDialogueChoices.OptionsShown(shownMs, presented, count, 0);
        }

        // Helper: "DialogueOption4" -> 4
        private static int ExtractOptionNumber(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;

            const string prefix = "DialogueOption";
            int idx = name.IndexOf(prefix, System.StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return -1;

            int start = idx + prefix.Length;
            if (start >= name.Length) return -1;

            int n = 0;
            bool any = false;
            for (int i = start; i < name.Length; i++)
            {
                char c = name[i];
                if (c < '0' || c > '9') break;
                any = true;
                n = (n * 10) + (c - '0');
            }

            return any ? n : -1;
        }



        private void HideNonButtonOptions()
        {
            if (dialogueOptionsRoot != null)
                dialogueOptionsRoot.gameObject.SetActive(false);
        }

        private void DisableAllButtons()
        {
            if (optionButtons == null) return;
            foreach (var button in optionButtons)
                if (button != null) button.gameObject.SetActive(false);
        }

        private static string JoinOptionsText(DialogueOption[] options)
        {
            if (options == null || options.Length == 0) return "";
            // Use | delimiter (safe for CSV, easier than commas)
            // If your text can include |, pick another delimiter.
            var parts = new string[options.Length];
            for (int i = 0; i < options.Length; i++)
                parts[i] = options[i] != null ? (options[i].buttonText ?? "") : "";
            return string.Join("|", parts);
        }

        private void PlayNextEvent()
        {
            _nextEvent?.Invoke();
        }
    }
}
