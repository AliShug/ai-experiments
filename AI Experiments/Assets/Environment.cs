using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

enum Tile
{
    EMPTY,
    START,
    REWARD,
    PUNISHMENT
}

// North = +Z, depth
// East = +X, width
public enum Direction : int
{
    NORTH, EAST, SOUTH, WEST
}

[ExecuteInEditMode]
class Environment : MonoBehaviour
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
    public GameObject rewardDisplayObj;
    public GameObject floorObj;
    public Camera camera;
    public double cameraHeight = 5.0f;

    public bool randomStart = false;
    public Vector2Int startPos = new Vector2Int(0, 0);

    // World representation
    private Tile[][] world_;
    private Action[] actions_;

    // Spawned object clean up
    private GameObject[] spawnedObjects_;
    private bool initialized_ = false;
    private bool dirty_ = false;

    // Floor texture
    Texture2D floorTexture_;

    private void Start()
    {
        if (initialized_)
        {
            Clear();
        }

        // Initialize actions
        actions_ = new Action[] {
            Direction.NORTH,
            Direction.EAST,
            Direction.SOUTH,
            Direction.WEST
        };

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
        spawnedObjects_ = new GameObject[nPunishments + nRewards];
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

            GameObject disp = GameObject.Instantiate(punishmentDisplayObj);
            disp.transform.localPosition = new Vector3(x, 0.5f, z);
            spawnedObjects_[i] = disp;
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
                    world_[x][z] = Tile.REWARD;
                    valid = true;
                }
            } while (!valid);

            GameObject disp = GameObject.Instantiate(rewardDisplayObj);
            disp.transform.localPosition = new Vector3(x, 0.5f, z);
            spawnedObjects_[nPunishments + i] = disp;
        }

        // Size the floor
        floorObj.transform.localScale = new Vector3(width, 1.0f, depth);
        floorObj.transform.localPosition = new Vector3(width / 2.0f - 0.5f, -0.5f, depth / 2.0f - 0.5f);

        // Fuck with the floor coloring
        floorTexture_ = new Texture2D(width, depth);
        floorObj.GetComponent<Renderer>().material.mainTexture = floorTexture_;

        for (int y = 0; y < floorTexture_.height; y++)
        {
            for (int x = 0; x < floorTexture_.width; x++)
            {
                Color color = ((x & y) != 0 ? Color.white : Color.gray);
                floorTexture_.SetPixel(x, y, color);
            }
        }
        floorTexture_.Apply();

        // we done
        initialized_ = true;
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
    }

    // Modifies the state 'next' to be the state resulting from the transition
    public void GetTransition(State current, State next, Action a, out double reward, out bool end)
    {
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
                if (slip) next.x += slide;
                break;
            case Direction.EAST:
                next.x++;
                if (slip) next.z += slide;
                break;
            case Direction.SOUTH:
                next.z--;
                if (slip) next.x += slide;
                break;
            case Direction.WEST:
                next.x--;
                if (slip) next.z += slide;
                break;
        }

        // Clamp to world boundaries
        if (next.z >= depth)
        {
            next.z = depth - 1;
        }
        else if (next.z < 0)
        {
            next.z = 0;
        }

        if (next.x >= width)
        {
            next.x = width - 1;
        }
        if (next.x < 0)
        {
            next.x = 0;
        }

        // Calculate the reward, and determine if a final state was reached
        reward = moveCost;
        end = false;
        if (IsReward(next))
        {
            reward += successReward;
            end = true;
        }
        else if (IsPunishment(next))
        {
            reward += punishmentCost;
            end = true;
        }
    }

    public bool IsReward(State s)
    {
        return world_[s.x][s.z] == Tile.REWARD;
    }

    public bool IsPunishment(State s)
    {
        return world_[s.x][s.z] == Tile.PUNISHMENT;
    }

    public Action[] GetActions()
    {
        return actions_;
    }

    public void RefreshFloorTexture(TabQ q)
    {
        for (int y = 0; y < floorTexture_.height; y++)
        {
            for (int x = 0; x < floorTexture_.width; x++)
            {
                float val = (float)q.Max(x, y);
                if (val > 0 && val < 5)
                {
                    Color color = Color.LerpUnclamped(Color.gray, Color.yellow, val/5);
                    floorTexture_.SetPixel(x, y, color);
                }
                else if (val >= 5)
                {
                    Color color = Color.LerpUnclamped(Color.yellow, Color.white, (val-5)/5);
                    floorTexture_.SetPixel(x, y, color);
                }
                else
                {
                    Color color = Color.LerpUnclamped(Color.black, Color.red, -val);
                    floorTexture_.SetPixel(x, y, color);
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

    private void Clear()
    {
        if (spawnedObjects_ != null)
        {
            foreach (GameObject o in spawnedObjects_)
            {
                DestroyImmediate(o);
            }
        }
        initialized_ = false;
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

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += StateChange;
    }

    private void StateChange(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode && initialized_)
        {
            Debug.Log("exiting edit mode " + initialized_);
            Debug.Log(state);
            Clear();
        }
    }
}
