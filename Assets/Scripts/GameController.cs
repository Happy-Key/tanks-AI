using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float backwardSpeed;
    [SerializeField] private float turnSpeed;

    [Header("NN Settings")]
    [SerializeField] private int[] layerSizes;
    [SerializeField] private Vector2Int concurrentBoards;
    [SerializeField] private float roundLength;
    [SerializeField] private int numRoundsPerGeneration;
    [SerializeField] private float mutationStrength;
    [SerializeField] private float fitnessPadding;
    [SerializeField] private float difficultyGrowth;
    [SerializeField] private float fitnessToProgress;

    [Header("Prefabs")]
    [SerializeField] private GameObject pTank;
    [SerializeField] private GameObject pTarget;
    [SerializeField] private GameObject pShell;
    [SerializeField] private GameObject pGround;
    [SerializeField] private GameObject pWall;

    [Header("Test")]
    [SerializeField] private float timeScale;

    private List<Tank> tankList = new List<Tank>();
    private List<Tank> unassignedTanks = new List<Tank>();
    private Transform[] boards;

    private float roundTimer = 0f;
    private int roundCounter = 0;
    private float difficulty = 0f;
    private int generationCounter = 0;


    private void Start()
    {
        Time.timeScale = timeScale;
        CreateBoards();
        CreateTanks();
        StartRound();
    }

    private void FixedUpdate()
    {
        TankUpdate();
    }

    private void Update()
    {
        roundTimer += Time.deltaTime;
        if (roundTimer >= roundLength)
        {
            EndRound();
            StartRound();
            roundTimer = 0;
        }
    }

    private void CreateBoards()
    {
        boards = new Transform[concurrentBoards.x * concurrentBoards.y];
        for (int i = 0; i < concurrentBoards.x; i++)
        {
            for (int j = 0; j < concurrentBoards.y; j++)
            {
                GameObject terrain = new GameObject();
                terrain.name = "Board (" + (i * concurrentBoards.y + j) + ")";
                terrain.transform.parent = transform;
                Transform board = terrain.transform;
                board.localPosition = new Vector3((i - concurrentBoards.x / 2f) * (gridSize.x + 3f), 0f, (j - concurrentBoards.y / 2f) * (gridSize.y + 3f));
                for (int x = -1; x <= gridSize.x; x++)
                {
                    for (int y = -1; y <= gridSize.y; y++)
                    {
                        if (x == -1 || x == gridSize.x || y == -1 || y == gridSize.y)
                        {
                            GameObject w = GameObject.Instantiate(pWall, board);
                            w.transform.localPosition = new Vector3(x - gridSize.x / 2f + 0.5f, 0f, y - gridSize.y / 2f + 0.5f);
                            continue;
                        }
                        GameObject g = GameObject.Instantiate(pGround, board);
                        g.transform.localPosition = new Vector3(x - gridSize.x / 2f + 0.5f, -0.5f, y - gridSize.y / 2f + 0.5f);
                    }
                }
                boards[i * concurrentBoards.y + j] = transform.Find("Board (" + (i * concurrentBoards.y + j) + ")");
            }
        }
    }

    private void CreateTanks()
    {
        for (int i = 0; i < concurrentBoards.x; i++)
        {
            for (int j = 0; j < concurrentBoards.y; j++)
            {
                Tank tank = new Tank(layerSizes);
                unassignedTanks.Add(tank);
            }
        }
    }

    private void StartRound()
    {
        for (int i = 0; i < concurrentBoards.x; i++)
        {
            for (int j = 0; j < concurrentBoards.y; j++)
            {
                foreach(MeshRenderer child in boards[i * concurrentBoards.y + j].GetComponentsInChildren<MeshRenderer>())
                {
                    child.material.color = unassignedTanks[0].color;
                }
                GameObject t = GameObject.Instantiate(pTank, boards[i * concurrentBoards.y + j]);
                t.transform.localPosition = new Vector3((gridSize.x - 1f) * Random.Range(-0.5f, -0.5f + difficulty), -0.4f, (gridSize.y - 1f) * Random.Range(-0.5f, -0.5f + difficulty));
                t.transform.localEulerAngles = new Vector3(0f, (Random.value * 360f - 180f) * difficulty + 45f, 0f);
                GameObject target = GameObject.Instantiate(pTarget, boards[i * concurrentBoards.y + j]);
                target.name = "Target";
                target.transform.localPosition = new Vector3((gridSize.x - 1f) * Random.Range(0.5f - difficulty, 0.5f), -0.4f, (gridSize.y - 1f) * Random.Range(0.5f - difficulty, 0.5f));

                //Random Assignment
                /*
                int choose = Random.Range(0, unassignedTanks.Count);
                Tank tank = unassignedTanks[choose];
                tankList.Add(tank);
                unassignedTanks.RemoveAt(choose);
                */

                //Ordered Assignment
                Tank tank = unassignedTanks[0];
                tank.obj = t;
                tank.target = target;

                tankList.Add(tank);
                unassignedTanks.RemoveAt(0);
            }
        }
    }

    private void EndRound()
    {
        for (int i = 0; i < tankList.Count; i++)
        {
            float distToTarget = Vector3.Distance(tankList[i].obj.transform.position, tankList[i].target.transform.position);
            float roundScore = 0f;
            roundScore += Mathf.Max(5f - distToTarget, 0f);
            roundScore += 2f * Mathf.Max(1 + Vector3.Dot(tankList[i].obj.transform.forward, (tankList[i].target.transform.position-tankList[i].obj.transform.position).normalized),
                                                2f - distToTarget,
                                                0f);
            roundScore *= roundScore;
            tankList[i].score += roundScore;
        }

        for (int i = 0; i < concurrentBoards.x; i++)
        {
            for (int j = 0; j < concurrentBoards.y; j++)
            {
                Destroy(boards[i * concurrentBoards.y + j].Find("Tank(Clone)").gameObject);
                Destroy(boards[i * concurrentBoards.y + j].Find("Target").gameObject);
            }
        }

        roundCounter++;
        if (roundCounter >= numRoundsPerGeneration)
        {
            roundCounter = 0;
            Selection();
            Reproduction();
            generationCounter++;
        }
        else unassignedTanks = tankList;

        tankList = new List<Tank>();
    }

    private void Selection()
    {
        float totalScore = 0f;
        for (int i = 0; i < tankList.Count; i++)
        {
            tankList[i].score /= numRoundsPerGeneration;
            tankList[i].score += fitnessPadding;
            totalScore += tankList[i].score;
        }
        float avgScore = totalScore / (boards.Length) - fitnessPadding;
        Debug.Log(generationCounter + " : " + avgScore + " : " + difficulty);
        if (avgScore > fitnessToProgress)
        {
            difficulty = Mathf.Min(1f, difficulty + difficultyGrowth);
        }
        for (int r = 0; r < Mathf.Ceil(boards.Length / 2f); r++)
        {
            float rand = Random.Range(0f, totalScore);
            for (int i = 0; i < tankList.Count; i++)
            {
                rand -= tankList[i].score;
                if (rand <= 0f)
                {
                    totalScore -= tankList[i].score;
                    unassignedTanks.Add(tankList[i]);
                    tankList.RemoveAt(i);
                    break;
                }
            }
        }
        for (int i = 0; i < unassignedTanks.Count; i++)
        {
            unassignedTanks[i].score = 0;
        }
    }

    private void Reproduction()
    {
        for (int i = unassignedTanks.Count - 1; i >= 0; i--)
        {
            unassignedTanks.Add(new Tank(unassignedTanks[i], mutationStrength));
            if (unassignedTanks.Count >= concurrentBoards.x * concurrentBoards.y) break;
        }
    }

    private void TankUpdate()
    {
        for (int i = 0; i < tankList.Count; i++)
        {
            Transform t = tankList[i].obj.transform;
            Transform target = tankList[i].target.transform;
            float[] inputs = {  target.position.x - t.position.x,
                                target.position.z - t.position.z,
                                t.forward.x,
                                t.forward.z };
            float[] outputs = tankList[i].nn.FeedForward(inputs);
            float max = 0f;
            int index = 0;
            for (int m = 1; m < 5; m++)
            {
                if (outputs[m] > max)
                {
                    max = outputs[m];
                    index = m;
                }
            }
            switch (index)
            {
                case 1:
                    t.position += t.forward * Time.fixedDeltaTime * forwardSpeed;
                    break;
                case 2:
                    t.position -= t.forward * Time.fixedDeltaTime * backwardSpeed;
                    break;
                case 3:
                    t.Rotate(new Vector3(0f, Time.fixedDeltaTime * turnSpeed, 0f));
                    break;
                case 4:
                    t.Rotate(new Vector3(0f, -1f * Time.fixedDeltaTime * turnSpeed, 0f));
                    break;
                default:
                    break;
            }
        }
    }
}
