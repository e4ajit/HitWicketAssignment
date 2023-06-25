using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public delegate void MyDelegate();
    public static MyDelegate UpdateAction;
    public static MyDelegate ResetAction;
    public static MyDelegate StopFielding;
    public static MyDelegate OnFielderThrowBall;

    public delegate void MyCollisionDelegate(Collider _collider);
    public static MyCollisionDelegate CollisionTriggerAction;

    public delegate bool MyBoolelegate();
    public static MyBoolelegate OnFielderCollectBall;

    [SerializeField] private Transform ballTransform, ballPitchingTransform;
    [SerializeField] private Transform stump1Transform, stump2Transform;
    [SerializeField] private BatsmanScript batsmanScript;

    [SerializeField] private Transform bowlerTransform;
    [SerializeField] private Transform bowlerHandBallRef;
    [SerializeField] private Transform bowlerHandHolderTransform;

   

    [SerializeField] private FielderScript[] fieldersScript;
    private List<FielderScript> activeFielders;

    [SerializeField] private Transform batTransform;

    private enum GameStatus
    {
        UserSelect,
        Idle,
        Ready,
        BowlerMoveBackToBowl,
        BowlerBowling,
        InGame,
        ResetAfterDelay,
        GameOver,
    }

    private GameStatus _gamestatus;

    private enum BallStatus
    {
        Idle,
        Bowl,
        Bat,
        FielderCollect,
        FielderThrow,
        HitStumps,
        Boundary,
    }

    private BallStatus _ballStatus;


    private Vector3 tempVector3;
    private float tempFloat;

    public const float Rad2Deg = Mathf.Rad2Deg;
    public const float Deg2Rad = Mathf.Deg2Rad;

    private float ballRadius = 0.073f;
    private Vector3 stumpsDimension = new Vector3(0.2286f, 0.71f, 0.04572f);

    private float horizontalSpeed, ballPitchDistance;
    private float ballAngle;
    private float ballProjectileAngle, ballProjectileAnglePerSecond, ballProjectileHeight;

    private bool haveChanceToHitStumps;

    private float resetTime = 2f;

    public const float groundRadius = 50f;
    private Vector3 groundCenterpos = Vector3.zero;

    private Vector3 ballReleasePos;

    [SerializeField] private Transform _camTransform;
    private Vector3 camInitialPos;
    private Vector3 camInitialRotation;

    [SerializeField] private Text _bowlerTypeText;

    private int _runsScored;
    private int _tempRunsScored;
    private int _wickets;

    private int _runsToBeScored;

    private bool isOut = false;

    [SerializeField] private Text ballResultText;
    [SerializeField] private Text gameResultText;
    [SerializeField] private GameObject userSelectionScreen;
    [SerializeField] private GameObject bowlerSelectionScreen;
    [SerializeField] private GameObject pitchSelectionScreen;
    [SerializeField] private GameObject runsSelectionScreen;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Text zeroRunPercentage;
    [SerializeField] private Text oneRunPercentage;
    [SerializeField] private Text twoRunPercentage;
    [SerializeField] private Text fourRunPercentage;
    [SerializeField] private Text sixRunPercentage;

    public enum BowlerType : byte
    {
        Spin,
        Fast
    }

    private BowlerType _bowlertype;
    private float bowlerMoveBackDistance;
    private float bowlerMoveSpeed;

    private byte _ballBounceCount;
    private float fielderRunSpeed = 6.5f;
    private float fielderActivationAngleFactor = 1f;

    private int ballsCompletedInOver;
    private int oversCompleted;
    private int ballsRemaining;

    private string statusInThisOver;
    [SerializeField] private Text statusInThisOverText;

    public const float Player_Height = 1.4f;

    private float _battingHandType;//1 = right hand, -1 = left hand

    private bool hitOnce = false;

    private int totalNumberOfWickets = 5;
    private int totalNumberOfOvers = 5;
    private int totalRunsToWin = 60;

    private List<int> runsPercentage;

    private int pitchArea = 1;

    void Start()
    {
        CollisionTriggerAction += OnTriggerEnterAction;
        OnFielderCollectBall += OnFielderCollectedTheBall;
        OnFielderThrowBall += OnFielderReleasedTheBall;

        camInitialPos = _camTransform.position;
        camInitialRotation = _camTransform.localEulerAngles;
        batsmanScript.decideBattingHand(out _battingHandType);
        _gamestatus = GameStatus.UserSelect;
        runsPercentage = new List<int> { 90,85,60,35,20 };
        ShowUserSelectionUI();
    }
    private void ShowUserSelectionUI()
    {
        userSelectionScreen.SetActive(true);
    }
    public void BowlerSelection(int val)
    {
        _bowlertype = (BowlerType)val;
        bowlerSelectionScreen.SetActive(false);
        pitchSelectionScreen.SetActive(true);
    }
    public void PitchSelection(int val)
    {
        pitchArea = val;
        pitchSelectionScreen.SetActive(false);
        runsSelectionScreen.SetActive(true);
        zeroRunPercentage.text = runsPercentage[0].ToString() + "%";
        oneRunPercentage.text = runsPercentage[1].ToString() + "%";
        twoRunPercentage.text = runsPercentage[2].ToString() + "%";
        fourRunPercentage.text = runsPercentage[3].ToString() + "%";
        sixRunPercentage.text = runsPercentage[4].ToString() + "%";
    }
    public void RunsSelection(int val)
    {

        runsSelectionScreen.SetActive(false);
        userSelectionScreen.SetActive(false);
        bowlerSelectionScreen.SetActive(true);
        _runsToBeScored = val;
        _gamestatus = GameStatus.Idle;
    }
    private void reset()
    {
        resetTime = 3f;
        hitOnce = false;
        _gamestatus = GameStatus.Ready;
        _ballStatus = BallStatus.Idle;

        tempVector3 = bowlerTransform.position;
        tempVector3.x = 0.75f * _battingHandType;
        tempVector3.z = stump2Transform.position.z;
        bowlerTransform.position = tempVector3;
        bowlerTransform.localEulerAngles = Vector3.zero;
        bowlerHandHolderTransform.localEulerAngles = Vector3.zero;


        ballTransform.parent = bowlerHandBallRef.parent;
        ballTransform.localPosition = bowlerHandBallRef.localPosition;

        _camTransform.position = camInitialPos;
        _camTransform.localEulerAngles = camInitialRotation;

        _bowlerTypeText.text = string.Empty;

        ballResultText.text = string.Empty;

        _ballBounceCount = 0;
        _tempRunsScored = 0;

        if(activeFielders == null)
        {
            activeFielders = new List<FielderScript>();
        }
        else
        {
            activeFielders.Clear();
        }

        ResetAction?.Invoke();
    }

    public void OnClick_RestartGameButton()
    {
        statusInThisOver = string.Empty;
        ballsCompletedInOver = 0;
        oversCompleted = 0;
        statusInThisOverText.text = statusInThisOver + "   (" + oversCompleted + "." + ballsCompletedInOver + "}";


        _runsScored = 0;
        _wickets = 0;
        gameResultText.text = _runsScored + "/" + _wickets;
        gameOverScreen.SetActive(false);

        _gamestatus = GameStatus.Idle;
    }

    

    // Update is called once per frame
    void Update()
    {
        switch (_gamestatus)
        {
            case GameStatus.UserSelect:
                break;
            case GameStatus.Idle:
                reset();
                break;
            case GameStatus.Ready:
                    bowlerMoveSpeed = -5f;
                    _gamestatus = GameStatus.BowlerMoveBackToBowl;

                    switch (_bowlertype)
                    {
                        
                        case BowlerType.Spin:
                            bowlerMoveBackDistance = 3.2f;
                            _bowlerTypeText.text = "SPIN ball "+pitchArea;
                            break;
                        case BowlerType.Fast:
                            bowlerMoveBackDistance = 7f;
                            _bowlerTypeText.text = "FAST ball "+pitchArea;
                            break;
                    }
                break;
            case GameStatus.BowlerMoveBackToBowl:
                bowlerTransform.position += bowlerTransform.forward * Time.deltaTime * bowlerMoveSpeed;
                if (bowlerTransform.position.z < stump2Transform.position.z - bowlerMoveBackDistance)
                {
                    _gamestatus = GameStatus.BowlerBowling;

                    switch (_bowlertype)
                    {
                        case BowlerType.Spin:
                            bowlerMoveSpeed = 7f;
                            break;
                        case BowlerType.Fast:
                            bowlerMoveSpeed = 12f;
                            break;
                    }
                }
                break;
            case GameStatus.BowlerBowling:
                bowlerTransform.position += bowlerTransform.forward * Time.deltaTime * bowlerMoveSpeed;
                if (bowlerTransform.position.z > stump2Transform.position.z)
                {
                    ballTransform.parent = null;

                    UpdateBallMovementParameters(BallStatus.Bowl);
                    _gamestatus = GameStatus.InGame;
                    batsmanScript.afterBallRelease();

                }
                break;
            case GameStatus.InGame:
                if(_ballStatus == BallStatus.Bowl && (batTransform.position.z- ballTransform.position.z) <= 8f && !hitOnce)
                {
                    hitOnce = true;
                    StartCoroutine(batsmanScript.HitBasedOnUserInput(_bowlertype));
                }
                if (_ballStatus == BallStatus.FielderCollect)
                {
                    break;
                }

                GetNextBallPos(out tempVector3);

                if (_ballStatus == BallStatus.FielderThrow)
                {
                    if (DistanceBetweenTwoVector2(ballReleasePos, tempVector3) > DistanceBetweenTwoVector2(ballReleasePos, bowlerHandBallRef.position))
                    {

                        ballTransform.parent = bowlerHandBallRef.parent;
                        tempVector3 = bowlerHandBallRef.position;

                        
                            increaseBallCount(BallCompleteStatus.None);
                            callToResetAFterDelay(2f);
                    }
                }
                else if (isHitStumps(ref tempVector3))
                {

                    UpdateBallMovementParameters(BallStatus.HitStumps);
                    batsmanScript.disableColliders();

                    increaseBallCount(BallCompleteStatus.Bowled);
                    callToResetAFterDelay(2f);
                }
                else if (DistanceBetweenTwoVector2(groundCenterpos, ballTransform.position) >= groundRadius)
                {
                    _ballStatus = BallStatus.Boundary;

                    if (_ballBounceCount == 0)
                    {
                        _tempRunsScored = 6;
                        increaseBallCount(BallCompleteStatus.Six);
                    }
                    else
                    {
                        _tempRunsScored = 4;
                        increaseBallCount(BallCompleteStatus.Four);
                    }

                    callToResetAFterDelay(2f);

                    StopFielding?.Invoke();
                }
                else if (ballTransform.position.z > stump1Transform.position.z + 1f)
                {
                    increaseBallCount(BallCompleteStatus.None);
                    callToResetAFterDelay(2f);
                }

                ballTransform.position = tempVector3;

                if (_ballStatus == BallStatus.Bat || _ballStatus == BallStatus.FielderThrow)
                {
                    if (tempVector3.y > _camTransform.position.y)
                    {
                        tempVector3.y = _camTransform.position.y;
                    }
                    _camTransform.LookAt(tempVector3);
                }

                for(int i = 0; i < activeFielders.Count; i++)
                {
                    for(int j = (i + 1); j < activeFielders.Count; j ++)
                    {
                        activeFielders[i].checkConflictWithOtherFielder(activeFielders[j]);
                    }
                }
                break;
            
            case GameStatus.ResetAfterDelay:

                if (ballTransform.parent == null && _ballStatus != BallStatus.FielderCollect)
                {
                    GetNextBallPos(out tempVector3);
                    ballTransform.position = tempVector3;
                }

                resetTime -= Time.deltaTime;
                if (resetTime <= 0f)
                {
                    //reset();
                    _gamestatus = GameStatus.UserSelect;
                    ShowUserSelectionUI();
                }
                break;
        }

        tempVector3 = ballTransform.position;
        tempFloat = tempVector3.y;
        tempVector3.y = 0.01f;

        tempFloat *= 0.05f;
        tempFloat = 1.3f - Mathf.Clamp(tempFloat, 0.3f, 1f);

        UpdateAction?.Invoke();
    }

    private void callToResetAFterDelay(float _delayTime)
    {
        _bowlerTypeText.text = string.Empty;
        
        if (_wickets == totalNumberOfWickets)
        {
            gameOverText.text = "Bowler Wins!";
            gameOverScreen.SetActive(true);
            _gamestatus = GameStatus.GameOver;
        }
        if (oversCompleted >= totalNumberOfOvers)
        {
            gameOverText.text = "Bowler Wins!";
            gameOverScreen.SetActive(true);
            _gamestatus = GameStatus.GameOver;
        }
        else if(_runsScored >= totalRunsToWin)
        {
            gameOverText.text = "Batsman Wins!";
            gameOverScreen.SetActive(true);
            _gamestatus = GameStatus.GameOver;
        }
        else
        {
            if (isOut)
            {
                batsmanScript.decideBattingHand(out _battingHandType);
                isOut = false;
            }
            resetTime = 2f;
            _gamestatus = GameStatus.ResetAfterDelay;
        }
    }

    private void GetNextBallPos(out Vector3 tempVector3)
    {
        ballProjectileAngle += (ballProjectileAnglePerSecond * Time.deltaTime);
        if (ballProjectileAngle > 180f)
        {
            UpdateBallParametersAfterPitch(ref horizontalSpeed, ref ballProjectileHeight, ref ballProjectileAngle, ref ballPitchDistance, ref ballProjectileAnglePerSecond, ref _ballBounceCount);

            if (_ballStatus == BallStatus.Bowl)
            {//for spin ball only
                if (_ballBounceCount == 1)
                {

                    if(Random.Range(0,2) == 1)
                    {
                        ballAngle -= Random.Range(2f, 6f);
                    }
                    else
                    {
                        ballAngle += Random.Range(2f, 6f);
                    }
                }

                tempVector3 = stump1Transform.position + (Vector3.back * stumpsDimension.z / 2f);

                tempVector3.x = stump1Transform.position.x + (stumpsDimension.x / 2f) + ballRadius;
                float minAngle = AngleBetweenTwoVector3(ballTransform.position, tempVector3);

                tempVector3.x = stump1Transform.position.x - (stumpsDimension.x / 2f) - ballRadius;
                float maxAngle = AngleBetweenTwoVector3(ballTransform.position, tempVector3);

                haveChanceToHitStumps = (ballAngle > minAngle && ballAngle < maxAngle);
            }
        }

        tempVector3 = ballTransform.position;
        tempVector3.x += Mathf.Cos(ballAngle * Deg2Rad) * horizontalSpeed * Time.deltaTime;
        tempVector3.z += Mathf.Sin(ballAngle * Deg2Rad) * horizontalSpeed * Time.deltaTime;
        tempVector3.y = ballRadius + Mathf.Sin(ballProjectileAngle * Deg2Rad) * ballProjectileHeight;
    }

    private bool isHitStumps(ref Vector3 nextBallPos)
    {
        if (haveChanceToHitStumps)
        {
            float zLimit = stump1Transform.position.z - (stumpsDimension.z / 2f);
            if (nextBallPos.z + ballRadius > zLimit && ballTransform.position.z + ballRadius <= zLimit && nextBallPos.y < (stump1Transform.position.y + stumpsDimension.y + ballRadius))
            {
                nextBallPos.z = zLimit - ballRadius;
                return true;
            }
        }
        return false;
    }

    private void UpdateBallMovementParameters(BallStatus _status)
    {
        _ballStatus = _status;
        switch (_ballStatus)
        {
            case BallStatus.Bowl:
                float pitchRangeFromStumps = 1.5f;
                float pitchRangeXDirection = 0;
                switch (pitchArea)
                {
                    case 1:
                        pitchRangeFromStumps = Random.Range(6.5f, 8f);
                        pitchRangeXDirection = Random.Range(0f,2f);
                        break;
                    case 2:
                        pitchRangeFromStumps = Random.Range(6.5f, 8f);
                        pitchRangeXDirection = Random.Range(-2f, 0f);

                        break;
                    case 3:
                        pitchRangeFromStumps = Random.Range(3f, 6.5f);
                        pitchRangeXDirection = Random.Range(0f, 2f);

                        break;
                    case 4:
                        pitchRangeFromStumps = Random.Range(3f, 6.5f);
                        pitchRangeXDirection = Random.Range(-2f, 0f);

                        break;
                    case 5:
                        pitchRangeFromStumps = Random.Range(1.5f, 3f);
                        pitchRangeXDirection = Random.Range(0f, 2f);

                        break;
                    case 6:
                        pitchRangeFromStumps = Random.Range(1.5f, 3f);
                        pitchRangeXDirection = Random.Range(-2f, 0f);

                        break;
                }
                switch (_bowlertype)
                {
                    
                    case BowlerType.Spin:
                        horizontalSpeed = Random.Range(15f, 20f);
                        break;
                    case BowlerType.Fast:
                        horizontalSpeed = Random.Range(18f, 28f);
                        
                        break;
                }

                tempVector3 = stump1Transform.position + (Vector3.back * stumpsDimension.z / 2f);
                tempVector3.x -= stumpsDimension.x;
                float maxAngle = AngleBetweenTwoVector3(ballTransform.position, tempVector3);
                tempVector3.x = (stump1Transform.position.x + pitchRangeXDirection) + (stumpsDimension.x / 2f);
                float minAngle = AngleBetweenTwoVector3(ballTransform.position, tempVector3);

                ballAngle = Random.Range(minAngle, maxAngle);

                tempVector3.x = stump1Transform.position.x - (stumpsDimension.x / 2f) - ballRadius;
                maxAngle = AngleBetweenTwoVector3(ballTransform.position, tempVector3);

                haveChanceToHitStumps = (ballAngle > minAngle && ballAngle < maxAngle);

                ballPitchDistance = (stump1Transform.position.z - ballTransform.position.z) - pitchRangeFromStumps;

                ballProjectileHeight = ballTransform.position.y;
                
                ballProjectileAnglePerSecond = ((180 - ballProjectileAngle) / ballPitchDistance) * horizontalSpeed;
                break;
            case BallStatus.Bat:
                switch(_runsToBeScored)
                {
                    case 0:
                        if(Random.Range(0, 100) < runsPercentage[0])
                        {
                            horizontalSpeed = 15f;
                            ballPitchDistance = 10f;
                            _tempRunsScored = 0;
                        }
                        else
                        {
                            _tempRunsScored = 0;
                            ballResultText.text = "MISSED";
                            return;
                        }
                        break;
                    case 1:
                        if (Random.Range(0, 100) < runsPercentage[1])
                        {
                            _tempRunsScored = 1;
                            horizontalSpeed = 17f;
                            ballPitchDistance = 12f;
                        }
                        else
                        {
                            _tempRunsScored = 0;
                            ballResultText.text = "MISSED";
                            return;
                        }
                        break;
                    case 2:
                        if (Random.Range(0, 100) < runsPercentage[2])
                        {
                            _tempRunsScored = 2;
                            horizontalSpeed = 19f;
                            ballPitchDistance = 14f;
                        }
                        else
                        {
                            _tempRunsScored = 0;
                            ballResultText.text = "MISSED";
                            return;
                        }
                        break;
                    case 4:
                        if (Random.Range(0, 100) < runsPercentage[3])
                        {
                            horizontalSpeed = 25f;
                            ballPitchDistance = groundRadius - 5f;
                        }
                        else
                        {
                            _tempRunsScored = 0;
                            ballResultText.text = "MISSED";
                            return;
                        }
                        break;
                    case 6:
                        if (Random.Range(0, 100) < runsPercentage[4])
                        {
                            horizontalSpeed = 25f;
                            ballPitchDistance = groundRadius + 5f;
                        }
                        else
                        {
                            _tempRunsScored = 0;
                            ballResultText.text = "MISSED";
                            return;
                        }
                        break;
                }

                ballAngle = batsmanScript.handRotatedAngle + 90f;
                ballAngle = (ballAngle + 360f) % 360f;
                ballAngle = Mathf.Clamp(ballAngle, 210f, 330f);

                ballProjectileHeight = ballPitchDistance * 0.1f;

                if (ballProjectileHeight < ballTransform.position.y)
                {
                    ballProjectileHeight = ballTransform.position.y;
                }
                ballProjectileAngle = Mathf.Asin(ballTransform.position.y / ballProjectileHeight) * Rad2Deg;
                ballProjectileAnglePerSecond = ((180 - ballProjectileAngle) / ballPitchDistance) * horizontalSpeed;
                break;
            case BallStatus.FielderThrow:
                ballAngle = AngleBetweenTwoVector3(ballTransform.position, bowlerHandBallRef.position);
                ballPitchDistance = DistanceBetweenTwoVector2(ballTransform.position, bowlerHandBallRef.position);
                horizontalSpeed = Random.Range(10f, 13f);
                ballProjectileHeight = ballPitchDistance * 0.15f;
                if (ballProjectileHeight < ballTransform.position.y)
                {
                    ballProjectileHeight = ballTransform.position.y + 1f;
                }

                if (ballProjectileHeight < bowlerHandBallRef.position.y)
                {
                    ballProjectileHeight = bowlerHandBallRef.position.y + 1f;
                }

                ballProjectileAngle = Mathf.Asin(ballTransform.position.y / ballProjectileHeight) * Rad2Deg;
                float endAngle = Mathf.Asin(bowlerHandBallRef.position.y / ballProjectileHeight) * Rad2Deg;
                ballProjectileAnglePerSecond = ((180 - ballProjectileAngle - endAngle) / ballPitchDistance) * horizontalSpeed;

                break;
            case BallStatus.HitStumps:
                ballAngle = -ballAngle - (15f * (ballTransform.position.x / Mathf.Abs(ballTransform.position.x)));
                ballAngle = (ballAngle + 360f) % 360f;
                horizontalSpeed = Random.Range(6f, 8f);
                ballProjectileHeight = ballTransform.position.y;
                ballPitchDistance = Random.Range(1f, 2f);
                if (ballPitchDistance < ballRadius)
                {
                    ballPitchDistance = ballRadius;
                }

                ballProjectileAngle = Mathf.Asin(ballTransform.position.y / ballProjectileHeight) * Rad2Deg;
                ballProjectileAnglePerSecond = ((180 - ballProjectileAngle) / ballPitchDistance) * horizontalSpeed;
                break;
        }
    }

    private void UpdateBallParametersAfterPitch(ref float _ballSpeed, ref float bpH, ref float bpA, ref float bpD, ref float bpAPs, ref byte _bounceCount)
    {
        _bounceCount++;
        bpA = 0f;


        bpH *= 0.3f;
        if (bpH < ballRadius/* || _bounceCount > 3*/)
        {
            //bpH = 0f;
            bpD *= 0.9f;
            _ballSpeed *= 0.95f;
        }
        else
        {
            bpD *= 0.5f;
            _ballSpeed *= 0.9f;
        }

        if (_ballSpeed < 0.2f)
        {
            _ballSpeed = 0f;
        }
        bpAPs = ((180 - bpA) / bpD) * _ballSpeed;
        
    }

    private bool OnFielderCollectedTheBall()
    {
        StopFielding?.Invoke();
        _ballStatus = BallStatus.FielderCollect;
        if (_ballBounceCount == 0)
        {
            increaseBallCount(BallCompleteStatus.Caught);
            callToResetAFterDelay(2f);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnTriggerEnterAction(Collider other)
    {
        if (other.name == "BallHolder")
        {
            _ballBounceCount = 0;

            UpdateBallMovementParameters(BallStatus.Bat);
            _gamestatus = GameStatus.InGame;
            batsmanScript.afterHitBall();
            CheckAndActivateFielders();

            _bowlerTypeText.text = string.Empty;
        }
    }

    private void OnFielderReleasedTheBall()
    {
        _gamestatus = GameStatus.InGame;
        UpdateBallMovementParameters(BallStatus.FielderThrow);
        ballReleasePos = ballTransform.position;
    }

    private void CheckAndActivateFielders()
    {

        for (byte i = 0; i < fieldersScript.Length; i++)
        {
            Transform _fielderTransform = fieldersScript[i].transform;
            float angle = Mathf.Abs(ballAngle - AngleBetweenTwoVector3(ballTransform.position, _fielderTransform.position));
            if (angle <= 50f * fielderActivationAngleFactor)
            {//active fielders to field
                float ballTravelAdjacentDistance = DistanceBetweenTwoVector2(ballTransform.position, _fielderTransform.position);

                tempVector3 = ballTransform.position;
                tempVector3.x += ballTravelAdjacentDistance * Mathf.Cos(ballAngle * Deg2Rad);
                tempVector3.z += ballTravelAdjacentDistance * Mathf.Sin(ballAngle * Deg2Rad);
                tempVector3.y = 0f;

                float tempTimeForFielderToReachThisTarget = DistanceBetweenTwoVector2(_fielderTransform.position, tempVector3) / fielderRunSpeed;

                float _ballSpeed = horizontalSpeed;
                float bpH = ballProjectileHeight;
                float bpA = ballProjectileAngle;
                float bpD = ballPitchDistance;
                float bpAPs = ballProjectileAnglePerSecond;

                float distanceCovered = 0f;

                byte _bounceCount = _ballBounceCount;

                while (ballTravelAdjacentDistance > 0f && _ballSpeed > 0f && tempTimeForFielderToReachThisTarget > 0f)
                {
                    float currentBallPitchDistance = ((180f - bpA) / bpAPs) * _ballSpeed;
                    if (ballTravelAdjacentDistance <= currentBallPitchDistance)
                    {
                        float ballReachYPos = Mathf.Sin(bpA * Deg2Rad) * bpH;

                        if (ballReachYPos > Player_Height)
                        {
                            float tempBPAAtPlayerHeight = Mathf.Asin(1.2f / bpH) * Rad2Deg;
                            ballTravelAdjacentDistance = ((180f - tempBPAAtPlayerHeight) / bpAPs) * _ballSpeed;
                        }

                        distanceCovered += ballTravelAdjacentDistance;

                        ballTravelAdjacentDistance = 0f;
                    }
                    else
                    {
                        ballTravelAdjacentDistance -= currentBallPitchDistance;

                        float tempTime = (currentBallPitchDistance / _ballSpeed);

                        if (tempTime <= tempTimeForFielderToReachThisTarget)
                        {
                            tempTimeForFielderToReachThisTarget -= tempTime;
                            distanceCovered += currentBallPitchDistance;
                        }
                        else
                        {
                            distanceCovered += (tempTimeForFielderToReachThisTarget * _ballSpeed);
                            tempTimeForFielderToReachThisTarget = 0f;
                        }

                        UpdateBallParametersAfterPitch(ref _ballSpeed, ref bpH, ref bpA, ref bpD, ref bpAPs, ref _bounceCount);
                    }
                }

                tempVector3 = ballTransform.position;
                tempVector3.x += distanceCovered * Mathf.Cos(ballAngle * Deg2Rad);
                tempVector3.z += distanceCovered * Mathf.Sin(ballAngle * Deg2Rad);
                tempVector3.y = 0f;

                fieldersScript[i].SetTargetTransform(ballTransform, tempVector3, fielderRunSpeed);
                fieldersScript[i].SetFieldStatus(FielderStatus.ActiveToField);

                tempVector3.y = 0.1f;

                activeFielders.Add(fieldersScript[i]);
            }
            else
            {//in active to field
                fieldersScript[i].SetFieldStatus(FielderStatus.InActiveToField);
            }

            _fielderTransform = null;
        }
    }

    public static float AngleBetweenTwoVector3(Vector3 v1, Vector3 v2)
    {
        float xDiff = v1.x - v2.x;
        float zDiff = v1.z - v2.z;
        float angle = Mathf.Atan2(xDiff, zDiff) * Rad2Deg;
        angle = ((270 - angle) + 360) % 360;
        return angle;
    }

    public static float DistanceBetweenTwoVector2(Vector3 go1, Vector3 go2)
    {
        float xDiff = go1.x - go2.x;
        float zDiff = go1.z - go2.z;

        float distance = Mathf.Sqrt(xDiff * xDiff + zDiff * zDiff);
        return distance;
    }

    private void OnDestroy()
    {
        CollisionTriggerAction -= OnTriggerEnterAction;
        UpdateAction = null;
        ResetAction = null;
        StopFielding = null;
        OnFielderCollectBall = null;
        OnFielderThrowBall = null;
    }

    

    private void increaseBallCount(BallCompleteStatus ballCompleteStatus)
    {
        if (!string.IsNullOrEmpty(statusInThisOver))
        {
            statusInThisOver += "|";
        }

        switch (ballCompleteStatus)
        {
            case BallCompleteStatus.None:
                if(_tempRunsScored == 0)
                {
                    ballResultText.text = "MISSED";
                }
                _runsScored += _tempRunsScored;
                ballsCompletedInOver++;
                statusInThisOver += _tempRunsScored;
                break;
            case BallCompleteStatus.Bowled:
                _wickets++;
                isOut = true;
                _runsScored += _tempRunsScored;
                ballsCompletedInOver++;
                ballResultText.text = "BOWLED";
                statusInThisOver += _tempRunsScored + "-" + "B";
                break;
            case BallCompleteStatus.Caught:
                _wickets++;
                isOut = true;
                _runsScored += _tempRunsScored;
                ballsCompletedInOver++;
                ballResultText.text = "CAUGHT";
                statusInThisOver += _tempRunsScored + "-" + "C";
                break;
            case BallCompleteStatus.Four:
                _runsScored += _tempRunsScored;
                ballsCompletedInOver++;
                ballResultText.text = "FOUR";
                statusInThisOver += _tempRunsScored;
                break;
            case BallCompleteStatus.Six:
                _runsScored += _tempRunsScored;
                ballsCompletedInOver++;
                ballResultText.text = "SIX";
                statusInThisOver += _tempRunsScored;
                break;
        }

        if (ballsCompletedInOver >= 6)
        {
            ballsCompletedInOver = 0;
            oversCompleted++;
            statusInThisOver = string.Empty;
        }
        ballsRemaining = (totalNumberOfOvers * 6) - (oversCompleted * 6) - ballsCompletedInOver;
        statusInThisOverText.text = statusInThisOver + "   (" + oversCompleted + "." + ballsCompletedInOver + ")" + "balls remaining : "+ballsRemaining;

        gameResultText.text = _runsScored + "/" + _wickets;
    }

    public enum BallCompleteStatus : byte
    {
        None,
        Bowled,
        Caught,
        Four,
        Six
    }

#if UNITY_EDITOR
    [SerializeField] private Transform _cube;
    [ContextMenu("Create Boundary Line")]
    private void CreateBoundaryLine()
    {
        float angle = 0f;
        
        float incrementAngle = (5f / groundRadius) * Rad2Deg;
        int count = (int)(360f / incrementAngle);
        Debug.Log(count);
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(_cube, _cube.parent);
            tempVector3 = Vector3.zero;
            tempVector3.x += groundRadius * Mathf.Cos(angle * Deg2Rad);
            tempVector3.z += groundRadius * Mathf.Sin(angle * Deg2Rad);
            obj.transform.localPosition = tempVector3;
            obj.LookAt(Vector3.zero);
            angle += incrementAngle;
        }
    }

    [SerializeField] private Transform _plane;
    [ContextMenu("Create Boundary Banner")]
    private void CreateBoundaryBanner()
    {
        float angle = 0f;

        float radius = groundRadius + 5f;

        float incrementAngle = (5f / radius) * Rad2Deg;
        int count = (int)(360f / incrementAngle);
        Debug.Log(count);
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(_plane, _plane.parent);
            tempVector3 = Vector3.zero;
            tempVector3.x += radius * Mathf.Cos(angle * Deg2Rad);
            tempVector3.z += radius * Mathf.Sin(angle * Deg2Rad);
            obj.transform.localPosition = tempVector3;
            obj.LookAt(Vector3.zero);
            angle += incrementAngle;
        }
    }
#endif
}
