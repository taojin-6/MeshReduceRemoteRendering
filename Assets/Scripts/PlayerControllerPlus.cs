using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerPlus : MonoBehaviour
{
    float speed = 5.0f;
    // Start is called before the first frame update
    void Start()
    {
        speed = 5.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(-1 * Vector3.forward * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(Vector3.left * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(Vector3.down * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.PageUp))
        {
            transform.Rotate(-30 * Time.deltaTime, 0, 0);
        }
        if (Input.GetKey(KeyCode.PageDown))
        {
            transform.Rotate(30 * Time.deltaTime, 0, 0);
        }
    }
}
