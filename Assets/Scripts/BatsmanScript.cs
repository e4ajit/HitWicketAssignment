using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatsmanScript : MonoBehaviour
{
    private Transform thisTransform;
    private Vector3 initPos = new Vector3(0.7f, 0f, 9f);
    public Vector3 batsmanPos => thisTransform.position;

    [SerializeField] private Transform ballTransform;

    [SerializeField] private Transform bodyTransform;
    [SerializeField] private Transform handTransform;
    [SerializeField] private Transform batInHandTransform;

    

    public float handRotatedAngle => handTransform.localEulerAngles.y;

    

    private Vector3 tempVector3;

    [SerializeField] private BoxCollider batCollider;
    [SerializeField] private SphereCollider ballCollider;

    private byte rotationProgress = 0;
    private float rotationAngle = 0;

    private bool canHitBall;


    private float battingHand;
    private int[] battingHandTypes = new int[] { 1, -1 };

    void Start()
    {
        thisTransform = this.transform;
        GameController.UpdateAction += UpdateAction;
        GameController.ResetAction += reset;
    }

    public void decideBattingHand(out float handType)
    {
        handType = battingHandTypes[UnityEngine.Random.Range(0, battingHandTypes.Length)];
        battingHand = handType;
    }

    private void reset()
    {
        tempVector3 = initPos;
        tempVector3.x *= battingHand;
        thisTransform.position = tempVector3;

        tempVector3 = initPos * -1f;
        tempVector3.x *= battingHand;
        tempVector3.x -= 0.3f * battingHand;

        bodyTransform.localEulerAngles = Vector3.up * 90f * battingHand;

        handTransform.localEulerAngles = Vector3.zero;

        tempVector3 = batInHandTransform.localPosition;
        tempVector3.x = -0.18f * battingHand;
        batInHandTransform.localPosition = tempVector3;

        tempVector3.x = 0.25f * battingHand;
        tempVector3.y = 0.2f;

        batInHandTransform.localEulerAngles = Vector3.forward * 58.26f * battingHand;
        rotationProgress = 0; 
        disableColliders();
        thisTransform.localEulerAngles = Vector3.zero;
    }

    public void afterBallRelease()
    {
        canHitBall = true;
    }
    public IEnumerator HitBasedOnUserInput(GameController.BowlerType bowlerType)
    {
        if(bowlerType == GameController.BowlerType.Fast)
        {
            yield return new WaitForSeconds(0.1f);
        }
        else if (bowlerType == GameController.BowlerType.Spin)
        {
            yield return new WaitForSeconds(0.3f);
        }
        
        SwingBat();
    }
    public void afterHitBall()
    {
        if (rotationProgress == 0)
        {
            rotationProgress = 3;
        }
        
        disableColliders();
    }

    public void disableColliders()
    {
        canHitBall = false;
        batCollider.enabled = false;
        ballCollider.enabled = false;
    }

    private void UpdateAction()
    {
        
        if (rotationProgress == 1)
        {
            tempVector3 = handTransform.localEulerAngles;
            tempVector3.y = rotationAngle;
            if ((battingHand == 1f && rotationAngle < 0f) || (battingHand == -1f && rotationAngle > 360f))
            {
                rotationAngle = 180f;
                rotationProgress = 0;

                batCollider.enabled = false;
                ballCollider.enabled = false;
            }

            handTransform.localEulerAngles = tempVector3;
        }
    }

    private void SwingBat()
    {
        rotationProgress = 1;

        tempVector3 = handTransform.localEulerAngles;
        rotationAngle = tempVector3.y = 180f;
        handTransform.localEulerAngles = tempVector3;
        if (canHitBall)
        {
            batCollider.enabled = true;
            ballCollider.enabled = true;
        }
    }

    
}
