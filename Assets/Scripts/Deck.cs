using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Deck : MonoBehaviour
{
    
    public Sprite[] faces; //imagenes de las 52 cartas
    public GameObject dealer; //el croupier ---> esta en la clase cardhand
    public GameObject player; //el jugador ----> esta en la clase cardhand

    //------botones --------

    public Button hitButton; //bton para pedir cartas
    public Button stickButton; //boton para quedarte con las que tienes, luego es el turno del croupier
    public Button playAgainButton; //boton para jugar otra partida
    public Button resetGameButton; //boton para reiniciar

    //------mensajes del juego ------

    public TMP_Text finalMessage; //si ganaste o perdiste
    public TMP_Text probMessage; //muestra las probabilidades

    //------mensaje para la apuesta del player------

    public TMP_Text creditText; //fichas actuales del jugador
    public TMP_Dropdown betDropdown; //muestra lo que el jugador puede apostar 100-500-1000 fichas

    //------ vaor de la mano ------

    public TMP_Text dealerPointsText; //puntos del croupier
    public TMP_Text playerPointsText; //puntos del player

    //------ otras variables ------

    public int[] values = new int[52]; //valor de las cartas
    int cardIndex = 0;

    int credit = 1000; //credito - fichas
    int currentBet = 10; //por default tiene la apuesta mas baja
    bool roundFinished = false;

    //-------------------------------------------------------------------------------------------//

    private void Awake()
    {
        InitCardValues();
    }

    private void Start()
    {
        UpdateCreditUI(); //actualiza el credito al inicio de una partida
        UpdateBetFromDropdown(); //aqui leemos la apuesta realizada
        ShuffleCards(); //barajea las cartas
        StartGame(); //comenzamos la partida
    }
    //------------------------------------------------------//
    private void InitCardValues()
    //esto es para darle valor a las cartas
    {
        for (int i = 0; i < 52; i++) //entre las 52 cartas
        {
            int rank = (i % 13) + 1; //asignamos rangos del 1 al 13

            if (rank == 1) //este es un as
                values[i] = 11; //tiene el valor maximo que puede tener un as

            else if (rank >= 11 && rank <= 13) //este si es una figura
                values[i] = 10; //todas las fiuras valen 10

            else //luego si es una carta normal
                values[i] = rank; //es el mismo valor que el rango de la carta
        }
    }
    //------------------------------------------------------//
    private void ShuffleCards()
    //esto es para desorganizarlas/barajearlas
    {
        for (int i = 0; i < 52; i++) //recorre todas las cartas del 0 al 52
        {
            int rnd = Random.Range(i, 52); //guardamos el numero de otra carta al azar de la baraja

            //intercambia las cartas
            Sprite tempFace = faces[i]; 
            faces[i] = faces[rnd];
            faces[rnd] = tempFace;

            //intercabia los valores
            int tempValue = values[i];
            values[i] = values[rnd];
            values[rnd] = tempValue;

        }
    }
    //------------------------------------------------------//
    void StartGame()
    //esto es el inicio del juego, cuando se reparten las cartas 
    // y se comprueba si tenemos blackjack
    {
        roundFinished = false;
        finalMessage.text = "";

        if (betDropdown != null)
            betDropdown.interactable = false;

        //se reparten dos cartas al jugador y al coupier
        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        UpdatePointsUI(); //importante, se tienen que actualizar los puntos
        CalculateProbabilities(); //se calculan las probabilidades actuales

        //guardamos los puntos del player y el coupier
        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;

        //si alguno llego a 21 al iniciar la partida, blackjack
        if (playerPoints == 21 || dealerPoints == 21)
        {
            dealer.GetComponent<CardHand>().InitialToggle();
            UpdatePointsUI();

            //si los dos tienen 21
            if (playerPoints == 21 && dealerPoints == 21)
            {
                finalMessage.text = "DRAW";
            }
            //si nosotros (el jugador) tiene 21 y ganamos
            else if (playerPoints == 21)
            {
                finalMessage.text = "PLAYER BLACKJACK!";
                credit += currentBet;
            }
            //si el coupier tiene 21  y gana
            else
            {
                credit -= currentBet;

                if (credit < 0)
                    finalMessage.text = "PERDISTE MAS CREDITOS DE LOS QUE TENIAS";
                else
                    finalMessage.text = "DEALER BLACKJACK!";
            }

            EndRound(); //se acaba la partida apenas empezo
        }
    }
    //------------------------------------------------------//
    private void CalculateProbabilities()
    //aqui se calculan y se muestran las tres probabilidades a calcular
    {
        int remainingCards = 52 - cardIndex;

        if (remainingCards <= 0)
        {
            probMessage.text = "Deal > Play: 0.0000\n17<=x<=21: 0.0000\nx > 21: 0.0000";
            return;
        }

        CardHand dealerHand = dealer.GetComponent<CardHand>();
        CardHand playerHand = player.GetComponent<CardHand>();

        int playerPoints = playerHand.points;

        int visibleDealerValue = 0;

        if (dealerHand.cards.Count >= 2)
            visibleDealerValue = dealerHand.cards[1].GetComponent<CardModel>().value;
        else if (dealerHand.cards.Count == 1)
            visibleDealerValue = dealerHand.cards[0].GetComponent<CardModel>().value;

        bool hiddenCardCovered = false;

        if (dealerHand.cards.Count > 0)
        {
            CardModel firstDealerCard = dealerHand.cards[0].GetComponent<CardModel>();
            SpriteRenderer sr = dealerHand.cards[0].GetComponent<SpriteRenderer>();
            hiddenCardCovered = sr.sprite == firstDealerCard.cardBack;
        }

        int dealerBetter = 0;
        int player17to21 = 0;
        int playerOver21 = 0;

        for (int i = cardIndex; i < 52; i++)
        {
            int nextCard = values[i];

            int simulatedDealer;

            if (hiddenCardCovered)
                simulatedDealer = AddCardToTotal(visibleDealerValue, nextCard);
            else
                simulatedDealer = AddCardToTotal(dealerHand.points, nextCard);

            if (simulatedDealer > playerPoints)
                dealerBetter++;

            int simulatedPlayer = AddCardToTotal(playerPoints, nextCard);

            if (simulatedPlayer >= 17 && simulatedPlayer <= 21)
                player17to21++;

            if (simulatedPlayer > 21)
                playerOver21++;
        }

        float pDealer = (float)dealerBetter / remainingCards;
        float p17to21 = (float)player17to21 / remainingCards;
        float pOver21 = (float)playerOver21 / remainingCards;

        probMessage.text =
            "Deal > Play: " + pDealer.ToString("F4") + "\n" +
            "17<=x<=21: " + p17to21.ToString("F4") + "\n" +
            "x > 21: " + pOver21.ToString("F4");
    }

    //------------------------------------------------------//
    private int AddCardToTotal(int total, int cardValue)
    //
    {
        int result = total + cardValue;

        if (cardValue == 11 && result > 21)
            result -= 10;

        return result;
    }
    //------------------------------------------------------//

        private void UpdatePointsUI()
    //actualizar los puntos de la carta de tu mano
    {
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        CardHand playerHand = player.GetComponent<CardHand>();

        if (playerPointsText != null)
            playerPointsText.text = "Puntos: " + playerHand.points.ToString();

        if (dealerPointsText != null)
        {
            int visiblePoints = 0;

            //comprobamos si hay cartas
            if (dealerHand.cards.Count > 0)
            {
                //miramos si la primera carta esta volteada
                CardModel firstCard = dealerHand.cards[0].GetComponent<CardModel>();
                SpriteRenderer sr = dealerHand.cards[0].GetComponent<SpriteRenderer>();

                bool hidden = sr.sprite == firstCard.cardBack;

                //si esta oculta, no contamos la primera carta
                if (hidden)
                {
                    for (int i = 1; i < dealerHand.cards.Count; i++)
                    {
                        visiblePoints += dealerHand.cards[i].GetComponent<CardModel>().value;
                    }
                }
                else
                {
                    //si no esta oculta, usamos los puntos normales
                    visiblePoints = dealerHand.points;
                }
            }

            dealerPointsText.text = "Puntos: " + visiblePoints.ToString();
        }
    }
    //------------------------------------------------------//
    void PushDealer()
    //roba una carta de la baraja
    {
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        UpdatePointsUI();
    }
    //------------------------------------------------------//
    void PushPlayer()
    //soba una carta de la baraja
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        UpdatePointsUI();
        CalculateProbabilities();
    }
    //------------------------------------------------------//
    public void Hit()
    {
        if (roundFinished)
            return;

        dealer.GetComponent<CardHand>().InitialToggle();
        UpdatePointsUI();

        PushPlayer();

        int playerPoints = player.GetComponent<CardHand>().points;

        if (playerPoints > 21)
        {
            credit -= currentBet;

            if (credit < 0)
                finalMessage.text = "QUEDASTE EN DEUDA";
            else
                finalMessage.text = "PLAYER LOSE";

            EndRound();
        }
    }
    //------------------------------------------------------//
    public void Stand()
    //esto es cuando el jugador se queda con lo que tiene, le toca al dealer
    //y se define quien gano
    {
        if (roundFinished)
            return;

        dealer.GetComponent<CardHand>().InitialToggle();
        UpdatePointsUI();

        while (dealer.GetComponent<CardHand>().points <= 16)
        {
            PushDealer();
        }

        int dealerPoints = dealer.GetComponent<CardHand>().points;
        int playerPoints = player.GetComponent<CardHand>().points;

        if (dealerPoints > 21)
        {
            finalMessage.text = "PLAYER WIN";
            credit += currentBet;
        }
        else if (dealerPoints > playerPoints)
        {
            credit -= currentBet;

            if (credit < 0)
                finalMessage.text = "PERDISTE MAS CREDITOS DE LOS QUE TENIAS";
            else
                finalMessage.text = "PLAYER LOSE";
        }
        else if (dealerPoints < playerPoints)
        {
            finalMessage.text = "PLAYER WIN";
            credit += currentBet;
        }
        else
        {
            finalMessage.text = "DRAW";
        }

        EndRound();
    }
    //------------------------------------------------------//
    public void PlayAgain()
    //hace que el jugador pueda jugar la sigueinte ronda
    {
        if (credit <= 0)
        {
            finalMessage.text = "NO CREDIT";
            return;
        }

        hitButton.interactable = true;
        stickButton.interactable = true;
        roundFinished = false;

        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();

        if (dealerPointsText != null)
            dealerPointsText.text = "Puntos: 0";

        if (playerPointsText != null)
            playerPointsText.text = "Puntos: 0";

        cardIndex = 0;
        UpdateBetFromDropdown();
        ShuffleCards();
        StartGame();
    }
    //------------------------------------------------------//
    private void EndRound()
    //hace que todo no sea interactuable y que el jugador 
    // haga su apuesta para la siguiente partida
    {
        roundFinished = true;
        hitButton.interactable = false;
        stickButton.interactable = false;
        UpdateCreditUI();

        if (betDropdown != null)
            betDropdown.interactable = true;
    }
    //------------------------------------------------------//
    private void UpdateCreditUI()
    //se actualiza la cantidad de fichas que le quedan al jugador
    {
        if (creditText != null)
            creditText.text = "Créditos: " + credit.ToString();
    }
    //------------------------------------------------------//
    public void UpdateBetFromDropdown()
    //se toma la cantidad de fichas seleccionada para la apuesta
    {
        if (betDropdown == null)
            return;

        string selected = betDropdown.options[betDropdown.value].text;
        selected = selected.Replace("Credits", "").Replace("Credit", "").Trim();

        int parsedBet;

        if (int.TryParse(selected, out parsedBet))
        {
            currentBet = parsedBet; // permite apostar mas de lo que tienes
        }
    }
    //------------------------------------------------------//
    public void ResetGame()
    //reinicia todo el juego desde cero y creditos == 1000
    {
        //se reinician los creditos
        credit = 1000;

        //reiniciar variables
        roundFinished = false;
        cardIndex = 0;

        //reactivar botones
        hitButton.interactable = true;
        stickButton.interactable = true;

        //las manos de nuevo con 0 cartas
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();

        //reiniciamos textos
        if (dealerPointsText != null)
            dealerPointsText.text = "Puntos: 0";

        if (playerPointsText != null)
            playerPointsText.text = "Puntos: 0";

        if (finalMessage != null)
            finalMessage.text = "";

        if (probMessage != null)
            probMessage.text = "";

        //se actualiza el credito en pantalla
        UpdateCreditUI();

        //y podemos elegir la apuesta de nuevo
        if (betDropdown != null)
            betDropdown.interactable = true;

        //barajamos y empezamos de nuevo
        ShuffleCards();
        UpdateBetFromDropdown();
        StartGame();
    }
    //------------------------------------------------------//

}