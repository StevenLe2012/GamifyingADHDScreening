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
    public class NewDialogueUI : MonoBehaviour
    {
        // this is where we will add the npc name and text they say
        [SerializeField] private TextMeshProUGUI subtitleText;

        private Button[] _buttons;  // all buttons user can press
        private DialogueOption[] _dialogueOptions;  // all dialogue options applied to each button
        private Queue<AudioObjects> _audioObjects;  // all audio objects in queue for text and sound

        // initializes all private variables
        void Start()
        {
            _buttons = GetComponentsInChildren<Button>();
            _audioObjects = new Queue<AudioObjects>();
            gameObject.SetActive(false);
        }

        // puts dialogue options and default options into the private variables
        public void SetDialogueOptions(DialogueOption[] dialogueOptions)
        {
            _dialogueOptions = dialogueOptions;
        }

        // displays the next dialogue, and keep calling until out of options then will end dialogue.
        public void ContinueDialogue()
        {
            gameObject.SetActive(true);
            if (_dialogueOptions.Length > 0)
            {
                DisplayDialogueOptions();
            }
            else
            {
                EndDialogue();
            }
        }

        // ends the dialogue
        public void EndDialogue()
        {
            gameObject.SetActive(false);
        }

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

        
        public void SetVocals(IEnumerable<AudioObjects> audioObjects)
        {
            _audioObjects.Clear();
            foreach (var audioObject in audioObjects)
            {
                _audioObjects.Enqueue(audioObject);
            }
        }

        public AudioObjects getAudioObject()
        {
            return _audioObjects.Dequeue();
            //Vocals.instance.Say(audioObject);  // my attempt to play the voice and subtitles when displaying sentences.
        }

    }

}

