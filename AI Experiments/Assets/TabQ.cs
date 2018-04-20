using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TabQ
{
    private Dictionary<State, double[]> q_ = new Dictionary<State, double[]>();
    private int width_, depth_, nActions_;
    private double startVal_;
    private Environment e_;
    private State localState_ = new State();

    public double this[State s, Action a]
    {
        get
        {
            if (q_.ContainsKey(s))
            {
                return q_[s][a.iVal];
            }
            else
            {
                return startVal_;
            }
        }
        set
        {
            SafeGet(s)[a.iVal] = value;
        }
    }

    private double[] SafeGet(State s)
    {
        if (q_.ContainsKey(s))
        {
            return q_[s];
        }
        else
        {
            // Create the double array entry
            State s_ = new State();
            s_.Set(s);
            q_[s_] = new double[nActions_];
            for (int i = 0; i < nActions_; i++)
            {
                q_[s][i] = startVal_;
            }
            return q_[s];
        }
    }

    public TabQ(Environment e, double startVal)
    {
        width_ = e.width;
        depth_ = e.depth;
        startVal_ = startVal;
        e_ = e;
        nActions_ = e.GetActions().Length;
    }

    public Action ArgMax(State s)
    {
        double best = Mathf.NegativeInfinity;
        int bestI = 0;
        int nBest = 0;

        for (int i = 0; i < nActions_; i++)
        {
            double v = SafeGet(s)[i];
            if (v > best)
            {
                nBest = 1;
                best = v;
                bestI = i;
                nBest = 1;
            }
            else if (v == best)
            {
                nBest++;
            }
        }

        return e_.GetActions()[bestI];
        /*if (nBest > 1)
        {
            return ArgRand(s);
        }
        else
        {
            return e_.GetActions()[bestI];
        }*/
    }

    public Action ArgRand(State s)
    {
        return e_.GetActions()[Random.Range(0, nActions_)];
    }

    public double Max(State s)
    {
        double best = Mathf.NegativeInfinity;
        for (int i = 0; i < nActions_; i++)
        {
            double v = SafeGet(s)[i];
            if (v > best)
            {
                best = v;
            }
        }

        return best;
    }

    public double Avg(State s)
    {
        double sum = 0.0f;
        for (int i = 0; i < nActions_; i++)
        {
            sum += SafeGet(s)[i];
        }

        return sum / nActions_;
    }

    private State IndexToKey(int x, int z, int k)
    {
        localState_.Set(x, z, (Knowledge) k);
        return localState_;
    }
}
