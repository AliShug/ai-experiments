using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class TabQ
{
    private Dictionary<int, double[]> q_ = new Dictionary<int, double[]>();
    private int width_, depth_, nActions_;
    private double startVal_;
    private Environment e_;

    public double this[State s, Action a]
    {
        get
        {
            int k = StateToKey(s);
            if (q_.ContainsKey(k))
            {
                return q_[k][a.iVal];
            }
            else
            {
                return startVal_;
            }
        }
        set
        {
            int k = StateToKey(s);
            SafeGet(k)[a.iVal] = value;
        }
    }

    private double[] SafeGet(int k)
    {
        if (q_.ContainsKey(k))
        {
            return q_[k];
        }
        else
        {
            // Create the double array entry
            q_[k] = new double[nActions_];
            for (int i = 0; i < nActions_; i++)
            {
                q_[k][i] = startVal_;
            }
            return q_[k];
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
        int k = StateToKey(s);
        int nBest = 0;

        for (int i = 0; i < nActions_; i++)
        {
            double v = SafeGet(k)[i];
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
        int k = StateToKey(s);
        for (int i = 0; i < nActions_; i++)
        {
            double v = SafeGet(k)[i];
            if (v > best)
            {
                best = v;
            }
        }

        return best;
    }

    public double Max(int x, int z, Knowledge knows)
    {
        double best = Mathf.NegativeInfinity;
        int k = IndexToKey(x, z, (int) knows);
        for (int i = 0; i < nActions_; i++)
        {
            double v = SafeGet(k)[i];
            if (v > best)
            {
                best = v;
            }
        }

        return best;
    }

    public double Avg(int x, int z, Knowledge knows)
    {
        double sum = 0.0f;
        int k = IndexToKey(x, z, (int) knows);
        for (int i = 0; i < nActions_; i++)
        {
            sum += SafeGet(k)[i];
        }

        return sum / nActions_;
    }

    private int StateToKey(State s)
    {
        return IndexToKey(s.x, s.z, (int) s.knows);
    }

    private int IndexToKey(int x, int z, int k)
    {
        return x* width_ + z + (width_ * depth_ * k);
    }
}
