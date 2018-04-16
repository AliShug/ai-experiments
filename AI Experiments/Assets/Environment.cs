using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public enum Tile
{
    EMPTY,
    START,
    REWARD_A,
    REWARD_B,
    PUNISHMENT,
    WALL,
}

public enum WorldType
{
    REWARDS_A,
    REWARDS_B,
    REWARDS_BOTH,
}

// North = +Z, depth
// East = +X, width
public enum Direction : int
{
    NORTH, EAST, SOUTH, WEST
}

[ExecuteInEditMode]
public class Environment : MonoBehaviour
{
    [Range(3, 200)]
    public int width = 10, depth = 10;

    public double moveCost = -0.05f;
    public double successReward = 2.0f;
    public double punishmentCost = -2.0f;
    
    // How likely we are to end up in an adjacent square
    public double slidiness = 0.1f;

    public int nRewards = 2;
    public int nPunishments = 5;

    public GameObject punishmentDisplayObj;
    public GameObject rewardADisplayObj, rewardBDisplayObj;
    public GameObject floorObj;
    public Camera camera;
    public double cameraHeight = 5.0f;

    public bool randomizeReward = false;
    public bool randomStart = false;
    public Vector2Int startPos = new Vector2Int(0, 0);

    public bool displayExpectedReward = false;
    public bool displayPolicy = false;

    // World representation
    private Tile[][] world_;
    private Action[] actions_;
    private WorldType worldType_ = WorldType.REWARDS_BOTH;

    // Spawned object logic
    private GameObject floor_;
    private bool dirty_ = false;

    // Floor texture
    Texture2D floorTexture_;

    public void GenerateRandomWorld()
    {
        Clear();

        // Initialize the world
        world_ = new Tile[width][];
        for (int x = 0; x < width; x++)
        {
            world_[x] = new Tile[depth];
            for (int z = 0; z < depth; z++)
            {
                world_[x][z] = Tile.EMPTY;
            }
        }
        world_[startPos.x][startPos.y] = Tile.START;

        // Initialize punishments and rewards
        for (int i = 0; i < nPunishments; i++)
        {
            bool valid = false;
            int x, z;
            do
            {
                x = Random.Range(0, width);
                z = Random.Range(0, depth);
                if (world_[x][z] == Tile.EMPTY)
                {
                    world_[x][z] = Tile.PUNISHMENT;
                    valid = true;
                }
            } while (!valid);

            GameObject disp = GameObject.Instantiate(punishmentDisplayObj, transform);
            disp.transform.localPosition = new Vector3(x, 0.5f, z);
        }
        for (int i = 0; i < nRewards; i++)
        {
            bool valid = false;
            int x, z;
            do
            {
                x = Random.Range(0, width);
                z = Random.Range(0, depth);
                if (world_[x][z] == Tile.EMPTY)
                {
                    world_[x][z] = Tile.REWARD_A;
                    valid = true;
                }
            } while (!valid);

            GameObject disp = GameObject.Instantiate(rewardADisplayObj, transform);
            disp.transform.localPosition = new Vector3(x, 0.5f, z);
        }
        for (int i = 0; i < nRewards; i++)
        {
            bool valid = false;
            int x, z;
            do
            {
                x = Random.Range(0, width);
                z = Random.Range(0, depth);
                if (world_[x][z] == Tile.EMPTY)
                {
                    world_[x][z] = Tile.REWARD_B;
                    valid = true;
                }
            } while (!valid);

            GameObject disp = GameObject.Instantiate(rewardBDisplayObj, transform);
            disp.transform.localPosition = new Vector3(x, 0.5f, z);
        }
    }

    public void LoadExistingWorld()
    {
        // Initialize the world
        world_ = new Tile[width][];
        for (int x = 0; x < width; x++)
        {
            world_[x] = new Tile[depth];
            for (int z = 0; z < depth; z++)
            {
                world_[x][z] = Tile.EMPTY;
            }
        }

        // Load world based on child objects
        foreach (Transform child in transform)
        {
            GameObject obj = child.gameObject;

            if (obj.tag == "FloorObj") floor_ = obj;
            else if (obj.tag == "Tile_Punishment")
            {
                world_[(int)child.localPosition.x][(int)child.localPosition.z] = Tile.PUNISHMENT;
            }
            else if (obj.tag == "Tile_Reward_A")
            {
                world_[(int)child.localPosition.x][(int)child.localPosition.z] = Tile.REWARD_A;
            }
            else if (obj.tag == "Tile_Reward_B")
            {
                world_[(int)child.localPosition.x][(int)child.localPosition.z] = Tile.REWARD_B;
            }
            else if (obj.tag == "Tile_Reward_B")
            {
                world_[(int)child.localPosition.x][(int)child.localPosition.z] = Tile.REWARD_B;
            }
            else if (obj.tag == "Tile_Wall")
            {
                world_[(int)child.localPosition.x][(int)child.localPosition.z] = Tile.WALL;
            }
        }

        // Size the floor correctly
        floor_.transform.localScale = new Vector3(width, 1.0f, depth);
        floor_.transform.localPosition = new Vector3(width / 2.0f - 0.5f, -0.5f, depth / 2.0f - 0.5f);
    }

    private void Start()
    {
        // Initialize actions
        actions_ = new Action[] {
            Direction.NORTH,
            Direction.EAST,
            Direction.SOUTH,
            Direction.WEST
        };

        LoadExistingWorld();

        // Place and size camera
        camera.orthographicSize = Mathf.Max(width, depth) / 2;
        camera.transform.localPosition = new Vector3(width / 2.0f - 0.5f, 50f, depth / 2.0f - 0.5f);

        InitFloorTexture();
    }
    
    // Modifies the state 'start' to be the starting state
    public void GetStartState(State start)
    {
        if (randomStart)
        {
            bool valid = false;
            int x, z;
            do
            {
                x = Random.Range(0, width);
                z = Random.Range(0, depth);
                if (world_[x][z] == Tile.EMPTY || world_[x][z] == Tile.START)
                {
                    valid = true;
                }
            } while (!valid);
            start.Set(x, z);
        }
        else
        {
            start.Set(startPos.x, startPos.y);
        }

        if (randomizeReward)
        {
            worldType_ = (WorldType) Random.Range(0, 2);
        }
        else
        {
            worldType_ = WorldType.REWARDS_BOTH;
        }
    }

    // Modifies the state 'next' to be the state resulting from the transition
    public void GetTransition(State current, State next, Action a, out double reward, out bool end)
    {
        reward = moveCost;

        // Slipping (which occurs randomly, with some fixed probability)
        // results in a slide in a random direction perpendicular to the
        // intended direction of travel
        bool slip = Random.Range(0f, 1f) < slidiness;
        int slide = (Random.Range(0f, 1f) < 0.5f) ? -1 : +1;
        next.Set(current);
        switch ((Direction)a.iVal)
        {
            case Direction.NORTH:
                next.z++;
                // Bounce back off walls/edges without sliding
                if (IsWall(next))
                {
                    next.z--;
                    reward += moveCost * 5;
                }
                else if (slip)
                {
                    next.x += slide;
                    if (IsWall(next))
                    {
                        next.x -= slide;
                        reward += moveCost * 5;
                    }
                }
                break;
            case Direction.EAST:
                next.x++;
                if (IsWall(next))
                {
                    next.x--;
                    reward += moveCost * 5;
                }
                else if (slip)
                {
                    next.z += slide;
                    if (IsWall(next))
                    {
                        next.z -= slide;
                        reward += moveCost * 5;
                    }
                }
                break;
            case Direction.SOUTH:
                next.z--;
                if (IsWall(next))
                {
                    next.z++;
                    reward += moveCost * 5;
                }
                else if (slip)
                {
                    next.x += slide;
                    if (IsWall(next))
                    {
                        next.x -= slide;
                        reward += moveCost * 5;
                    }
                }
                break;
            case Direction.WEST:
                next.x--;
                if (IsWall(next))
                {
                    next.x++;
                    reward += moveCost * 5;
                }
                else if (slip)
                {
                    next.z += slide;
                    if (IsWall(next))
                    {
                        next.z -= slide;
                        reward += moveCost * 5;
                    }
                }
                break;
        }

        // Calculate the reward, and determine if a final state was reached
        end = false;
        if (worldType_ == WorldType.REWARDS_BOTH)
        {
            if (IsReward(next))
            {
                reward += successReward;
                end = true;
            }
        }
        else if (worldType_ == WorldType.REWARDS_A)
        {
            if (IsReward(next, Tile.REWARD_A))
            {
                reward += successReward;
                end = true;
            }
            else if (IsReward(next, Tile.REWARD_B))
            {
                // Agent gets no reward, but now knows it's not in a REWARDS_B world
                next.knows = Knowledge.VISITED_B;
            }
        }
        else if (worldType_ == WorldType.REWARDS_B)
        {
            if (IsReward(next, Tile.REWARD_B))
            {
                reward += successReward;
                end = true;
            }
            else if (IsReward(next, Tile.REWARD_A))
            {
                // Now knows it's not in a REWARDS_A world
                next.knows = Knowledge.VISITED_A;
            }
        }

        // Punishments are the same for all world types
        if (IsPunishment(next))
        {
            reward += punishmentCost;
            end = true;
        }
    }

    public bool IsWall(State s)
    {
        return  s.x < 0 ||
                s.x >= width ||
                s.z < 0 ||
                s.z >= depth ||
                world_[s.x][s.z] == Tile.WALL;
    }

    public bool IsReward(State s)
    {
        return world_[s.x][s.z] == Tile.REWARD_A || world_[s.x][s.z] == Tile.REWARD_B;
    }

    public bool IsReward(State s, Tile rewardType = Tile.REWARD_A)
    {
        return world_[s.x][s.z] == rewardType;
    }

    public bool IsPunishment(State s)
    {
        return world_[s.x][s.z] == Tile.PUNISHMENT;
    }

    public Action[] GetActions()
    {
        return actions_;
    }

    private void InitFloorTexture()
    {
        // Fuck with the floor coloring
        floorTexture_ = new Texture2D(width*3, depth*3);
        floorTexture_.filterMode = FilterMode.Point;
        floor_.GetComponent<Renderer>().sharedMaterial.mainTexture = floorTexture_;

        for (int y = 0; y < floorTexture_.height; y++)
        {
            for (int x = 0; x < floorTexture_.width; x++)
            {
                Color color = ((x & y) != 0 ? Color.white : Color.gray);
                floorTexture_.SetPixel(x, y, color);
            }
        }
        floorTexture_.Apply();
    }

    private Action actionSlot;
    public void RefreshFloorTexture(TabQ q, State s)
    {
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color;
                if (displayExpectedReward)
                {
                    float val = (float)q.Max(x, z, s.knows);
                    if (val > 0 && val < 5)
                    {
                        color = Color.LerpUnclamped(Color.gray, Color.yellow, val / 5);
                    }
                    else if (val >= 5)
                    {
                        color = Color.LerpUnclamped(Color.yellow, Color.white, (val - 5) / 5);
                    }
                    else
                    {
                        color = Color.LerpUnclamped(Color.black, Color.red, -val);
                    }
                }
                else
                {
                    color = Color.black;
                }
                // Apply color
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        floorTexture_.SetPixel(x * 3 + j, z * 3 + i, color);
                    }
                }
                // Show Q direction
                if (displayPolicy)
                {
                    Direction d = (Direction)q.ArgMax(x, z, s.knows);
                    switch (d)
                    {
                        case Direction.NORTH:
                            floorTexture_.SetPixel(x * 3 + 1, z * 3 + 1, Color.gray);
                            floorTexture_.SetPixel(x * 3 + 1, z * 3 + 2, Color.white);
                            break;
                        case Direction.SOUTH:
                            floorTexture_.SetPixel(x * 3 + 1, z * 3 + 1, Color.gray);
                            floorTexture_.SetPixel(x * 3 + 1, z * 3, Color.white);
                            break;
                        case Direction.EAST:
                            floorTexture_.SetPixel(x * 3 + 1, z * 3 + 1, Color.gray);
                            floorTexture_.SetPixel(x * 3 + 2, z * 3 + 1, Color.white);
                            break;
                        case Direction.WEST:
                            floorTexture_.SetPixel(x * 3 + 1, z * 3 + 1, Color.gray);
                            floorTexture_.SetPixel(x * 3, z * 3 + 1, Color.white);
                            break;
                    }
                }
            }
        }
        floorTexture_.Apply();
    }

    // Editor update logic

    void Update()
    {
        if (!Application.isPlaying && dirty_)
        {
            Start();
            dirty_ = false;
        }
    }

    public void Clear()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // Create and size the floor
        floor_ = Instantiate(floorObj, transform);
        floor_.transform.localScale = new Vector3(width, 1.0f, depth);
        floor_.transform.localPosition = new Vector3(width / 2.0f - 0.5f, -0.5f, depth / 2.0f - 0.5f);
    }

    private void OnValidate()
    {
        dirty_ = true;

        if (nPunishments + nRewards > width * depth - 2)
        {
            nPunishments = width * depth - 2 - nRewards;
        }
        if (nPunishments < 0)
        {
            nPunishments = 0;
        }
        if (nRewards > width * depth - 2)
        {
            nRewards = width * depth - 2;
        }

        if (width > 200)
        {
            width = 200;
        }

        if (depth > 200)
        {
            depth = 200;
        }
    }
}
