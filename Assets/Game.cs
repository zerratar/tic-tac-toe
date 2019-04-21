using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

public class Game : MonoBehaviour
{
    // Start is called before the first frame update

    public Object Player1;

    public Object Player2;

    public Transform GameObjects;

    public Transform[] Placements;

    public int[] Grid = new int[9];

    public GameObject GameBoard;

    public Text Log;

    public GameObject RedTeamWins, BlueTeamWins, NoTeamWins, ResetText;

    public float Player2ZOffset = -1.63f;

    private readonly List<string> players = new List<string>();

    private int playerTurn;

    private GameObject[] teams;
    private bool gameOver;
    private int dummyIndex = 0;

    private GameServer server = new GameServer();

    public float ShrinkTimer = 1f;
    public float TimeToShrink = 1f;

    public float GrowTimer = 1f;
    public float TimeToGrow = 1f;

    void Start()
    {
        server.Start();
        teams = new[]
        {
            NoTeamWins, BlueTeamWins, RedTeamWins
        };
        ResetText.SetActive(false);
        RedTeamWins.SetActive(false);
        BlueTeamWins.SetActive(false);
        NoTeamWins.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        HideGameBoard();

        for (var i = 0; i < 9; ++i)
        {
            var key = (KeyCode)(KeyCode.Alpha1 + i);
            if (Input.GetKeyUp(key))
            {
                Play(null, "Dummy" + (++dummyIndex), i + 1);
                //Grid[i] = (GameObject)GameObject.Instantiate(Player2, GameObjects);
                //var transformPosition = Placements[i].position;
                //var targetPosition = new Vector3(transformPosition.x, transformPosition.y + 1, transformPosition.z + Player2ZOffset);
                //Grid[i].transform.position = targetPosition;
            }
        }

        if (Input.GetKeyUp(KeyCode.Delete))
        {
            this.ResetGame(null);
        }

        if (server.IsBound)
        {
            var packet = server.ReadPacket();

            if (packet?.Command == null)
            {
                return;
            }

            Log.text = packet.Command;

            var nameSplit = packet.Command.IndexOf(':');
            var playerName = packet.Command.Remove(nameSplit);
            var command = packet.Command.Substring(nameSplit + 1);
            if (command.StartsWith("reset", StringComparison.InvariantCultureIgnoreCase))
            {
                ResetGame(packet.Client);
            }
            else //if (command.IndexOf("play ", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                command = command.Replace("play ", "");
                var num = int.Parse(command);
                if (num > 0)
                {
                    Play(packet.Client, playerName, num);
                }
            }
        }
    }

    private void HideGameBoard()
    {
        if (!this.gameOver || !(ShrinkTimer > 0))
        {
            return;
        }

        ShrinkTimer -= Time.deltaTime;
        if (ShrinkTimer <= 0)
        {
            this.GameBoard.transform.localScale = Vector3.zero;
        }
        else
        {
            var xyz = ShrinkTimer / TimeToShrink;
            this.GameBoard.transform.localScale = new Vector3(xyz, xyz, xyz);
        }
    }

    //private void ShowGameOver()
    //{
    //    if (this.gameOver && GrowTimer > 0)
    //    {
    //        GrowTimer -= Time.deltaTime;
    //        if (GrowTimer <= 0)
    //        {

    //            this.GameBoard.transform.localScale = Vector3.zero;
    //        }
    //        else
    //        {
    //            var xyz = ShrinkTimer / TimeToShrink;
    //            this.GameBoard.transform.localScale = new Vector3(xyz, xyz, xyz);
    //        }
    //    }
    //}

    public void Play(GameClient client, string playerName, int slot)
    {
        int playerIndex;
        if (!this.players.Contains(playerName))
        {
            this.players.Add(playerName);
            playerIndex = this.players.IndexOf(playerName) % 2;
            client?.Write(playerName + ":join|" + playerIndex);
        }
        else
        {
            playerIndex = this.players.IndexOf(playerName) % 2;
        }

        if (gameOver || playerTurn != playerIndex)
        {
            RejectPlay(client, playerName, false);
            return;
        }

        var i = slot - 1;
        if (i < 0 || i > Grid.Length)
        {
            RejectPlay(client, playerName, true);
            return;
        }

        var zOffset = playerIndex == 0 ? 0 : Player2ZOffset;
        var obj = (GameObject)GameObject.Instantiate(playerIndex == 0 ? Player1 : Player2, GameObjects);
        Grid[i] = playerIndex + 1;
        var transformPosition = Placements[i].position;
        var targetPosition = new Vector3(transformPosition.x, transformPosition.y + 1, transformPosition.z + zOffset);
        obj.transform.position = targetPosition;
        playerTurn = (++playerTurn) % 2;

        client?.Write(playerName + ":play|" + slot);

        if (CheckForWin(Grid, out var winner))
        {
            EndGame(client, winner);
        }
    }

    public bool CheckForWin(int[] grid, out int playerIndex)
    {
        playerIndex = -1;

        if (grid == null || grid.Length != 9) return false;

        var gridFilled = true;

        // horizontal
        if (CheckLines(grid, ref playerIndex, ref gridFilled, (row, col) => row * 3 + col))
        {
            return true;
        }

        // horizontal test
        if (CheckLines(grid, ref playerIndex, ref gridFilled, (row, col) => col * 3 + row))
        {
            return true;
        }

        // diagonal test
        if (CheckLines(grid, ref playerIndex, ref gridFilled, (dir, i) => dir == 0 ? i * 3 + i : 2 + 2 * i, 2))
        {
            return true;
        }

        if (gridFilled)
        {
            playerIndex = 0;
            return true;
        }

        return false;
    }

    private static bool CheckLines(
        int[] grid,
        ref int playerIndex,
        ref bool gridFilled,
        Func<int, int, int> getIndex,
        int aLength = 3,
        int bLength = 3)
    {
        for (var a = 0; a < aLength; ++a)
        {
            var lastPlayer = 0;
            var playerWinRow = true;
            for (var b = 0; b < bLength; ++b)
            {
                var index = getIndex(a, b);
                if (grid[index] == 0) gridFilled = false;
                if (b == 0) lastPlayer = grid[index];
                else if (grid[index] != lastPlayer)
                {
                    playerWinRow = false;
                }
            }

            if (playerWinRow && lastPlayer > 0)
            {
                playerIndex = lastPlayer;
                return true;
            }
        }
        return false;
    }

    private void EndGame(GameClient client, int result)
    {
        client?.Write("*:end_game|" + result);
        this.teams[result].SetActive(true);
        this.gameOver = true;
        this.ResetText.SetActive(true);
    }

    private void RejectPlay(GameClient client, string playerName, bool invalidMove)
    {
        if (gameOver)
        {
            client?.Write(playerName + ":reject|game_over|Action was rejected because the game is over.");
            Debug.Log(playerName + "'s action was rejected because the game is over.");
        }
        else if (invalidMove)
        {
            client?.Write(playerName + ":reject|invalid_move|Action was rejected as it was out of bounds.");
            Debug.Log(playerName + "'s action was rejected as it is not his/her turn yet.");
        }
        else
        {
            client?.Write(playerName + ":reject|not_your_turn|Action was rejected as it is not your turn yet.");
            Debug.Log(playerName + "'s action was rejected as it is not his/her turn yet.");
        }
    }

    private void ResetGame(GameClient client)
    {
        var toDelete = new List<GameObject>();
        for (var i = 0; i < GameObjects.childCount; ++i)
        {
            toDelete.Add(GameObjects.GetChild(i).transform.gameObject);
        }

        foreach (var delete in toDelete)
        {
            DestroyImmediate(delete);
        }

        for (var i = 0; i < 9; ++i) this.Grid[i] = 0;

        this.ShrinkTimer = this.TimeToShrink;
        this.GameBoard.transform.localScale = Vector3.one;

        this.players.Clear();
        this.playerTurn = 0;
        this.gameOver = false;
        this.RedTeamWins.SetActive(false);
        this.BlueTeamWins.SetActive(false);
        this.NoTeamWins.SetActive(false);
        this.ResetText.SetActive(false);
        client?.Write("*:reset");
    }
}

public enum GameResult : int
{
    Blue = 0,
    Red = 1,
    Draw = 2
}