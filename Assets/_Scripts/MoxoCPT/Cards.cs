using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MoxoCPT
{
    public class Cards : MonoBehaviour
    {
        public Cards instance;
        
        // there are 6 (0.5f), 3(1f), and 1(4f), making the probabilities, 60%, 30%, and 10% respectively.
        public float[] cardDuration = {0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f, 1f, 1f, 4f};
        
        [HideInInspector] public Transform curCard;
        [HideInInspector] public Transform[] cardArr;

        [HideInInspector] public int numCards;
        [HideInInspector] public int numDurations;


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(instance);
        }

        private void Start()
        {
            cardArr = GetComponentsInChildren<Transform>().Where(child =>
                child.gameObject.tag == "Target" || child.gameObject.tag == "NonTarget").ToArray();

            numCards = cardArr.Length;
            numDurations = cardDuration.Length;
        }

        public void UpdateCurCard(Transform newCard)
        {
            curCard = newCard;
        }

        public void TurnAllCardsOff()
        {
            foreach (var card in cardArr) card.gameObject.SetActive(false);
        }
    }
}

