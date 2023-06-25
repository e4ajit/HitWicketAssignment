using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FielderStatus
{
    Idle,
    WaitForBatsmanToHit,
    ActiveToField,
    WaitForBall,
    InActiveToField, 
    Throw,
    StopField
}

public class FielderScript : MonoBehaviour
{
    private Transform thisTransform;
    private Vector3 initialPos;

    private Vector3 targetPos;
    private Transform targetTransform;
    private Vector3 batHitPos;

    private FielderStatus _fielderStatus;
    private float delayTime;
    private float fielderSpeed;

    private Vector3 tempVector3;

    private float mySiblingIndex;
    private float incrementAngle;

    [HideInInspector]
    public bool isChasingTheBall;

    void Start()
    {
        thisTransform = this.transform;
        initialPos = thisTransform.position;

        GameController.UpdateAction += UpdateAction;
        GameController.ResetAction += reset;

        GameController.StopFielding += StopFielding;

        incrementAngle = 180f / thisTransform.parent.childCount;
        mySiblingIndex = thisTransform.GetSiblingIndex();
    }

    private void reset()
    {
        _fielderStatus = FielderStatus.Idle;

        //thisTransform.position = initialPos;

        float halfIncrementANgle = (incrementAngle / 2f);
        float angle = 180f + halfIncrementANgle + (mySiblingIndex * incrementAngle);
        halfIncrementANgle = (incrementAngle / 3f);
        angle += Random.Range(-halfIncrementANgle, halfIncrementANgle);
        float distance = Random.Range(10f, GameController.groundRadius - 15f);
        if(Mathf.Abs(270f - angle) < incrementAngle && distance < 20f)
        {
            distance += 10f;
        }

        tempVector3 = Vector3.zero;
        tempVector3.x += distance * Mathf.Cos(angle * GameController.Deg2Rad);
        tempVector3.z += distance * Mathf.Sin(angle * GameController.Deg2Rad);

        thisTransform.position = tempVector3;

        tempVector3 = Vector3.forward * 9f;
        thisTransform.LookAt(tempVector3);

        isChasingTheBall = false;
    }

    public void SetTargetTransform(Transform _target, Vector3 targetPos, float _speed)
    {
        this.targetPos = targetPos;
        targetTransform = _target;
        fielderSpeed = _speed;
        batHitPos = _target.position;
        thisTransform.LookAt(targetPos);
    }

    public void SetFieldStatus(FielderStatus _status)
    {
        _fielderStatus = _status;
    }

    public void StopFielding()
    {
        _fielderStatus = FielderStatus.StopField;
        targetTransform = null;
    }

    public void checkConflictWithOtherFielder(FielderScript otherFielderScript)
    {
        if (_fielderStatus == FielderStatus.ActiveToField && otherFielderScript._fielderStatus == FielderStatus.ActiveToField && targetTransform != null)
        {
            float distanceBtwThisFielderAndBall = GameController.DistanceBetweenTwoVector2(targetTransform.position, thisTransform.position);
            float distanceBtwOtherFielderAndBall = GameController.DistanceBetweenTwoVector2(targetTransform.position, otherFielderScript.transform.position);
            if (isChasingTheBall && otherFielderScript.isChasingTheBall == false && distanceBtwThisFielderAndBall > distanceBtwOtherFielderAndBall)
            {
                StopFielding();
            }
            else if (isChasingTheBall && otherFielderScript.isChasingTheBall)
            {
                if (distanceBtwThisFielderAndBall > distanceBtwOtherFielderAndBall)
                {
                    StopFielding();
                }
                else
                {
                    otherFielderScript.StopFielding();
                }
            }
        }
    }

    private void UpdateAction()
    {
        switch (_fielderStatus)
        {
            case FielderStatus.Idle:
                break;
            case FielderStatus.WaitForBatsmanToHit:
                break;
            case FielderStatus.ActiveToField:
                if(GameController.DistanceBetweenTwoVector2(batHitPos, targetTransform.position)
                    > GameController.DistanceBetweenTwoVector2(batHitPos, targetPos))
                {
                    targetPos = targetTransform.position;
                    targetPos.y = 0f;
                    thisTransform.LookAt(targetPos);

                    isChasingTheBall = true;
                }

                float angleToMove = GameController.AngleBetweenTwoVector3(thisTransform.position, targetPos);
                tempVector3 = thisTransform.position;
                tempVector3.x += Mathf.Cos(angleToMove * GameController.Deg2Rad) * fielderSpeed * Time.deltaTime;
                tempVector3.z += Mathf.Sin(angleToMove * GameController.Deg2Rad) * fielderSpeed * Time.deltaTime;
                thisTransform.position = tempVector3;

                if (targetTransform.position.y <= GameController.Player_Height && GameController.DistanceBetweenTwoVector2(thisTransform.position, targetTransform.position) < 0.5f)
                {
                    if (GameController.OnFielderCollectBall())
                    {
                        _fielderStatus = FielderStatus.Idle;
                    }
                    else
                    {
                        _fielderStatus = FielderStatus.Throw;
                        delayTime = 0.2f;
                    }
                }
                else if(GameController.DistanceBetweenTwoVector2(thisTransform.position, targetPos) < 0.2f)
                {
                    thisTransform.position = targetPos;
                    _fielderStatus = FielderStatus.WaitForBall;
                }
                break;
            case FielderStatus.WaitForBall:
                if (targetTransform.position.y <= GameController.Player_Height && GameController.DistanceBetweenTwoVector2(targetTransform.position, thisTransform.position) < 0.5f)
                {
                    if (GameController.OnFielderCollectBall())
                    {
                        _fielderStatus = FielderStatus.Idle;
                    }
                    else
                    {
                        _fielderStatus = FielderStatus.Throw;
                        delayTime = 0.2f;
                    }
                }
                else
                {
                    tempVector3 = targetTransform.position;
                    tempVector3.y = 0f;
                    thisTransform.LookAt(tempVector3);
                }
                break;
            case FielderStatus.InActiveToField:
                break;
            case FielderStatus.Throw:
                delayTime -= Time.deltaTime;
                if (delayTime <= 0f)
                {
                    thisTransform.LookAt(Vector3.back * 9f);
                    GameController.OnFielderThrowBall?.Invoke();
                    _fielderStatus = FielderStatus.StopField;
                }
                break;
        }
    }
}
