using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FishType
{
    Friendly,
    Shy,
    Legend
}

public class FishConfig
{
    public FishType Type;
    public float NibbleChance;
    public float MinDistance;
    public float MaxDistance;

    public float PullTimerMax;
    public float PullTimerStartMax;
    public float PullTimer;
    public float PullForceMax;
    public float PullForceMin;
    public bool Pulling;

    public static FishConfig GetFor(FishType type)
    {
        FishConfig cfg = new FishConfig();
        cfg.Type = type;

        if (type == FishType.Friendly)
        {
            cfg.NibbleChance = 0.8f;
            cfg.MinDistance = 0f;

            cfg.PullForceMax = 0.9f;
            cfg.PullForceMin = 0.3f;

            cfg.PullTimerStartMax = 1f;
            cfg.PullTimerMax = 2f;

            cfg.PullTimer = 0f;
        }
        else if (type == FishType.Shy)
        {
            cfg.NibbleChance = 0.6f;
            cfg.MinDistance = 90f;

            cfg.PullForceMax = 1.2f;
            cfg.PullForceMin = 0.6f;

            cfg.PullTimerStartMax = 1.5f;
            cfg.PullTimerMax = 2f;

            cfg.PullTimer = 0f;
        }
        else if (type == FishType.Legend)
        {
            cfg.NibbleChance = 0.5f;
            cfg.MinDistance = 20f;

            cfg.PullForceMax = 2f;
            cfg.PullForceMin = 1.5f;

            cfg.PullTimerStartMax = 1.5f;
            cfg.PullTimerMax = 3f;

            cfg.PullTimer = 0f;
        }

        return cfg;
    }
}

public class Fishes : MonoBehaviour
{
    public GameObject FriendlyFishPrefab;
    public GameObject ShyFishPrefab;
    public GameObject LegendFishPrefab;

    public Transform ShyFishArea;
    public Transform LegendFishArea;


    public Collider ShyFishAreaCollider => ShyFishArea.GetComponent<Collider>();
    public Collider LegendFishAreaCollider => LegendFishArea.GetComponent<Collider>();

    public GameObject GetPrefabOfFishType(FishType type)
    {
        switch (type)
        {
            case FishType.Friendly:
                return FriendlyFishPrefab;
            case FishType.Shy:
                return ShyFishPrefab;
            case FishType.Legend:
                return LegendFishPrefab;
            default:
                return null;
        }
    }
}
