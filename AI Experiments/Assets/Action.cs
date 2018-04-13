using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Action
{
    public int iVal;

    public Action(int i)
    {
        iVal = i;
    }

    public static implicit operator Action(Direction d)
    {
        return new Action((int)d);
    }
}
