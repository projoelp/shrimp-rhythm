using UnityEngine;

public class InputTest : MonoBehaviour
{
    private void Update()
    {
        // Test if Update is even running
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"InputTest Update running - Frame {Time.frameCount}");
        }

        // Test every possible key
        if (Input.anyKeyDown)
        {
            Debug.Log("SOME KEY WAS PRESSED!");
        }

        // Specific tests
        if (Input.GetKeyDown(KeyCode.Space))
            Debug.Log("SPACE PRESSED");

        if (Input.GetKeyDown(KeyCode.A))
            Debug.Log("A PRESSED");

        if (Input.GetKeyDown(KeyCode.D))
            Debug.Log("D PRESSED");

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Debug.Log("LEFT ARROW PRESSED");

        if (Input.GetKeyDown(KeyCode.RightArrow))
            Debug.Log("RIGHT ARROW PRESSED");

        // Test mouse
        if (Input.GetMouseButtonDown(0))
            Debug.Log("MOUSE CLICKED");
    }
}