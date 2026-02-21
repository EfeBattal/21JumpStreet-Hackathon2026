using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{

    [Header("Table Setup")]
    public float dealSpeed = 0.1f;
    public float spinSpeed = 50f;

    [Header("Offset Settings")]
    private float cardWidth = 0.1f;
    private float yStackOffset = 0.007f;

    [Header("Game Participants")]
    public User dealer;
    public User player;

    [Header("Card Maker")]
    public GameObject cardDeck;


    public GameObject[] masterDeck;

    // current cards in the deck
    private List<GameObject> workingDeck = new List<GameObject>();

    private int currBet;

    private void Start()
    {
        workingDeck.Clear();
        FillWorkingDeck();
        ShuffleDeck();
        currBet = 0;
        StartCoroutine(StartGameSequence());

    }

    private void FillWorkingDeck()
    {
        foreach(GameObject card in masterDeck)
        {
            workingDeck.Add(card);
            workingDeck.Add(card);
        }
    }



    private void ShuffleDeck()
    {
        // Fisher-Yates shuffle algorithm
        for (int i = workingDeck.Count - 1; i > 0; i--)
        {
            // Pick a random index from 0 to i
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            // Swap the cards
            GameObject tempCard = workingDeck[i];
            workingDeck[i] = workingDeck[randomIndex];
            workingDeck[randomIndex] = tempCard;
        }
    }

    public GameObject PopFromDeck()
    {
        int lastIndex = workingDeck.Count - 1;
        GameObject cardToDeal = workingDeck[lastIndex];
        workingDeck.RemoveAt(lastIndex);
        return cardToDeal;

    }

    public void DealCardTo(User targetUser, bool isFaceUp)
    {
        GameObject cardPrefab = PopFromDeck();
        Vector3 finalPosition = CalculateCardPosition(targetUser);
        GameObject spawnedCard = Instantiate(cardPrefab, cardDeck.GetComponent<Transform>().position, Quaternion.Euler(90, 0, 90));
        targetUser.cardsInHand++;
        targetUser.currentCards.Add(spawnedCard);
        StartCoroutine(AnimateCard(spawnedCard, finalPosition, isFaceUp));
    }

    private System.Collections.IEnumerator AnimateCard(GameObject card, Vector3 destination, bool isFaceUp)
    {

        Quaternion finalRotation = isFaceUp ? Quaternion.Euler(-90, 0, 90) : Quaternion.Euler(90, 0, 90);

        while(Vector3.Distance(card.transform.position, destination) > 0.01f)
        {
            card.transform.position = Vector3.MoveTowards(card.transform.position, destination, dealSpeed * Time.deltaTime);
            
            card.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            yield return null;
        }

        card.transform.position = destination;
        card.transform.rotation = finalRotation;
    }

    public Vector3 CalculateCardPosition(User targetUser)
{
    float horizontalOffset = cardWidth * 0.75f; 
    int cardsInHand = targetUser.cardsInHand;

    float totalZShift = horizontalOffset * cardsInHand;
    float totalYShift = yStackOffset * cardsInHand;

    Vector3 finalPosition = targetUser.handAnchor + new Vector3(0, totalYShift, totalZShift);

    return finalPosition;
}

    public int[] SumCards(User targetUser)
    {
        int[] results = new int[2];
        bool isThereAnAce = false;
        foreach(GameObject card in targetUser.currentCards)
        {
            if(card.GetComponent<CardData>().isAce == true && !isThereAnAce)
            {
                results[0] += 1;
                results[1] += 11;
                isThereAnAce = true;
            } else
            {
                results[0] += card.GetComponent<CardData>().value;
                results[1] += card.GetComponent<CardData>().value;
            }
        }
        return results;
    }

    public System.Collections.IEnumerator StartGameSequence()
    {
        DealCardTo(player, true);
        yield return new WaitForSeconds(0.5f);

        DealCardTo(dealer, true);
        yield return new WaitForSeconds(0.5f);

        DealCardTo(player, true);
        yield return new WaitForSeconds(0.5f);

        DealCardTo(dealer, false);
    }

    public int GetBestScore(int[] scores)
{
    if (scores[1] <= 21) 
    {
        return scores[1];
    }
    return scores[0]; 
}

    public void OnHitPressed()
    {
        DealCardTo(player, true);
        int currentScore = GetBestScore(SumCards(player));

        if(currentScore > 21)
        {
            Debug.Log("Player Busted with " + currentScore + "!");
        }
    }
    public void OnStandPressed()
    {
        StartCoroutine(DealerTurnSequence());
    }
    private System.Collections.IEnumerator DealerTurnSequence()
    {
        // 1. Reveal the dealer's hidden card (the second one dealt)
        if (dealer.currentCards.Count > 1)
        {
            // Using the face-up rotation from our earlier fix
            dealer.currentCards[1].transform.rotation = Quaternion.Euler(-90, 0, 90); 
        }

        yield return new WaitForSeconds(1.0f); // Pause for dramatic effect

        int dealerScore = GetBestScore(SumCards(dealer));

        // 2. Dealer must hit on 16 or lower
        while (dealerScore < 17)
        {
            DealCardTo(dealer, true);
            yield return new WaitForSeconds(1.2f); // Wait for the animation to finish
            dealerScore = GetBestScore(SumCards(dealer));
        }

        // 3. Determine the Winner
        int playerScore = GetBestScore(SumCards(player));
        
        if (dealerScore > 21) 
        {
            Debug.Log("Dealer Busts! Player Wins!");
        } 
        else if (playerScore > dealerScore) 
        {
            Debug.Log("Player Wins! " + playerScore + " to " + dealerScore);
        } 
        else if (dealerScore > playerScore) 
        {
            Debug.Log("Dealer Wins! " + dealerScore + " to " + playerScore);
        } 
        else 
        {
            Debug.Log("Push! It's a tie.");
        }

        Invoke("ResetRound", 4.0f);
    }

    public void ResetRound()
{
    // 1. Destroy the physical 3D card models on the table
    foreach (GameObject card in player.currentCards)
    {
        Destroy(card);
    }
    foreach (GameObject card in dealer.currentCards)
    {
        Destroy(card);
    }

    // 2. Clear the data lists so the math resets
    player.currentCards.Clear();
    player.cardsInHand = 0;

    dealer.currentCards.Clear();
    dealer.cardsInHand = 0;

    // 3. Deck Management: If the shoe is running low, reshuffle!
    // (A standard casino reshuffles when the cut card is reached, usually ~20% of the shoe)
    if (workingDeck.Count < 20) 
    {
        Debug.Log("Reshuffling the shoe!");
        workingDeck.Clear();
        FillWorkingDeck();
        ShuffleDeck();
    }

    // 4. Trigger the opening deal again
    StartCoroutine(StartGameSequence());
}

