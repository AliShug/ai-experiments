using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum Knowledge : int
{
    NONE,
    VISITED_A,
    VISITED_B,
}

public class State
{
    public Vector2Int agent = new Vector2Int(0, 0);
    public Vector2Int package = new Vector2Int(0, 0);
    public Knowledge knows = Knowledge.NONE; 

    public int x
    {
        get { return agent.x; }
        set { agent.x = value; }
    }

    public int z
    {
        get { return agent.y; }
        set { agent.y = value; }
    }

    public void Set(State other)
    {
        agent = other.agent;
        package = other.package;
        knows = other.knows;
    }

    public void Set(int p1, int p2, Knowledge k = Knowledge.NONE)
    {
        agent.x = p1;
        agent.y = p2;
        knows = k;
    }

    public State(int p1 = 0, int p2 = 0, Knowledge k = Knowledge.NONE)
    {
        agent.x = p1;
        agent.y = p2;
        knows = k;
    }

    // Hash map overrides
    public override bool Equals(object obj)
    {
        State other = obj as State;
        if (other == null)
        {
            return false;
        }
        return Equals(other);
    }

    public bool Equals(State other)
    {
        return agent == other.agent &&
                package == other.package &&
                knows == other.knows;
    }

    public override int GetHashCode()
    {
        int hash = 13;
        unchecked
        {
            hash = (hash * 7) + agent.GetHashCode();
            hash = (hash * 7) + package.GetHashCode();
            hash = (hash * 7) + knows.GetHashCode();
        }
        return hash;
    }
}
