using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlugIdleFlavorAnimation : StateMachineBehaviour
{
    const int numIdleAnimations = 1;
    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        animator.SetInteger("IdleAnimationIndex", Random.Range(0, numIdleAnimations));
    }
}
