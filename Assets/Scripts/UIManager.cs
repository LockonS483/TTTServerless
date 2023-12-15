using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIState
{
    menu,
    waiting,
    playing,
    gameover
}

public class UIManager : MonoBehaviour
{
    public string displayState = "";
    public Sprite wait, forward, backward, attack1, attack2, block, noneS;
    public Sprite checkEmpty, checkFilled;
    public Image[] actionBlocks;
    public Image[] turnReadyChecks;

    public GameObject menuObjects;
    public GameObject gameplayObjects;
    public GameObject gameoverObjects;

    public TMP_Text statusText;
    public TMP_Text gameoverText;

    public UIState uistate;

    public void UpdateStatusText(string txt)
    {
        statusText.text = txt;
    }

    public void UpdateGameoverText(string txt)
    {
        gameoverText.text = txt;
    }

    // Start is called before the first frame update
    public void UpdateUIState(UIState s)
    {
        uistate = s;
        if (uistate == UIState.menu)
        {
            menuObjects.SetActive(true);
            gameplayObjects.SetActive(false);
            gameoverObjects.SetActive(false);
        }
        else if (uistate == UIState.waiting)
        {
            menuObjects.SetActive(false);
            gameplayObjects.SetActive(false);
            gameoverObjects.SetActive(false);
        }
        else if (uistate == UIState.playing)
        {
            menuObjects.SetActive(false);
            gameplayObjects.SetActive(true);
            gameoverObjects.SetActive(false);
        }
        else if (uistate == UIState.gameover)
        {
            menuObjects.SetActive(false);
            gameplayObjects.SetActive(false);
            gameoverObjects.SetActive(true);
        }
    }

    public void UpdateIcons(PlayerActions[] actions)
    {
        for(int i=0; i<actions.Length; i++)
        {
            UpdateIcon(i, actions[i]);
        }
    }

    public void UpdateIcon(int i, PlayerActions action)
    {
        Image cSprite = actionBlocks[i];
        switch (action)
        {
            case PlayerActions.Wait:
                //idle action
                cSprite.sprite = wait;
                break;
            case PlayerActions.Forward:
                cSprite.sprite = forward;
                break;
            case PlayerActions.Backward:
                cSprite.sprite = backward;
                break;
            case PlayerActions.Attack:
                cSprite.sprite = attack1;
                break;
            case PlayerActions.Recover:
                cSprite.sprite = attack2;
                break;
            case PlayerActions.Block:
                cSprite.sprite = block;
                break;
            default:
                cSprite.sprite = noneS;
                break;
        }
    }

    public void UpdateCheckboxes(int i, bool filled)
    {
        Sprite s = checkEmpty;
        if (filled)
        {
            s = checkFilled;
        }
        turnReadyChecks[i].sprite = s;
    }

    public void BackToMenu()
    {
        UpdateUIState(UIState.menu);
    }
}
