using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIController : MonoBehaviour
{
    public SquareGrid grid;              // Viite ruudukkoon
    public int numberOfAIEmpires = 50;      // vihujen määrä

    public GameObject aiManagerPrefab;   // Prefab, jossa on AIManager-komponentti

    private List<AIManager> aiManagers = new List<AIManager>();

  
    public static AIController Instance { get; private set; }

    void Awake() { 
        if (Instance != null && Instance != this) 
        { Destroy(gameObject); return; } 
        Instance = this; 
        DontDestroyOnLoad(gameObject);
    
    }


    public void InitializeAIManagers()
    {
        foreach (Transform child in transform)
        {
            AIManager existingAI = child.GetComponent<AIManager>();
            if (existingAI != null)
            {
                Destroy(child.gameObject);
            }
        }
        aiManagers.Clear();

        for (int i = 0; i < numberOfAIEmpires; i++)
        {
            GameObject aiGO = Instantiate(aiManagerPrefab, transform);
            AIManager aiManager = aiGO.GetComponent<AIManager>();
            if (aiManager != null)
            {
                aiManager.grid = grid;
                aiManagers.Add(aiManager);
            }
        }
    }



    // Kutsu tätä metodia vuoron lopussa, jolloin kaikki AI-managerit suorittavat vuoronsa
    public void ProcessAllAITurns(int currentTurn)
    {
        foreach (var aiManager in aiManagers.ToList())
        {
            if (aiManager != null)
            {
                aiManager.ProcessAITurn(currentTurn);
            }
            else
            {
                aiManagers.Remove(aiManager);
            }
        }
    }

    public void RemoveAIManagerForNation(Nation nation)
    {
        AIManager managerToRemove = aiManagers.FirstOrDefault(m => m.aiNation == nation);
        if (managerToRemove != null)
        {
            managerToRemove.MarkForRemoval();
            aiManagers.Remove(managerToRemove);
            Destroy(managerToRemove.gameObject);
            //Debug.Log($"Tekoälyvastustaja ({nation.EmpireColor}) poistettu pelistä!");
        }
    }

    public void RegisterAIManager(AIManager manager)
    {
        aiManagers.Add(manager);
        //Debug.Log("Uusi AIManager rekisteröity tekoälylistaa");
    }
}
