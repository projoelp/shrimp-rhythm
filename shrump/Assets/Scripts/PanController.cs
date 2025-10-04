using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanController : MonoBehaviour
{
    [SerializeField] private Transform panPivot;
    [SerializeField] private float rotationSpeed = 120f;
    private float maxRotation = 180f;

    public float currentZRotation = 0f;
    private float gravityThreshold = 5f;
    private float gravitySpeed = 300f;
    private bool toggle_panGravity = false;
    public bool IsBurning => Time.time - time_lastInput > burnTimer;
    public Transform PanTransform => panPivot != null ? panPivot : transform;

    private float time_lastInput;
    [SerializeField] private float burnTimer = 10f;

    private void Start()
    {
        time_lastInput = Time.time;
    }
    private void Update()
    {
        if (IsBurning)
        {
            //Debug.Log("ow !! ouch!1 im b urining!!");
        }
        else 
        {
            //Debug.Log($"Time left until burn: {burnTimer - (Time.time - time_lastInput)}");
        }

        

        float rotationalInput = 0f;
        if (Input.GetKeyDown(KeyCode.Space) )
        {
            toggle_panGravity = !toggle_panGravity;
            
        }
        if (Input.GetKey(KeyCode.A))
        {
            rotationalInput = 1f;
            time_lastInput = Time.time;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rotationalInput = -1f;
            time_lastInput = Time.time;
        }

        if (rotationalInput != 0)
        {
            RotatePan(rotationalInput, Time.deltaTime);
        }
        else 
        {
            if (!toggle_panGravity) {
                ApplyGravity(Time.deltaTime);
            }
        }
    }

    private void RotatePan(float direction, float deltaTime)
    {



        currentZRotation += direction * rotationSpeed * deltaTime;
        currentZRotation = Mathf.Clamp(currentZRotation, -maxRotation, maxRotation);

        Vector3 currentRotation = panPivot.localEulerAngles;
        panPivot.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, currentZRotation);
        //Debug.Log($"Rotation: {currentZRotation}");
    }

    private void ApplyGravity(float deltaTime) 
    {
        if (Mathf.Abs(currentZRotation) > gravityThreshold)
        { 
            float gravityDirection = Mathf.Sign(currentZRotation);
            float target = gravityDirection * maxRotation;

            float pastThreshold = (Mathf.Abs(currentZRotation) - gravityThreshold) / (maxRotation - gravityThreshold);
            float scaledGravitySpeed = gravitySpeed * pastThreshold;

            currentZRotation = Mathf.MoveTowards(currentZRotation, target, scaledGravitySpeed * deltaTime);
            Vector3 currentRotation = panPivot.localEulerAngles;

            panPivot.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, currentZRotation);
            //Debug.Log($"Rotation: {currentZRotation}");
        }
    }
}
