using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


class State
{
    public int x, z;

    public void Set(State other)
    {
        x = other.x;
        z = other.z;
    }

    public void Set(int p1, int p2)
    {
        x = p1;
        z = p2;
    }

    public State(int p1, int p2)
    {
        x = p1;
        z = p2;
    }
}
