using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform PlayerCamera = null;
    [SerializeField] float MouseSensitivity = 3.5f;
    [SerializeField] float WalkSpeed = 6f;
    [SerializeField] [Range(0.0f, 0.5f)] float MoveSmoothTime = 0.3f;
    [SerializeField] [Range(0.0f, 0.5f)] float MouseSmoothTime = 0.03f;
    [SerializeField] bool LockCursor = true;
    [SerializeField] Transform FishingRod = null;
    [SerializeField] Transform FishingRodLineAttachPoint = null;

    // Boat bounds
    [SerializeField] float MinX = -1.5f;
    [SerializeField] float MaxX = 1.5f;
    [SerializeField] float MinZ = -4f;
    [SerializeField] float MaxZ = 4f;

    // UI
    GameUI UI;

    // Interact
    [SerializeField] float InteractDistance = 2f;

    float cameraPitch = 0.0f;
    // CharacterController controller = null;
    Vector2 currentDir = Vector2.zero;
    Vector2 currentDirVelocity = Vector2.zero;
    Vector2 currentMouseDelta = Vector2.zero;
    Vector2 currentMouseDeltaVelocity = Vector2.zero;
    Boat _boat;
    Waves _waves;

    private float _originaHeight;

    // HoldingRodState
    public float maxHoldTime = 5f;
    public float throwThresholdHoldTime = 3f;
    public float currHoldTime;

    // FishingState
    public GameObject BaitPrefab;
    public float throwForce = 60f;
    private GameObject currentBait;
    private Rigidbody currentBaitRB;
    private Bait _bait;
    private float reelingInHold;
    private float reelingInHoldDecayRate = 2.0f;
    private float reelingInHoldPullRate = 5.0f;
    private float reelingInHoldMax = 20f;
    private LineRenderer fishingLineRenderer;
    private bool fishBite;

    // FightingState
    private float fightPerc;
    public float fightPullForceBase = 0.7f;
    private float pFightPullForce => fightPullForceBase + FriendlyCaught * 0.2f + ShyCaught * 0.5f;

    // EndFightState
    private GameObject caughtFish = null;
    private bool wonFight;
    private string endFightText;


    // Game
    public int FriendlyCaught;
    public int ShyCaught;
    public bool LegendCaught;

    PlayerState _state;
    PlayerState? _nextState = null;
    public enum PlayerState
    {
        Walking,
        Driving,
        EnterDriving,
        HoldingRod,
        Fishing,
        Fighting,
        EndFight,
        Letter
    }

    void Start()
    {
        if (LockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        _originaHeight = transform.localPosition.y;
        _boat = FindObjectOfType<Boat>();
        _waves = FindObjectOfType<Waves>();
        UI = FindObjectOfType<GameUI>();

        ChangeState(PlayerState.Walking);

        UI.UpdateProgressText(true, FriendlyCaught, ShyCaught, pFightPullForce);
    }

    void Update()
    {
        if (_nextState != null)
            _gotoState(_nextState.Value);

        // Debug.Log($"Current state: {_state}");
        if (_state == PlayerState.Walking)
            WalkingStateUpdate();
        else if (_state == PlayerState.Driving)
            DrivingStateUpdate();
        else if (_state == PlayerState.EnterDriving)
            EnterDrivingStateUpdate();
        else if (_state == PlayerState.HoldingRod)
            HoldingRodStateUpdate();
        else if (_state == PlayerState.Fishing)
            FishingStateUpdate();
        else if (_state == PlayerState.Fighting)
            FightingStateUpdate();
        else if (_state == PlayerState.EndFight)
            EndFightStateUpdate();
        else if (_state == PlayerState.Letter)
            LetterStateUpdate();

    }

    private void LetterStateUpdate()
    {
    }

    private void EndFightStateUpdate()
    {
        if (caughtFish != null)
            caughtFish.transform.RotateAround(caughtFish.transform.position, Vector3.up, 30f * Time.deltaTime);
    }

    private void FightingStateUpdate()
    {
        fishingLineRenderer.SetPosition(0, FishingRodLineAttachPoint.position);
        fishingLineRenderer.SetPosition(1, currentBait.transform.position);

        UpdateMouseLook();
        UpdateMovement();

        // Reeling in
        if (Input.GetMouseButton(0))
        {
            fightPerc += pFightPullForce * Time.deltaTime;

            Vector3 pullVec = (FishingRodLineAttachPoint.position - currentBait.transform.position).normalized;
            pullVec.y *= 0.3f;
            currentBaitRB.AddForce(pullVec * 6f * pFightPullForce, ForceMode.Force);

            // Too close
            if (Vector3.Distance(currentBait.transform.position, transform.position) < 5f)
            {
                // Destroy(currentBait);
                wonFight = true;
                endFightText = $"You've just caught a {_bait.currentFish.Type} fish!";
                ChangeState(PlayerState.EndFight);
                return;
            }
        }
        fightPerc = _bait.UpdateFishPull(fightPerc);

        fightPerc = Mathf.Clamp(fightPerc, 0, 1f);


        var minRot = Quaternion.Euler(-60, 0, 0);
        var maxRot = Quaternion.Euler(-120, -20, 0);
        FishingRod.localRotation = Quaternion.Slerp(minRot, maxRot, fightPerc + 0.03f * UnityEngine.Random.Range(-1f, 1f));

        if (fightPerc >= 1 || fightPerc <= 0)
        {
            wonFight = false;
            endFightText = fightPerc >= 1 ? "The fishing rod line broke!" : "The fish got away";
            ChangeState(PlayerState.EndFight);
        }

        UI.UpdateFightingPanel(true, fightPerc);
    }
 
    private void FishingStateUpdate()
    {
        UpdateMouseLook();
        UpdateMovement();

        Debug.DrawLine(transform.position, transform.position + transform.forward.normalized * 6f);
        if (currentBait == null)
        {
            currentBait = Instantiate(BaitPrefab, FishingRodLineAttachPoint.position, Quaternion.identity);// , _waves.gameObject.transform);
            currentBaitRB = currentBait.GetComponent<Rigidbody>();
            _bait = currentBait.GetComponent<Bait>();
            var perc = Mathf.Clamp01(currHoldTime / maxHoldTime);
            currentBaitRB.AddForce(transform.forward.normalized * perc * throwForce, ForceMode.Impulse);
            fishingLineRenderer = currentBait.GetComponent<LineRenderer>();
            fishingLineRenderer.enabled = true;

            reelingInHold = 0.2f * reelingInHoldMax;

            fishBite = false;
            _bait.OnFishBiteEvent += () => fishBite = true;
        }

        // Line Renderer
        fishingLineRenderer.SetPosition(0, FishingRodLineAttachPoint.position);
        fishingLineRenderer.SetPosition(1, currentBait.transform.position);

        // Cancel bait
        if (Input.GetMouseButtonDown(1))
        {
            Destroy(currentBait);
            ChangeState(PlayerState.HoldingRod);
            return;
        }

        // Reeling in
        if (Input.GetMouseButton(0))
        {
            reelingInHold += reelingInHoldPullRate * Time.deltaTime;
            reelingInHold = Mathf.Min(reelingInHold, reelingInHoldMax);
            Vector3 pullVec = (FishingRodLineAttachPoint.position - currentBait.transform.position).normalized;
            pullVec.y *= 0.3f;
            currentBaitRB.AddForce(pullVec * 5f, ForceMode.Force);

            // Too close
            if (Vector3.SqrMagnitude(currentBait.transform.position - transform.position) < 5f * 5f)
            {
                Destroy(currentBait);
                ChangeState(PlayerState.HoldingRod);
                return;
            }
        }

        reelingInHold = Mathf.Max(reelingInHold - reelingInHoldDecayRate * Time.deltaTime, 0);


        var minRot = Quaternion.Euler(-60, 0, 0);
        var maxRot = Quaternion.Euler(-120, -20, 0);
        FishingRod.localRotation = Quaternion.Slerp(minRot, maxRot, reelingInHold/reelingInHoldMax);


        // Check bite
        if (fishBite)
        {
            Debug.Log("Fish Bite!!");
            ChangeState(PlayerState.Fighting);
            // Bait.OnFishPullEvent += (v) => fightPerc -= v;
        }
    }

    private void HoldingRodStateUpdate()
    {
        UpdateMouseLook();
        UpdateMovement();

        string updateText = "";

        if (Input.GetKeyDown(KeyCode.E))
        {
            _boat.PlaceFishingRod();
            FishingRod.gameObject.SetActive(false);
            ChangeState(PlayerState.Walking);
        }

        if (Input.GetMouseButton(0))
        {
            currHoldTime += 3 * Time.deltaTime;
            updateText = "Charging";

            // Cancel mid charge
            if (Input.GetMouseButtonDown(1))
            {
                currHoldTime = 0;
            }
        }
        else
            currHoldTime = currHoldTime - 4 * Time.deltaTime;

        currHoldTime = Mathf.Clamp(currHoldTime, 0, maxHoldTime);

        var holdPerc = currHoldTime / maxHoldTime;
        var minRot = Quaternion.Euler(-60, 0, 0);
        var maxRot = Quaternion.Euler(-120, -20, 0);
        FishingRod.localRotation = Quaternion.Slerp(minRot, maxRot, holdPerc);

        if (currHoldTime >= throwThresholdHoldTime)
        {
            updateText = "THROW!";
            if (Input.GetMouseButtonUp(0))
            {
                ChangeState(PlayerState.Fishing);
            }
        }

        UI.UpdateHoldingFishingRodPanel(true, holdPerc, updateText);
    }

    private void EnterDrivingStateUpdate()
    {

        var dir = Quaternion.Lerp(transform.localRotation, _boat.DrivingPosition.localRotation, 0.05f);
        var pos = Vector3.Lerp(transform.localPosition, _boat.DrivingPosition.localPosition, 0.05f);
        if (Vector3.Distance(pos, _boat.DrivingPosition.localPosition) < 0.1f)
        {
            pos = _boat.DrivingPosition.localPosition;
            dir = _boat.DrivingPosition.localRotation;
        }

        transform.localRotation = dir;
        transform.localPosition = pos;

        if (transform.localPosition == _boat.DrivingPosition.localPosition && transform.localRotation == _boat.DrivingPosition.localRotation)
        {
            ChangeState(PlayerState.Driving);
        }
    }

    private void DrivingStateUpdate()
    {
        UpdateMouseLook();
        DriveBoat();
        if (Input.GetKeyDown(KeyCode.E))
        {
            var pos = new Vector3(0, _originaHeight, MinZ + 1.5f);
            transform.localPosition = pos;

            ChangeState(PlayerState.Walking);
        }
    }


    private void WalkingStateUpdate()
    {
        UpdateMouseLook();
        UpdateMovement();
        UpdateInteract();
    }

    private void UpdateInteract()
    {
        GameObject pointingTo = null;

        int layerMask = 1 << 6; // Selectables
        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Debug.DrawRay(ray.origin, ray.direction.normalized * InteractDistance, Color.red);
        UI.CannotInteract();
        if (Physics.Raycast(ray, out hit, InteractDistance, layerMask))
        {
            // Debug.Log($"Hitting {hit.collider.gameObject.name}");
            pointingTo = hit.collider.gameObject;
            UI.CanInteractWith(pointingTo.name);
        }

        if (pointingTo != null && Input.GetKeyDown(KeyCode.E))
        {
            if (pointingTo.name == "Motor")
            {
                ChangeState(PlayerState.EnterDriving);
            }
            else if (pointingTo.name == "Fishing Rod")
            {
                ChangeState(PlayerState.HoldingRod);
            }
            else if (pointingTo.name == "Letter")
            {
                ChangeState(PlayerState.Letter);
            }
        }
    }

    private void DriveBoat()
    {
        int steer = -Mathf.RoundToInt(Input.GetAxis("Horizontal"));
        _boat.Steer(steer);
        int accel = Mathf.RoundToInt(Input.GetAxis("Vertical"));
        _boat.Accelerate(accel);
    }

    private void UpdateMovement()
    {
        Vector2 targetDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        targetDir.Normalize();

        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentDirVelocity, MoveSmoothTime);

        // Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * WalkSpeed;
        Vector3 velocity = (Vector3.forward * currentDir.y + Vector3.right * currentDir.x) * WalkSpeed;
        transform.Translate(velocity * Time.deltaTime);
        // controller.Move(velocity * Time.deltaTime);

        var curr_pos = transform.localPosition;
        curr_pos.x = Mathf.Clamp(curr_pos.x, MinX, MaxX);
        curr_pos.z = Mathf.Clamp(curr_pos.z, MinZ, MaxZ);
        transform.localPosition = curr_pos;
    }

    void UpdateMouseLook()
    {
        Vector2 targetMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, MouseSmoothTime);

        cameraPitch -= currentMouseDelta.y * MouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        PlayerCamera.localEulerAngles = Vector3.right * cameraPitch;

        transform.Rotate(Vector3.up * currentMouseDelta.x * MouseSensitivity);
    }


    public void _ChangeToHoldingFishingRodState() => _nextState = PlayerState.HoldingRod;
    public void _ChangeToWalkingState() => _nextState = PlayerState.Walking;

    void ChangeState(PlayerState state)
    {
        _nextState = state;
    }
    void _gotoState(PlayerState newState)
    {
        // Exit stuff
        if (_state == PlayerState.HoldingRod)
        {
            Debug.Log("Exiting HoldingRod State");
            UI.UpdateHoldingFishingRodPanel(false);
        }
        else if (_state == PlayerState.Letter)
        {
            UI.UpdateProgressText(true, FriendlyCaught, ShyCaught, pFightPullForce);
            UI.UpdateLetterPanel(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (_state == PlayerState.Fishing)
        {
            reelingInHold = 0;
            fishBite = false;
            //if (currentBait != null)
            //    Destroy(currentBait);
        }
        else if (_state == PlayerState.Fighting)
        {
            UI.UpdateFightingPanel(false);
        }
        else if (_state == PlayerState.EndFight)
        {
            if (wonFight)
                Destroy(caughtFish);
            UI.UpdateEndFightPanel(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Enter stuff
        UI.HideHelp();
        if (newState == PlayerState.Walking)
        {
            _boat.PlaceFishingRod();
            FishingRod.gameObject.SetActive(false);
            UI.HideHelp();
        }
        else if (newState == PlayerState.Letter)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UI.UpdateProgressText(false);
            UI.UpdateLetterPanel(true);
            UI.CannotInteract();
        }
        else if (newState == PlayerState.EnterDriving)
        {
            UI.CannotInteract();
        }
        else if (newState == PlayerState.HoldingRod)
        {
            currHoldTime = 0;
            UI.CannotInteract();
            _boat.RemoveFishingRod();
            FishingRod.gameObject.SetActive(true);
            if (currentBait != null)
            {
                currentBaitRB = null;
                _bait = null;
                Destroy(currentBait);
                currentBait = null;
            }
            UI.ShowHelp("HoldingRod");
        }
        else if (newState == PlayerState.Fishing)
        {
            fishBite = false;
            UI.ShowHelp("Fishing");
        }
        else if (newState == PlayerState.Fighting)
        {
            fightPerc = 0.5f;
        }
        else if (newState == PlayerState.EndFight)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (wonFight)
            {
                if (_bait.currentFish.Type == FishType.Friendly)
                    FriendlyCaught += 1;
                else if (_bait.currentFish.Type == FishType.Shy)
                    ShyCaught += 1;
                else if (_bait.currentFish.Type == FishType.Legend)
                {
                    LegendCaught = true; // Game ended
                    endFightText = $"You've just caught the Legend Fish!!!\nYour grandpa would be so proud!\n\nThanks for playing!\nPress 'ESC' to exit";
                }

                var fishPrefab = _waves.gameObject.GetComponent<Fishes>().GetPrefabOfFishType(_bait.currentFish.Type);
                caughtFish = Instantiate(
                    fishPrefab,
                    PlayerCamera.position + PlayerCamera.transform.forward.normalized * 2f + Vector3.up * 0.3f,
                    Quaternion.identity,
                    transform);

            }

            UI.UpdateEndFightPanel(true, endFightText);
            UI.UpdateProgressText(true, FriendlyCaught, ShyCaught, pFightPullForce);

            _bait = null;
            currentBaitRB = null;
            Destroy(currentBait);
            currentBait = null;
            FishingRod.gameObject.SetActive(false);
        }

        Debug.Log($"Exiting state {_state}. Entering state {newState}");
        _state = newState;
        _nextState = null;
    }
}
