using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

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

    [Header("UI Text Reference")]
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI betText;
    public GameObject startUIContainer;
    public GameObject actionUIContainer;
    public GameObject betUIContainer;
    public GameObject EndUIContainer;
    public TextMeshProUGUI endingMessage;

    [Header("Audio Effects")]
    public AudioSource sfxAudioSource;
    public AudioClip hitSound;
    public AudioClip blackjackSound;
    public AudioClip dealerBustSound;
    public AudioClip playerLostSound;

    public GameObject[] masterDeck;
    private const int TOTAL_CARD_NUM = 104;

    private bool isActionAllowed = false;

    private List<GameObject> workingDeck = new List<GameObject>();

    private int currentBet;
    private string handResult;

    private void Start()
    {
        workingDeck.Clear();
        FillWorkingDeck();
        ShuffleDeck();
        currentBet = 0;
        handResult = "LOSS";
        actionUIContainer.SetActive(false);
        betUIContainer.SetActive(true);
        startUIContainer.SetActive(true);
        EndUIContainer.SetActive(false);
        UpdateUI();
    }
    public void StartGame()
    {
        startUIContainer.SetActive(false);
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
        for (int i = workingDeck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

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
            if(card.GetComponent<CardData>().isAce)
            {
                isThereAnAce = true;
            }
            
            results[0] += card.GetComponent<CardData>().value;
        }
        results[1] = results[0];
        if(isThereAnAce)
        {
            results[1] += 10;
        }
        return results;
    }

    public System.Collections.IEnumerator StartGameSequence()
    {

        betUIContainer.SetActive(false);
        player.currMoney -= currentBet;
        UpdateUI();

        DealCardTo(player, true);
        yield return new WaitForSeconds(0.5f);

        DealCardTo(dealer, true);
        yield return new WaitForSeconds(0.5f);

        DealCardTo(player, true);
        yield return new WaitForSeconds(0.5f);

        DealCardTo(dealer, false);
        yield return new WaitForSeconds(0.5f);

        actionUIContainer.SetActive(true);
        isActionAllowed = true;

        checkStatus();

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
        if(!isActionAllowed) return;
        sfxAudioSource.PlayOneShot(hitSound);
        actionUIContainer.SetActive(false);
        isActionAllowed = false;

        StartCoroutine(HitSequence());
    }
    public void OnDoublePressed()
    {
        if(!isActionAllowed) return;

        if(currentBet <= player.currMoney)
        {
            player.currMoney -= currentBet;
            currentBet *= 2;
            UpdateUI();
            
            StartCoroutine(DoubleDownSequence());
        }
        
    }
    public void OnStandPressed()
    {
        if(!isActionAllowed) return;
        actionUIContainer.SetActive(false);
        isActionAllowed = false;
        StartCoroutine(DealerTurnSequence());
    }
    private System.Collections.IEnumerator DealerTurnSequence()
    {
        yield return new WaitForSeconds(1f);

        if (dealer.currentCards.Count > 1)
        {
            dealer.currentCards[1].transform.rotation = Quaternion.Euler(-90, 0, 90); 
        }

        yield return new WaitForSeconds(1.0f);

        int dealerScore = GetBestScore(SumCards(dealer));
        int playerScore = GetBestScore(SumCards(player));

        if(playerScore <= 21)
        {
            while (dealerScore < 17)
            {
                DealCardTo(dealer, true);
                yield return new WaitForSeconds(1.2f);
                dealerScore = GetBestScore(SumCards(dealer));
            }
        }
        

        if(playerScore > 21)
        {
            handResult = "LOST";
        } else if(dealerScore > 21)
        {
            if(playerScore == 21) { handResult = "BLACKJACK"; } 
            else
            {
                handResult = "BUST";
            }
        } else
        {
            if(playerScore > dealerScore)
            {
                if(playerScore == 21)
                {
                    handResult = "BLACKJACK";
                } else
                {
                    handResult = "WIN";
                }
            } else if(playerScore == dealerScore)
            {
                handResult = "PUSH";
            } else
            {
                handResult = "LOSS";
            }
        }

    
        ResolveHand();
        UpdateUI();
        ShowEndUI();
    }

    public void ResetRound()
{
    foreach (GameObject card in player.currentCards)
    {
        Destroy(card);
    }
    foreach (GameObject card in dealer.currentCards)
    {
        Destroy(card);
    }

    player.currentCards.Clear();
    player.cardsInHand = 0;

    dealer.currentCards.Clear();
    dealer.cardsInHand = 0;

    if (workingDeck.Count < (0.2*TOTAL_CARD_NUM))
    {
        workingDeck.Clear();
        FillWorkingDeck();
        ShuffleDeck();
    }
}

    public void AddToBet(int amount)
    {
        if (player.currMoney >= currentBet + amount)
        {
            currentBet += amount;
            UpdateUI();
        } else
        {
            print("Insufficient funds");
        }
    }

    public void ResolveHand()
    {
        if (handResult == "WIN")
        {
            player.currMoney += (currentBet * 2);
            endingMessage.text = $"You WIN!\n You Won: ${currentBet}";
        
        } else if (handResult == "PUSH")
        {   
            player.currMoney += currentBet;
            endingMessage.text = $"It's a PUSH.\n";
        } else if(handResult == "BLACKJACK")
        {
            player.currMoney += (int)(currentBet * 2.5);
            endingMessage.text = $"BLACKJACK!\n You Won: ${currentBet*2.5}";
            sfxAudioSource.PlayOneShot(blackjackSound);
        } else if(handResult == "BUST")
        {
            player.currMoney += (currentBet * 2);
            sfxAudioSource.PlayOneShot(dealerBustSound);
            endingMessage.text = $"Dealer BUST!!!!\n You Won: ${currentBet}";
        } 
        else
        {
            endingMessage.text = "You Lost...\n";
            sfxAudioSource.PlayOneShot(playerLostSound);
        }
        
    }

    private void UpdateUI()
    {
        balanceText.text = $"Balance:\n${player.currMoney}";
        betText.text = $"Current Bet:\n${currentBet}";
    }

    private void ShowEndUI()
    {
        startUIContainer.SetActive(false);
        betUIContainer.SetActive(true);
        actionUIContainer.SetActive(false);
        EndUIContainer.SetActive(true);

        if (player.currMoney <= 0)
        {
            endingMessage.text += "\n\nBANKRUPT!\nPress Restart for a new loan.";
        }
    }

    public void RestartRound()
    {
        if (player.currMoney <= 0)
        {
            player.currMoney = 50;
            currentBet = 0;
            
            EndUIContainer.SetActive(false);
            actionUIContainer.SetActive(false);
            betUIContainer.SetActive(true);
            startUIContainer.SetActive(true);
            
            ResetRound();
            UpdateUI();
        }
        else if (currentBet <= player.currMoney)
        {
            actionUIContainer.SetActive(false);
            EndUIContainer.SetActive(false);
            startUIContainer.SetActive(false);
            
            ResetRound();
            StartCoroutine(StartGameSequence());
        }
        else
        {
            currentBet = 0;
            EndUIContainer.SetActive(false);
            actionUIContainer.SetActive(false);
            betUIContainer.SetActive(true);
            startUIContainer.SetActive(true);
            
            ResetRound();
            UpdateUI();
        }
    }

    public void ResetBet()
    {
        currentBet = 0;
        UpdateUI();
    }

    public void checkStatus()
    {
        int currentScore = GetBestScore(SumCards(player));
        if((currentScore > 21) || (player.cardsInHand == 2 && currentScore == 21))
        {
            isActionAllowed = false;
            StartCoroutine(DealerTurnSequence());
        }
    }

    private System.Collections.IEnumerator DoubleDownSequence()
    {
        DealCardTo(player, true);
        
        yield return new WaitForSeconds(1.0f); 
        
        int currentScore = GetBestScore(SumCards(player));
        if (currentScore <= 21) 
        {
            StartCoroutine(DealerTurnSequence());
        } 
        else 
        {
            checkStatus(); 
        }
    }

    private System.Collections.IEnumerator HitSequence()
    {
        DealCardTo(player, true);

        yield return new WaitForSeconds(0.5f);

        actionUIContainer.SetActive(true);
        isActionAllowed = true;

        checkStatus();
    }

}

