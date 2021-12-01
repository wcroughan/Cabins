using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlugAnimationEventHandler : MonoBehaviour
{
    [SerializeField]
    SlugController slugController;

    void OnExitTransitionAnimationFinished()
    {
        slugController.OnExitTransitionAnimationFinished();
    }
}
