using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SlugMotor))]
public class SlugNPCController : MonoBehaviour
{
    private SlugMotor motor;
    private GameObject nextTarget;
    [SerializeField]
    SlugStats stats;
    private int numLeftTurnsLeft, numRightTurnsLeft, numStepsLeft;

    void Awake()
    {
        motor = GetComponent<SlugMotor>();
        numLeftTurnsLeft = 0;
        numRightTurnsLeft = 0;
        numStepsLeft = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        motor.controllerCallback += OnLastActionFinished;
        SendNextActionToMotor();
    }

    void OnEnable()
    {
        // Debug.Log($"Slug {name} is active");
    }

    void OnDisable()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnLastActionFinished()
    {
        SendNextActionToMotor();
    }

    private void SendNextActionToMotor()
    {
        // Debug.Log($"Slug {name} is doing a thing", this.gameObject);
        if (numStepsLeft > 0)
        {
            numStepsLeft--;
            motor.PerformAction(SlugMotor.SlugAction.MoveForward);
            return;
        }
        else if (numLeftTurnsLeft > 0)
        {
            numLeftTurnsLeft--;
            motor.PerformAction(SlugMotor.SlugAction.TurnLeft);
            return;
        }
        else if (numRightTurnsLeft > 0)
        {
            numRightTurnsLeft--;
            motor.PerformAction(SlugMotor.SlugAction.TurnRight);
            return;
        }

        SelectNextTarget();
        if (nextTarget != null)
        {
            motor.SetNextLungeTarget(nextTarget);
            motor.PerformAction(SlugMotor.SlugAction.Lunge);
            return;
        }
        else
        {
            float r = Random.Range(0f, 1f);
            if (r < 0.25f)
            {
                numLeftTurnsLeft = Random.Range(1, 5);
                motor.PerformAction(SlugMotor.SlugAction.TurnLeft);
            }
            else if (r < 0.5f)
            {
                numRightTurnsLeft = Random.Range(1, 5);
                motor.PerformAction(SlugMotor.SlugAction.TurnRight);
            }
            else if (r < 0.75f)
            {
                numStepsLeft = Random.Range(2, 9);
                motor.PerformAction(SlugMotor.SlugAction.MoveForward);
            }
            else
            {
                // numStepsLeft = Random.Range(2, 9);
                // motor.PerformAction(SlugMotor.SlugAction.MoveForward);
                StartCoroutine(WaitBeforeNextAction(Random.Range(1f, 3f)));
                return;
            }
        }
    }

    private IEnumerator WaitBeforeNextAction(float delay)
    {
        yield return new WaitForSeconds(delay);

        SendNextActionToMotor();
    }

    private void SelectNextTarget()
    {
        Collider[] possibleTargets = Physics.OverlapSphere(transform.position, stats.targetSearchRadius);
        float minDist = float.PositiveInfinity;
        nextTarget = null;
        for (int i = 0; i < possibleTargets.Length; i++)
        {
            Transform t = possibleTargets[i].transform;
            Vector3 d = t.position - transform.position;
            if (d == Vector3.zero)
            {
                continue;
            }
            float a = Vector3.Angle(transform.forward, d);
            if (a < stats.targetMaxAngle)
            {
                float dds = d.sqrMagnitude;
                if (dds < minDist)
                {
                    nextTarget = possibleTargets[i].gameObject;
                    minDist = dds;
                }
            }

        }

    }

}
