using UnityEngine;
using System.Collections.Generic;

public class User : MonoBehaviour
{
    [Header("Hand Setup")]
    public int cardsInHand = 0;
    public Vector3 handAnchor;
    public List<GameObject> currentCards = new List<GameObject>();

    private const int STARTING_MONEY = 50;

    public int currMoney = STARTING_MONEY;



    public void ResetHand()
    {
        cardsInHand = 0;
        currentCards.Clear();
    }

}
