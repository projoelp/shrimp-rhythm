using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanController : MonoBehaviour
{
    [SerializeField] private Transform panPivot;
    [SerializeField] private float rotationSpeed = 120f;
    private float maxRotation = 180f;

    private float currentZRotation = 0f;
    private float gravityThreshold = 5f;
    private float gravitySpeed = 300f;

    private void FixedUpdate()
    {
        float rotationalInput = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            rotationalInput = 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rotationalInput = -1f;
        }

        if (rotationalInput != 0)
        {
            RotatePan(rotationalInput, Time.deltaTime);
        }
        else 
        {
            ApplyGravity(Time.deltaTime);
        }
    }

    private void RotatePan(float direction, float deltaTime)
    {



        currentZRotation += direction * rotationSpeed * deltaTime;
        currentZRotation = Mathf.Clamp(currentZRotation, -maxRotation, maxRotation);

        Vector3 currentRotation = panPivot.localEulerAngles;
        panPivot.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, currentZRotation);
        Debug.Log($"Rotation: {currentZRotation}");
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
            Debug.Log($"Rotation: {currentZRotation}");
        }
    }
}
