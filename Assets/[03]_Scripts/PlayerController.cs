using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Animator animator;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {

        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {

        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {

        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {

        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            SlowMode();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Shoot();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            Bomb();
        }
    }

    void SlowMode()
    {

    }

    void Shoot()
    {

    }

    void Bomb()
    {

    }
}
