using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace MoxoCPT
{
    public class Cards : MonoBehaviour
    {
        public static Cards Instance;
        
        // there are 6 (0.5f), 3(1f), and 1(4f), making the probabilities, 60%, 30%, and 10% respectively.
        public float[] cardDuration = {0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f, 1f, 1f, 4f};
        
        [HideInInspector] public Transform curCard;
        [HideInInspector] public Transform[] cardArr;

        [HideInInspector] public int numCards;
        [HideInInspector] public int numDurations;
        [HideInInspector] public int numSeen = 0;
        [HideInInspector] public bool isActive;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else Destroy(Instance);
        }

        private void Start()
        {
            cardArr = GetComponentsInChildren<Transform>().Where(child =>
                child.gameObject.CompareTag("Target") || child.gameObject.CompareTag("NonTarget")).ToArray();

            numCards = cardArr.Length;
            numDurations = cardDuration.Length;
            
            TurnAllCardsOff();
        }

        public void UpdateCurCard(Transform newCard)
        {
            curCard = newCard;
            numSeen++;
        }

        private void TurnAllCardsOff()
        {
            foreach (var card in cardArr) card.gameObject.SetActive(false);
        }
    }
}

