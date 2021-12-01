using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState { Loading, Playing }
    public GameState gameState { get; private set; }

    public System.Action<GameState> OnGameStateChanged;

    public static GameStateManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        gameState = GameState.Loading;
        StartCoroutine(CheckIfTerrainLoaded());
    }

    IEnumerator CheckIfTerrainLoaded()
    {
        while (!EndlessTerrainV2.hasAnyTerrainCollider)
            yield return new WaitForSeconds(1);

        gameState = GameState.Playing;
        if (OnGameStateChanged != null)
            OnGameStateChanged(gameState);
    }
}