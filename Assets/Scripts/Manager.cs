using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum PlayerActions
{
    Wait,
    Forward,
    Backward,
    Block,
    Attack,
    Recover,
    None
}

public class Manager : MonoBehaviour
{
    public const int maxActions = 4;

    public UIManager uiManager;
    public int actionIterator;
    //int minIterator = 0;
    public int actionPointer;
    public PlayerActions[] myActions;
    public PlayerActions[] enemyActions;

    float turnLength = 1f;
    float tileSize = 0.5f;

    public PlayerController controller1, controller2;

    bool canEdit = false;
    bool gameover = false;
    bool gotOppActions = false;
    bool gotMyActions = false;

    PlayerActions overflowedAction;

    int pos1, pos2;

    public RectTransform playturnPointer;
    const float playturnPointerPos = -268f;
    const float playturnPointerGap = 40f;

    WebSocketService wss;

    // Start is called before the first frame update
    void Start()
    {
        ResetGame();
    }

    public void ResetGame(bool resetUI = true)
    {
        pos1 = -1;
        pos2 = 1;
        myActions = new PlayerActions[maxActions] { PlayerActions.None, PlayerActions.None, PlayerActions.None, PlayerActions.None };
        enemyActions = new PlayerActions[maxActions] { PlayerActions.Backward, PlayerActions.Backward, PlayerActions.Forward, PlayerActions.Forward };
        gotOppActions = false;
        gotMyActions = false;
        wss = GetComponent<WebSocketService>();

        if (resetUI)
        {
            uiManager.UpdateIcons(myActions);
            uiManager.UpdateStatusText("Not Connected");
            uiManager.UpdateUIState(UIState.menu);
        }

        canEdit = false;
        gameover = false;

        controller1.InitGame();
        controller2.InitGame();
    }

    void Update()
    {
        Vector3 ps1 = controller1.transform.position;
        Vector3 target1 = new Vector3(pos1 * tileSize, 0, 0);

        Vector3 ps2 = controller2.transform.position;
        Vector3 target2 = new Vector3(pos2 * tileSize, 0, 0);


        controller1.transform.position = Vector3.Lerp(ps1, target1, 0.05f);
        controller2.transform.position = Vector3.Lerp(ps2, target2, 0.05f);

        if (Input.GetKeyDown(KeyCode.D))
        {
            ActionEdit(PlayerActions.Forward);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            ActionEdit(PlayerActions.Backward);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            ActionEdit(PlayerActions.Attack);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            ActionEdit(PlayerActions.Block);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            ActionEdit(PlayerActions.Wait);
        }else if (Input.GetKeyDown(KeyCode.Backspace)){
            ActionEdit(PlayerActions.None, true);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SubmitActions(true, new PlayerActions[maxActions]);
        }
    }

    void SubmitActions(bool mine, PlayerActions[] opp)
    {
        if (mine)
        {
            foreach (var ca in myActions)
            {
                if (ca == PlayerActions.None) return;
            }
            gotMyActions = true;
            uiManager.UpdateCheckboxes(0, gotMyActions);
            canEdit = false;
            wss.SendTurnActions(myActions);
        }
        else
        {
            for(int i=0; i < maxActions; i++)
            {
                if (opp[i] == PlayerActions.None) opp[i] = PlayerActions.Wait;
                enemyActions[i] = opp[i];
            }
            gotOppActions = true;
            uiManager.UpdateCheckboxes(1, gotOppActions);
        }

        if (gotMyActions && gotOppActions)
        {
            PlayTurn();
        }
    }

    void ResetTurn()
    {
        controller1.TurnReset();
        controller2.TurnReset();
        playturnPointer.gameObject.SetActive(false);

        actionPointer = 0;
        myActions = new PlayerActions[maxActions] { PlayerActions.None, PlayerActions.None, PlayerActions.None, PlayerActions.None };
        enemyActions = new PlayerActions[maxActions] { PlayerActions.Wait, PlayerActions.Wait, PlayerActions.Wait, PlayerActions.Wait };
        gotOppActions = false;
        gotMyActions = false;

        uiManager.UpdateCheckboxes(0, false);
        uiManager.UpdateCheckboxes(1, false);

        canEdit = true;
        if (overflowedAction != PlayerActions.None)
        {
            myActions[0] = overflowedAction;
            actionPointer++;
        }

        overflowedAction = PlayerActions.None;

        uiManager.UpdateIcons(myActions);
    }

    void ActionEdit(PlayerActions action, bool isRemoval=false) //num = -1 to remove action
    {
        if (gameover)
            return;

        if (!canEdit)
        {
            return;
        }
        if (isRemoval) //delete action
        {
            RemoveAction();
            return;
        }

        AddAction(action);
    }

    void OverflowAction(PlayerActions a)
    {
        overflowedAction = a;
    }

    void AddAction(PlayerActions a)
    {
        if (actionPointer >= maxActions)
        {
            return;
        }

        if (a == PlayerActions.Block)
        {
            myActions[actionPointer] = PlayerActions.Recover;
            actionPointer++;

            if (actionPointer < maxActions)
            {
                myActions[actionPointer] = PlayerActions.Block;
                actionPointer++;
            }
            else
            {
                OverflowAction(PlayerActions.Block);
            }

        }
        else 
        {
            myActions[actionPointer] = a;
            actionPointer++;
        }

        if (a == PlayerActions.Attack)
        {
            if (actionPointer < maxActions)
            {
                myActions[actionPointer] = PlayerActions.Recover;
                actionPointer++;
            }
            else
            {
                OverflowAction(PlayerActions.Recover);
            }
        }

        uiManager.UpdateIcons(myActions);
    }
    void RemoveAction()
    {
        if (actionPointer - 1 < 0)
        {
            return;
        }else if (actionPointer == maxActions)
        {
            //removing last action -> remove overflow as well
            overflowedAction = PlayerActions.None;
        }
        PlayerActions lastAction = myActions[actionPointer - 1];
        if (lastAction != PlayerActions.Recover && lastAction != PlayerActions.Block)
        {
            myActions[actionPointer - 1] = PlayerActions.None;
            actionPointer--;
        }
        else
        {
            if (actionPointer - 1 < 1)
            {
                //its an overflow action from last turn
                return;
            }
            if (lastAction == PlayerActions.Block || lastAction == PlayerActions.Recover)
            {
                //block, remove 2
                myActions[actionPointer - 1] = PlayerActions.None;
                actionPointer--;
                if (lastAction == PlayerActions.Recover)
                {
                    if (myActions[actionPointer - 1] == PlayerActions.Attack)
                    {
                        myActions[actionPointer - 1] = PlayerActions.None;
                        actionPointer--;
                    }
                }else if (lastAction == PlayerActions.Block)
                {
                    myActions[actionPointer - 1] = PlayerActions.None;
                    actionPointer--;
                }
                
            }
        }
        uiManager.UpdateIcons(myActions);
    }

    void PlayTurn()
    {
        canEdit = false;
        actionIterator = 0;
        playturnPointer.gameObject.SetActive(true);
        for(int i=0; i<maxActions; i++)
        {
            Invoke("DoAction", turnLength * i);
        }
        Invoke("ResetTurn", turnLength * maxActions);
    }

    int GetDistance(bool aftermove)
    {
        int ndist = Mathf.Abs(pos2 - pos1);
        if (!aftermove)
        {
            return ndist;
        }

        if (myActions[actionIterator] == PlayerActions.Forward)
        {
            ndist--;
        }
        else if (myActions[actionIterator] == PlayerActions.Backward)
        {
            ndist++;
        }

        if (enemyActions[actionIterator] == PlayerActions.Forward)
        {
            ndist--;
        }
        else if (enemyActions[actionIterator] == PlayerActions.Backward)
        {
            ndist++;
        }

        return ndist;
    }

    public bool MoveValid()
    {
        int ndist = GetDistance(true);

        if (ndist < 1)
        {
            return false;
        }

        return true;
    }

    public bool CheckAttack(bool aPlayer) //aplayer for player1, !aplayer for player2
    {
        if (GetDistance(true) <= 1)
        {
            if (aPlayer)
            {
                if (myActions[actionIterator] == PlayerActions.Attack && enemyActions[actionIterator] == PlayerActions.Block)
                    return false;
                if (myActions[actionIterator] == PlayerActions.Attack && enemyActions[actionIterator] != PlayerActions.Attack)
                    return true;
            }
            else
            {
                if (myActions[actionIterator] == PlayerActions.Block && enemyActions[actionIterator] == PlayerActions.Attack)
                    return false;
                if (myActions[actionIterator] != PlayerActions.Attack && enemyActions[actionIterator] == PlayerActions.Attack)
                    return true;
            }
        }

        return false;
    }

    void CMove(int c, int d) //character, direction
    {
        if (c == 1)
        {
            pos1 += d;
        }
        if (c == 2)
        {
            pos2 -= d;
        }
    }

    void DoAction()
    {
        var action1 = myActions[actionIterator];
        var action2 = enemyActions[actionIterator];
        playturnPointer.localPosition = new Vector3(playturnPointerPos + (playturnPointerGap * actionIterator), 30, 0);

        if (MoveValid())
        {
            if (action1 == PlayerActions.Forward)
            {
                CMove(1, 1);
            }
            else if (action1 == PlayerActions.Backward)
            {
                CMove(1, -1);
            }

            if (action2 == PlayerActions.Forward)
            {
                CMove(2, 1);
            }
            else if (action2 == PlayerActions.Backward)
            {
                CMove(2, -1);
            }
        }

        controller1.ExecuteAction(action1);
        controller2.ExecuteAction(action2);

        if (action1 == PlayerActions.Attack)
        {
            bool hit = CheckAttack(true);
            if (hit)
            {
                controller2.GetHit();
                wss.SendWinMessage();
                gameover = true;
            }
        }

        if (action2 == PlayerActions.Attack)
        {
            bool hit = CheckAttack(false);
            if (hit)
            {
                controller1.GetHit();
                gameover = true;
            }
        }

        actionIterator++;
    }

    public void StartGame()
    {
        wss.InitializeGame();
        uiManager.UpdateStatusText("waiting for server...");
        uiManager.UpdateUIState(UIState.waiting);
    }

    public void BeginPlaying()
    {
        uiManager.UpdateStatusText("connected");
        uiManager.UpdateUIState(UIState.playing);
        ResetTurn();
    }

    public void LeaveGame()
    {
        wss.QuitMatch();
        ResetGame();
    }

    public void ReceiveActions(PlayerActions[] acs)
    {
        SubmitActions(false, acs);
    }

    public void EndGame(bool didwin)
    {
        gameover = true;
        if (didwin)
        {
            uiManager.UpdateGameoverText("YOU WON");
        }
        else
        {
            uiManager.UpdateGameoverText("YOU LOST");
        }

        uiManager.UpdateUIState(UIState.gameover);
        uiManager.UpdateStatusText("Not Connected");
        wss.QuitMatch();
        CancelInvoke("DoAction");
        CancelInvoke("ResetTurn");
        //ResetGame(false);
    }
    public void BackToMenu()
    {
        uiManager.BackToMenu();
        ResetGame();
    } 

    public void TryWinning()
    {
        wss.SendWinMessage();
    }
}