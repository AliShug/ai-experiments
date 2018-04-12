using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public struct SystemState
{
    public int x, z;

    public SystemState(int p1, int p2)
    {
        x = p1;
        z = p2;
    }
}

// North = +Z, depth
// East = +X, width
public enum Direction : int
{
    NORTH, EAST, SOUTH, WEST
}

public class Agent : MonoBehaviour
{
    public int superSpeed = 50;
    public int worldWidth = 5;
    public int worldDepth = 5;
    public float startEpsilon = 1.0f;
    public float learningRate = 0.5f;
    public float gamma = 0.99f;
    public float moveReward = -0.1f;
    public float punishment = -2.0f;
    public float success = 10.0f;
    public int numPunishments = 20;
    public GameObject punishmentDisplay;
    public GameObject floorObj;

    private SystemState currentState_ = new SystemState(0, 0);
    private float[][][] q_;
    private Vector2Int[] bads_;
    private Vector2Int succs_;

	// Use this for initialization
	void Start()
    {
        q_ = new float[worldWidth][][];
        
        // Initialize rewards
        for (int i = 0; i < worldWidth; i++)
        {
            q_[i] = new float[worldDepth][];
            for (int j = 0; j < worldDepth; j++)
            {
                q_[i][j] = new float[] { 0, 0, 0, 0 };
            }
        }

        // Initialize punishments
        bads_ = new Vector2Int[numPunishments];
        for (int i = 0; i < numPunishments; i++) {
            bads_[i].x = Random.Range(0, worldWidth);
            bads_[i].y = Random.Range(0, worldDepth);

            GameObject disp = GameObject.Instantiate(punishmentDisplay);
            disp.transform.localPosition = new Vector3(bads_[i].x, 0.5f, bads_[i].y);
        }

        // Size the floor
        floorObj.transform.localScale = new Vector3(worldWidth, 1.0f, worldDepth);
        floorObj.transform.localPosition = new Vector3(worldWidth / 2.0f - 0.5f, -0.5f, worldDepth / 2.0f - 0.5f);
    }
	
    void Reset()
    {
        currentState_ = new SystemState(0, 0);
    }

	// Update is called once per frame
	void FixedUpdate()
    {
        Direction action;

        for (int i = 0; i < superSpeed; i++)
        {
            if (Random.Range(0.0f, 1.0f) > startEpsilon)
            {
                // Greedy choice
                float best = -9999999;
                int bestA = 0;
                for (int a = 0; a < 4; a++)
                {
                    float qV = q_[currentState_.x][currentState_.z][a];
                    if (qV > best)
                    {
                        best = qV;
                        bestA = a;
                    }
                }
                action = (Direction)bestA;
            }
            else
            {
                action = (Direction)Random.Range(0, 4);
            }
            TakeAction(action);
        }
	}

    void Update()
    {
        UpdateVisible();
    }

    void TakeAction(Direction d)
    {
        SystemState newState = currentState_;
        switch (d)
        {
            case Direction.NORTH:
                newState.z++;
                if (newState.z >= worldDepth)
                {
                    newState.z = worldDepth - 1;
                }
                break;
            case Direction.EAST:
                newState.x++;
                if (newState.x >= worldWidth)
                {
                    newState.x = worldWidth - 1;
                }
                break;
            case Direction.SOUTH:
                newState.z--;
                if (newState.z < 0)
                {
                    newState.z = 0;
                }
                break;
            case Direction.WEST:
                newState.x--;
                if (newState.x < 0)
                {
                    newState.x = 0;
                }
                break;
        }

        // check for punishment
        bool done = false;
        float dR = moveReward;
        for (int i = 0; i < numPunishments; i++)
        {
            if (bads_[i].x == newState.x && bads_[i].y == newState.z)
            {
                dR += punishment;
                done = true;
                break;
            }
        }
        // check for success
        if (newState.x == worldWidth - 1 && newState.z == worldDepth - 1)
        {
            dR += success;
            done = true;
        }
        Learn(newState.x, newState.z, (int)d, dR, done);
        currentState_ = newState;

        if (done)
        {
            Reset();
        }
    }

    void UpdateVisible()
    {
        Vector3 localPos = transform.localPosition;
        localPos.x = currentState_.x;
        localPos.z = currentState_.z;
        transform.localPosition = localPos;
    }

    void Learn(int newX, int newZ, int action, float reward, bool done)
    {
        if (done)
        {
            q_[currentState_.x][currentState_.z][action] += learningRate * (reward - q_[currentState_.x][currentState_.z][action]);
        }
        else
        {
            q_[currentState_.x][currentState_.z][action] += learningRate * (reward + gamma * q_[newX][newZ].Max() - q_[currentState_.x][currentState_.z][action]);
        }
    }
}
