using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum Knowledge : int
{
    NONE,
    VISITED_A,
    VISITED_B,
}

public class State
{
    public int x, z;
    public Knowledge knows = Knowledge.NONE; 

    public void Set(State other)
    {
        x = other.x;
        z = other.z;
        knows = other.knows;
    }

    public void Set(int p1, int p2, Knowledge k = Knowledge.NONE)
    {
        x = p1;
        z = p2;
        knows = k;
    }

    public State(int p1, int p2, Knowledge k = Knowledge.NONE)
    {
        x = p1;
        z = p2;
        knows = k;
    }
}
