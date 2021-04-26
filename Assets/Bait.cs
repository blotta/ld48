using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bait : MonoBehaviour
{
    public event Action OnFishBiteEvent;
    // public static event Action<float> OnFishPullEvent;


    public Mesh[] fishMeshes;

    public enum BaitState
    {
        Throwing,
        Waiting,
        Caught
    }

    public FishConfig currentFish;
    public BaitState _state = BaitState.Throwing;
    public BaitState? _nextState = null;

    public float ChanceCheckTime = 3f;
    private float _chanceCheckTimer;
    private Fishes _fishes;
    private Transform _boat;
    private GameUI UI;

    private Collider shyCol;
    private Collider legendCol;

    public float pDistanceToBoat => Vector3.Distance(transform.position, _boat.transform.position);


    void Start()
    {
        _fishes = FindObjectOfType<Fishes>();
        shyCol = _fishes.ShyFishAreaCollider;
        legendCol = _fishes.LegendFishAreaCollider;

        GetComponent<Rigidbody>().useGravity = true;
        _chanceCheckTimer = ChanceCheckTime;
        // currentFish = FishConfig.GetFor(FishType.Friendly);

        _boat = FindObjectOfType<Boat>().transform;

        UI = FindObjectOfType<GameUI>();
    }

    private void OnDestroy()
    {
        UI.UpdateLineDistance(false);
    }

    void Update()
    {
        _gotoNextState();

        if (_state == BaitState.Throwing)
            ThrowingStateUpdate();
        else if (_state == BaitState.Waiting)
            WaitingStateUpdate();
        else if (_state == BaitState.Caught)
            CaughtStateUpdate();


        UI.UpdateLineDistance(true, pDistanceToBoat);
    }
    private void ThrowingStateUpdate()
    {
        if (transform.position.y <= 0)
        {
            GetComponent<WaterFloat>().enabled = true;
            GetComponent<Rigidbody>().useGravity = false;
            ChangeState(BaitState.Waiting);
        }
    }


    private void WaitingStateUpdate()
    {
        _chanceCheckTimer -= Time.deltaTime;
        if (_chanceCheckTimer <= 0)
        {
            // See if bite
            currentFish = BiteChance();
            if (currentFish != null)
            {
                OnFishBiteEvent.Invoke();
                ChangeState(BaitState.Caught);
            }
        }
    }

    private FishConfig BiteChance()
    {

        FishType? ft;
        InsideFishArea(transform.position, out ft, FishType.Friendly);
        var fishAreaFish = FishConfig.GetFor(ft.Value);
        Debug.Log($"Bait inside fish area for {fishAreaFish.Type}");

        // Fish bite conditions
        Debug.Log($"Dist: {Vector3.Distance(_boat.position, transform.position)}");
        if (pDistanceToBoat < fishAreaFish.MinDistance)
        {
            Debug.Log("Too close, no bite");
            return null;
        }

        // Bit chance
        var rand = UnityEngine.Random.Range(0f, 1f);// >= currentFish.NibbleChance;
        if (rand <= fishAreaFish.NibbleChance)
        {
            // currentFish = fishAreaFish;
            return fishAreaFish;
        }

        return null;
    }

    private void CaughtStateUpdate()
    {
        // Fish fighting for its life code goes here!
        currentFish.PullTimer += Time.deltaTime;
        if (currentFish.PullTimer >= currentFish.PullTimerMax)
        {
            currentFish.Pulling = !currentFish.Pulling;
            // OnFishPullEvent(currentFish.PullForce);
            currentFish.PullTimer = UnityEngine.Random.Range(0, currentFish.PullTimerStartMax);
        }
    }

    public float UpdateFishPull(float currFight)
    {
        var val = currentFish.PullForceMin;
        if (currentFish.Pulling)
            val = UnityEngine.Random.Range(currentFish.PullForceMin, currentFish.PullForceMax);

        return currFight - val * Time.deltaTime;
    }

    void ChangeState(BaitState next)
    {
        if (_nextState == null)
            _nextState = next;
    }

    void _gotoNextState()
    {
        if (_nextState == null)
            return;

        BaitState next = _nextState.Value;
        _nextState = null;

        // Exit state stuff

        // Enter state stuff
        if (next == BaitState.Waiting)
        {
            currentFish = null;
        }

        _state = next;
    }

    private bool InsideFishArea(Vector3 pos, out FishType? type, FishType? def = null)
    {
        type = def;
        int layerMask = 1 << 7; // FishAreas
        var colls = Physics.OverlapSphere(pos, 1f, layerMask, QueryTriggerInteraction.Collide);
        if (colls.Length > 0)
        {
            if (colls[0] == shyCol)
            {
                type = FishType.Shy;
                return true;
            }
            else if (colls[0] == legendCol)
            {
                type = FishType.Legend;
                return true;
            }
        }

        return false;
    }
}
