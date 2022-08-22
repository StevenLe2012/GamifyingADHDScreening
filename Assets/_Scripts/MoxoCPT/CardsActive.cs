using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoxoCPT
{
    public class CardsActive : MonoBehaviour
    {
        [SerializeField] private Cards _cards;
        
        public void SetCardsActive(bool active)
        {
            _cards.gameObject.SetActive(active);
        }
    }
}

