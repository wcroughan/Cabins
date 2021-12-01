using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    GameObject[] playerGameObjects;

    private int currentPlayerObjectIndex;

    GameStateManager gameStateManager;

    // Start is called before the first frame update
    void Start()
    {
        if (playerGameObjects.Length == 0)
            throw new MissingReferenceException("No player objects supplied to Player Manager");

        gameStateManager = GameStateManager.Instance;
        gameStateManager.OnGameStateChanged += OnGameStateChanged;

        foreach (GameObject gameObject in playerGameObjects)
            gameObject.SetActive(false);
        currentPlayerObjectIndex = 0;
    }

    void OnGameStateChanged(GameStateManager.GameState gameState)
    {
        Debug.Log(gameState);
        playerGameObjects[0].SetActive(true);
    }

    public void TransitionToNextPlayerObject()
    {
        Debug.Log("In transition function ");
        playerGameObjects[currentPlayerObjectIndex].SetActive(false);
        currentPlayerObjectIndex++;

        if (currentPlayerObjectIndex == playerGameObjects.Length)
        {
            Debug.Log("Finished with last player object, restarting from 0");
            currentPlayerObjectIndex = 0;
        }

        Debug.Log("Transitioning to playerobject " + currentPlayerObjectIndex);
        playerGameObjects[currentPlayerObjectIndex].SetActive(true);
    }

}
