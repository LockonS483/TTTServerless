using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Animator charAnimator;
    public SpriteRenderer sprite;
    string currentState = "";
    bool isDead = false;

    Color c = Color.white;

    // Start is called before the first frame update
    void Start()
    {
        ChangeAnimationState("Idle");
        /*
        ChangeAnimationState("Run");
        ChangeAnimationState("Block");
        ChangeAnimationState("Attack");
        ChangeAnimationState("Recover");
        */
        InitGame();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
        {
            Color curC = sprite.color;
            curC.a -= 0.55f * Time.deltaTime;

            sprite.color = curC;
        }
    }

    public void GetHit()
    {
        isDead = true;
    }

    public void InitGame()
    {
        sprite.color = c;
        isDead = false;
        ChangeAnimationState("Idle");
    }

    public void TurnReset()
    {
        if (currentState == "Run" || currentState == "Block")
        {
            ChangeAnimationState("Idle");
        }
    }

    public void ExecuteAction(PlayerActions targetAction)
    {
        if (isDead)
            return;

        switch (targetAction)
        {
            case PlayerActions.Wait:
                //idle action
                ChangeAnimationState("Idle");
                break;
            case PlayerActions.Forward:
                ChangeAnimationState("Idle");
                break;
            case PlayerActions.Backward:
                ChangeAnimationState("Idle");
                break;
            case PlayerActions.Attack:
                ChangeAnimationState("Attack");
                break;
            case PlayerActions.Recover:
                ChangeAnimationState("Recover");
                break;
            case PlayerActions.Block:
                ChangeAnimationState("Block");
                break;
            default:
                break;
        }
    }

    void ChangeAnimationState(string newState)
    {
        if (currentState == newState) return;

        charAnimator.Play(newState);

        currentState = newState;
    }
}
