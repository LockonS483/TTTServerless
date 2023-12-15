using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketService : MonoBehaviour
{
    public const string RequestStartOp = "1";
    public const string PlayingOp = "11";
    public const string TurnOp = "5";
    public const string YouWonOp = "91";
    public const string YouLostOp = "92";

    Manager _manager;

    private bool intentionalClose = false;
    private WebSocket _websocket;
    private string _wsDNS = "wss://prad2ooqf3.execute-api.us-west-1.amazonaws.com/production/";
    void Update()
    {
        if (_websocket != null)
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            _websocket.DispatchMessageQueue();
        #endif
        }
    }

    private void SetupWebsocketCallbacks()
    {
        _websocket.OnOpen += () =>
        {
            Debug.Log("Connection Open!");
            intentionalClose = false;
            GameMessage startRequest = new GameMessage("OnMessage", RequestStartOp);
            SendWebsocketMessage(JsonUtility.ToJson(startRequest));
        };

        _websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        _websocket.OnClose += (e) =>
        {
            Debug.Log("Connection Closed!");

            if (!intentionalClose)
            {
                //something here
            }
        };

        _websocket.OnMessage += (bytes) =>
        {
            Debug.Log("OnMessage!");
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log(message);

            ProcessReceivedMessage(message);
        };
    }

    private void ProcessReceivedMessage(string msg)
    {
        GameMessage gameMessage = JsonUtility.FromJson<GameMessage>(msg);
        if (gameMessage.uuid == null) return;

        if (gameMessage.opcode == PlayingOp) 
        {
            //playing
            _manager.BeginPlaying();
        }
        else if (gameMessage.opcode == TurnOp)
        {
            //received turn from other player
            string turnstring = gameMessage.message;
            PlayerActions[] actions = ConvertActions(turnstring);

            _manager.ReceiveActions(actions);
        }
        else if (gameMessage.opcode == YouWonOp)
        {
            //won game
            _manager.EndGame(true);
        }
        else if (gameMessage.opcode == YouLostOp)
        {
            //lost game
            _manager.EndGame(false);
        }
    }

    public async void SendWebsocketMessage(string message)
    {
        if (_websocket.State == WebSocketState.Open)
        {
            await _websocket.SendText(message);
            Debug.Log("MESSAGE SENT: " + message);
        }
    }

    //ACTIONS
    // 0 - wait
    // 1 - forward
    // 2 - backward
    // 3 - attack
    // 4 - block
    // 5 - recover

    private PlayerActions[] ConvertActions(string str)
    {
        PlayerActions[] a = new PlayerActions[Manager.maxActions];

        for(int i=0; i<Manager.maxActions; i++)
        {
            switch (str[i])
            {
                case '0':
                    a[i] = PlayerActions.Wait; break;
                case '1':
                    a[i] = PlayerActions.Forward; break;
                case '2':
                    a[i] = PlayerActions.Backward; break;
                case '3':
                    a[i] = PlayerActions.Attack; break;
                case '4':
                    a[i] = PlayerActions.Block; break;
                case '5':
                    a[i] = PlayerActions.Recover; break;
            }
        }

        return a;
    }

    private string ConstructTurnMessage(PlayerActions[] actions)
    {
        string msg = "";
        foreach (PlayerActions a in actions)
        {
            switch (a)
            {
                case PlayerActions.Wait:
                    msg += "0"; break;
                case PlayerActions.Forward:
                    msg += "1"; break;
                case PlayerActions.Backward:
                    msg += "2"; break;
                case PlayerActions.Attack:
                    msg += "3"; break;
                case PlayerActions.Block:
                    msg += "4"; break;
                case PlayerActions.Recover:
                    msg += "5"; break;
                default:
                    msg += "0"; break;
            }
        }
        GameMessage gmsg = new GameMessage("OnMessage", WebSocketService.TurnOp, msg);
        return JsonUtility.ToJson(gmsg);
    }

    public void SendWinMessage()
    {
        GameMessage gmsg = new GameMessage("OnMessage", WebSocketService.YouWonOp);
        string mstr = JsonUtility.ToJson(gmsg);
        SendWebsocketMessage(mstr);
    }

    async public void JoinMatch()
    {
        await _websocket.Connect();
    }

    public void SendTurnActions(PlayerActions[] actions)
    {
        string msg = ConstructTurnMessage(actions);
        SendWebsocketMessage(msg);
    }

    public async void QuitMatch()
    {
        intentionalClose = true;
        await _websocket.Close();
    }

    public async void OnApplicationQuit()
    {
        await _websocket.Close();
    }

    public void InitializeGame()
    {
        _manager = GetComponent<Manager>();
        Debug.Log("Websocket Service Start");
        intentionalClose = false;

        _websocket = new WebSocket(_wsDNS);
        SetupWebsocketCallbacks();
        JoinMatch();
    }

    public void GameOver()
    {
        SendWinMessage();
    }

    /*public void TestTurnMessage()
    {
        PlayerActions[] testactions = new PlayerActions[4] { PlayerActions.Wait, PlayerActions.Wait, PlayerActions.Forward, PlayerActions.Backward };
        SendTurnActions(testactions);
    }*/
}
