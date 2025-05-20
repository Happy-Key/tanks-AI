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
                            w.transform.localPosition = new Vector3(x - gridSize.x / 2f, 0f, y - gridSize.y / 2f);
                            continue;
                        }
                        GameObject g = GameObject.Instantiate(pGround, board);
                        g.transform.localPosition = new Vector3(x - gridSize.x / 2f, -0.5f, y - gridSize.y / 2f);
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
                GameObject t = GameObject.Instantiate(pTank, boards[i * concurrentBoards.y + j]);
                t.transform.localPosition = new Vector3(gridSize.x * (Random.value - 0.5f) * 0.8f, -0.4f, gridSize.y * (Random.value - 0.5f) * 0.8f);
                t.transform.localEulerAngles = new Vector3(0f, Random.value * 360f, 0f);
                GameObject target = GameObject.Instantiate(pTarget, boards[i * concurrentBoards.y + j]);
                target.name = "Target";
                target.transform.localPosition = new Vector3(gridSize.x * (Random.value - 0.5f) * 0.8f, -0.4f, gridSize.y * (Random.value - 0.5f) * 0.8f);

                //Random Assignment
                /*
                int choose = Random.Range(0, unassignedTanks.Count);
                Tank tank = unassignedTanks[choose];
                tankList.Add(tank);
                unassignedTanks.RemoveAt(choose);
                */

                //Ordered Assignment
                Tank tank = unassignedTanks[0];
                tankList.Add(tank);
                unassignedTanks.RemoveAt(0);

                tank.obj = t;
                tank.target = target;
            }
        }
    }

    private void EndRound()
    {
        for (int i = 0; i < tankList.Count; i++)
        {
            float distToTarget = Vector3.Distance(tankList[i].obj.transform.position, tankList[i].target.transform.position);
            tankList[i].score += Mathf.Max(5f - distToTarget, 0f);
            tankList[i].score += 2f * Mathf.Max(1 + Vector3.Dot(tankList[i].obj.transform.forward, (tankList[i].target.transform.position-tankList[i].obj.transform.position).normalized),
                                                2f - distToTarget,
                                                0f);
        }
        roundCounter++;
        if (roundCounter >= numRoundsPerGeneration)
        {
            roundCounter = 0;
            Selection();
            Reproduction();
        }
        else unassignedTanks = tankList;
        

        for (int i = 0; i < concurrentBoards.x; i++)
        {
            for (int j = 0; j < concurrentBoards.y; j++)
            {
                Destroy(boards[i * concurrentBoards.y + j].Find("Tank(Clone)").gameObject);
                Destroy(boards[i * concurrentBoards.y + j].Find("Target").gameObject);
            }
        }
        tankList = new List<Tank>();
    }

    private void Selection()
    {
        int populationSize = tankList.Count;
        float totalScore = 0f;
        for (int i = 0; i < tankList.Count; i++)
        {
            tankList[i].score /= numRoundsPerGeneration;
            tankList[i].score += fitnessPadding;
            totalScore += tankList[i].score;
        }
        Debug.Log(totalScore / (concurrentBoards.x * concurrentBoards.y) - fitnessPadding);
        for (int r = 0; r < Mathf.Ceil(populationSize / 2f); r++)
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
