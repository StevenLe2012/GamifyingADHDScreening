using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MoxoCPT
{
    public class ChangeShapes : MonoBehaviour
    {
        [SerializeField] private float _secondsTillGameStarts = 5.0f;

        private Cards _cards;

        private Dictionary<int, int> _counts;

        // change this number for how many total stimuli trials are needed.
        private const int NUM_TRIALS = 59;
        private const int MAX_NUM_NONTARGET = 5;


        private void Awake()
        {
            _cards = GetComponent<Cards>();
        }

        private void Start()
        {
            _counts = new Dictionary<int, int>();
            
            //_cards.TurnAllCardsOff();

            StartCoroutine("Change");
        }

        IEnumerator Change()
        {
            // for the first card
            yield return new WaitForSeconds(_secondsTillGameStarts);
            var newCard = GetNextObj();
            TurnCardOn(newCard);
            _cards.UpdateCurCard(newCard);
            
            for (var i = 1; i < NUM_TRIALS; i++)
            {
                var duration = GetCardDuration();
                
                yield return new WaitForSeconds(duration);
                newCard = GetNextObj();
                TurnCardOff(_cards.curCard);
                
                yield return new WaitForSeconds(duration);
                TurnCardOn(newCard);
                _cards.UpdateCurCard(newCard);
            }
            
            TurnCardOff(_cards.curCard);
            
            

            foreach (var num in _counts) Debug.Log(num);
        }

        private Transform GetNextObj()
        {
            var index = Random.Range(0, _cards.numCards);
            
            AddToDict(index);
            
            return _cards.cardArr[index];
        }

        private void AddToDict(int index)
        {
            // a lot of edge case checking to make sure not to access invalid dictionary index
            if (!_counts.ContainsKey(index)) _counts.Add(index, 1);
            else
            {
                while (index != 0 && _counts[index] >= MAX_NUM_NONTARGET)
                {
                    index = Random.Range(0, _cards.numCards);
                    if (!_counts.ContainsKey(index)) _counts.Add(index, 0);
                }

                _counts[index] += 1;
            }
        }

        private float GetCardDuration()
        {
            return _cards.cardDuration[Random.Range(0, _cards.numDurations)];
        }

        private void TurnCardOn(Transform card)
        {
            card.gameObject.SetActive(true);
            _cards.isActive = true;
        }

        private void TurnCardOff(Transform card)
        {
            card.gameObject.SetActive(false);
            _cards.isActive = false;
        }
    }
}

