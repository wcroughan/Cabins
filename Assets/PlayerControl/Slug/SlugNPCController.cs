using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SlugMotor))]
public class SlugNPCController : MonoBehaviour
{
    [SerializeField]
    PlayerManager playerManager;

    private SlugMotor motor;
    private Vector2 userMovementInput;

    // Start is called before the first frame update
    void Start()
    {
        motor = GetComponent<SlugMotor>();
    }

    void OnEnable()
    {
        Debug.Log("The NPC slug is becoming active!");
    }

    void OnDisable()
    {
    }

    // Update is called once per frame
    void Update()
    {
        motor.PerformAction(SlugMotor.SlugAction.MoveForward);
    }
}
