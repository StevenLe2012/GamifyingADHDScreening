using System.Collections.Generic;
using Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * This file sets the UI such as NPC name and text they say for the dialogue.
 */

namespace UIElements
{
    public class DialogueUI : MonoBehaviour
    {
        // this is where we will add the npc name and text they say
        [SerializeField] private TextMeshProUGUI sentenceText;

        private Button[] _buttons;  // all buttons user can press
        private Queue<AudioObjects> _audioObjects;
        private Queue<string> _sentences;  // all sentences that will be said one by one
        private DialogueOption[] _dialogueOptions;  // all dialogue options applied to each button
        private DialogueOption _defaultDialogueOption; // the default dialogue option
        private AudioObjects _curAudioObject;
        private Vocals _speaker;

        // initializes all private variables
        void Start()
        {
            _buttons = GetComponentsInChildren<Button>();
            _sentences = new Queue<string>();
            _audioObjects = new Queue<AudioObjects>();
            _speaker = GetComponent<Vocals>();
            gameObject.SetActive(false);
        }

        /*
        // puts all sentences into a queue
        public void SetSentences(IEnumerable<AudioObjects> audioObjects)
        {
            _sentences.Clear();
            foreach (var audioObject in audioObjects)
            {
                _sentences.Enqueue(audioObject.subtitle);
            }
        }
        */
        public void SetAudioObjects(IEnumerable<AudioObjects> audioObjects)
        {
            _audioObjects.Clear();
            foreach (var audioObject in audioObjects)
            {
                _audioObjects.Enqueue(audioObject);
            }
        }

        // puts dialogue options and default options into the private variables
        public void SetDialogueOptions(DialogueOption[] dialogueOptions, DialogueOption defaultOptions)
        {
            _dialogueOptions = dialogueOptions;
            _defaultDialogueOption = defaultOptions;
        }

        // displays the next dialogue, and keep calling until out of options then will end dialogue.
        public void ContinueDialogue()
        {
            gameObject.SetActive(true);
            if (GetNextAudioObject())
            {
                for (var i = 0; i < _buttons.Length; i++)
                {
                    _buttons[i].gameObject.SetActive(false);
                }
                SayCurDialogue();
                Invoke("ContinueDialogue", _curAudioObject.clip.length); //SOMETIMES SKIPS (BUGGY?)
            }
            else if (_dialogueOptions.Length > 0)
            {
                SayCurDialogue();
                DisplayDialogueOptions();
            }
            else if (_defaultDialogueOption != null)
            {
                //SayCurDialogue();
                DisplayDefaultDialogueOption();
            }
            else
            {
                EndDialogue();
            }
        }

        public void SayCurDialogue()
        {
            _speaker.Say(_curAudioObject);
        }

        // ends the dialogue
        public void EndDialogue()
        {
            gameObject.SetActive(false);
        }

        private bool GetNextAudioObject()
        {
            if (_audioObjects.Count == 0)
            {
                return false;
            }
            _curAudioObject = _audioObjects.Dequeue();
            DisplaySentence(_curAudioObject);
            return _audioObjects.Count > 0;
        }

        // displays sentence on UI as long as there are still sentences in queue left.
        // Returns true when there is at least 1 more sentence in queue after showing it. Else, return false.
        private void DisplaySentence(AudioObjects audioObject)
        {
            sentenceText.text = audioObject.subtitle;
        }

        /*
        // displays only the continue button. The rest will be turned off.
        private void DisplayContinueDialogueButton()
        {
            if (_buttons.Length <= 0)
            {
                return;
            }

            for (var i = 0; i < _buttons.Length; i++)
            {
                if (i == 0)
                {
                    var text = _buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    text.text = "Continue";  // TODO: make this a localized string
                    _buttons[i].onClick.RemoveAllListeners();
                    _buttons[i].onClick.AddListener(ContinueDialogue);
                    _buttons[i].gameObject.SetActive(true);
                }
                else
                {
                    _buttons[i].gameObject.SetActive(false);
                }
            }
        }
        */

        // displays all the dialogue options availiable and turn the rest of the buttons off.
        private void DisplayDialogueOptions()
        {
            var optionsCount = _dialogueOptions.Length;
            for (var i = 0; i < _buttons.Length; i++)
            {
                if (i < optionsCount)
                {
                    var text = _buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    text.text = _dialogueOptions[i].buttonText;
                    _buttons[i].onClick.RemoveAllListeners();
                    _buttons[i].onClick.AddListener(_dialogueOptions[i].actionToTrigger.Invoke);
                    _buttons[i].gameObject.SetActive(true);
                }
                else
                {
                    _buttons[i].gameObject.SetActive(false);
                }
            }
        }

        private void DisplayDefaultDialogueOption()
        {
            if (_buttons.Length <= 0)
            {
                return;
            }

            for (var i = 0; i < _buttons.Length; i++)
            {
                if (i == 0)
                {
                    var text = _buttons[i].GetComponentInChildren<TextMeshProUGUI>();
                    text.text = _defaultDialogueOption.buttonText;
                    _buttons[i].onClick.RemoveAllListeners();
                    _buttons[i].onClick.AddListener(_defaultDialogueOption.actionToTrigger.Invoke);
                    _buttons[i].gameObject.SetActive(true);
                }
                else
                {
                    _buttons[i].gameObject.SetActive(false);
                }
            }

        }

        public AudioObjects getAudioObject()
        {
            return _audioObjects.Dequeue();
            //Vocals.instance.Say(audioObject);  // my attempt to play the voice and subtitles when displaying sentences.
        }

    }

}

