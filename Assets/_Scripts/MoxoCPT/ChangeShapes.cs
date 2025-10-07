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

        private Report _report;

        private Dictionary<int, int> _counts;

        // change this number for how many total stimuli trials are needed.
        private const int NUM_TRIALS = 59;
        private const int MAX_NUM_NONTARGET = 5;

        private void Start()
        {
            _counts = new Dictionary<int, int>();
            _report = new Report();

            StartCoroutine("Change");
        }

        IEnumerator Change()
        {
            // for the first card
            yield return new WaitForSeconds(_secondsTillGameStarts);
            var newCard = GetNextObj();
            TurnCardOn(newCard);
            Cards.Instance.UpdateCurCard(newCard);
            
            for (var i = 1; i < NUM_TRIALS; i++)
            {
                var duration = GetCardDuration();
                
                _report.ResetReport();
                StartCoroutine(Interact.StartReport(_report, duration * 2));
                yield return new WaitForSeconds(duration);
                newCard = GetNextObj();
                TurnCardOff(Cards.Instance.curCard);
                
                yield return new WaitForSeconds(duration);
                TurnCardOn(newCard);
                Cards.Instance.UpdateCurCard(newCard);
            }
            
            TurnCardOff(Cards.Instance.curCard);


            foreach (var num in _counts) Debug.Log(num);
        }

        private Transform GetNextObj()
        {
            var index = Random.Range(0, Cards.Instance.numCards);
            
            AddToDict(index);
            
            return Cards.Instance.cardArr[index];
        }

        private void AddToDict(int index)
        {
            // a lot of edge case checking to make sure not to access invalid dictionary index
            if (!_counts.ContainsKey(index)) _counts.Add(index, 1);
            else
            {
                while (index != 0 && _counts[index] >= MAX_NUM_NONTARGET)
                {
                    index = Random.Range(0, Cards.Instance.numCards);
                    if (!_counts.ContainsKey(index)) _counts.Add(index, 0);
                }

                _counts[index] += 1;
            }
        }

        private float GetCardDuration()
        {
            return Cards.Instance.cardDuration[Random.Range(0, Cards.Instance.numDurations)];
        }

        private void TurnCardOn(Transform card)
        {
            card.gameObject.SetActive(true);
            Cards.Instance.isActive = true;
        }

        private void TurnCardOff(Transform card)
        {
            card.gameObject.SetActive(false);
            Cards.Instance.isActive = false;
        }
    }
}

