using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class Agent : MonoBehaviour
{
    [Range(1, 8000)]
    public int superSpeed = 50;

    [Range(0f, 1f)]
    public float startEpsilon = 1.0f;
    [Range(0f, 1f)]
    public float endEpsilon = 0.02f;
    public int epsilonShiftEpisodes = 10000;
    [Range(0f, 1f)]
    public double learningRate = 0.2f;
    [Range(0f, 1f)]
    public double gamma = 0.99f;

    public Text textDisplay;
    public Environment env;

    private double epsilon_;
    private State currentState_ = new State(0, 0);
    private State nextState_ = new State(0, 0);
    private TabQ q_;
    private int episodes_ = 0;
    private int wins_ = 0;
    private int fails_ = 0;
    private double reward_ = 0;
    private double lastReward_ = 0;
    private double expected_ = 0;
    private double bestReward_ = Mathf.NegativeInfinity;
    private double sumReward_ = 0;

    private const int nSaves_ = 500;
    private double[] savedRewards_ = new double[nSaves_];
    private bool[] savedWins_ = new bool[nSaves_];
    private int saveInd_ = 0;

    private StringBuilder sb_ = new StringBuilder();
    private TrailRenderer trail_;

	// Use this for initialization
	void Start()
    {
        trail_ = GetComponent<TrailRenderer>();
        q_ = new TabQ(env, 0.0f);
        epsilon_ = startEpsilon;
        Reset();
    }
	
    void Reset()
    {
        env.GetStartState(currentState_);
        reward_ = 0.0f;
    }

    // Fixed update called reliably on timer
	void FixedUpdate()
    {
        Action action = null;
        for (int i = 0; i < superSpeed; i++)
        {
            if (Random.Range(0.0f, 1.0f) > epsilon_)
            {
                // Greedy choice
                action = q_.ArgMax(currentState_);
                expected_ = q_.Max(currentState_);
            }
            else
            {
                action = q_.ArgRand(currentState_);
            }
            TakeAction(action);
        }
	}

    void TakeAction(Action a)
    {
        // Observe results of taking the action in our environment
        double reward;
        bool done;
        env.GetTransition(currentState_, nextState_, a, out reward, out done);

        if (learningRate > 0)
        {
            // Learn from our mistakes (and successes)
            Learn(currentState_, nextState_, a, reward, done);
        }
        reward_ += reward;

        // Episodic logic
        if (done)
        {
            if (env.IsReward(nextState_))
            {
                EndEpisode(true, reward_);
            }
            else if (env.IsPunishment(nextState_))
            {
                EndEpisode(false, reward_);
            }
        }
        else
        {
            if (reward_ < -1000)
            {
                // Early termination
                EndEpisode(false, reward_);
            }
            else
            {
                currentState_.Set(nextState_);
            }
        }
    }

    void Learn(State s0, State s1, Action a, double r, bool done)
    {
        if (done)
        {
            q_[s0, a] += learningRate * (r - q_[s0, a]);
        }
        else
        {
            q_[s0, a] += learningRate * (r + gamma * q_.Max(s1) - q_[s0, a]);
        }
    }

    // Update is called once per frame - update display
    void Update()
    {
        Vector3 localPos = transform.localPosition;
        localPos.x = currentState_.x;
        localPos.z = currentState_.z;
        transform.localPosition = localPos;

        // calculate average reward and win/loss ratio
        double sum = 0;
        int wins = 0;
        for (int i = 0; i < nSaves_; i++)
        {
            sum += savedRewards_[i];
            wins += savedWins_[i] ? 1 : 0;
        }
        int fails = nSaves_ - wins;

        // UI
        sb_.Length = 0;
        sb_.AppendFormat("Episodes: {0}", episodes_).AppendLine();
        sb_.AppendFormat("Wins: {0}", wins_).AppendLine();
        sb_.AppendFormat("Losses: {0}", fails_).AppendLine();
        sb_.AppendFormat("Current reward: {0:F2}", reward_).AppendLine();
        sb_.AppendFormat("Expected reward: {0:F2}", expected_ + reward_).AppendLine();
        sb_.AppendFormat("Last reward: {0:F2}", lastReward_).AppendLine();
        sb_.AppendFormat("Best reward: {0:F2}", bestReward_).AppendLine();
        sb_.AppendFormat("Last {1} W/L: {0:F3}", (double)wins/fails, nSaves_).AppendLine();
        sb_.AppendFormat("Last {1} average: {0:F2}", sum / nSaves_, nSaves_).AppendLine();
        sb_.AppendFormat("Epsilon: {0:F3}", epsilon_).AppendLine();
        textDisplay.text = sb_.ToString();

        env.RefreshFloorTexture(q_, currentState_);
    }

    private void EndEpisode(bool win, double reward)
    {
        epsilon_ = Mathf.Lerp(startEpsilon, endEpsilon, (float)episodes_ / epsilonShiftEpisodes);
        episodes_++;
        lastReward_ = reward;
        if (reward_ > bestReward_) bestReward_ = reward;
        sumReward_ += reward;

        if (win)
        {
            wins_++;
        }
        else
        {
            fails_++;
        }
        savedRewards_[saveInd_] = reward;
        savedWins_[saveInd_] = win;
        saveInd_ = (saveInd_ + 1) % nSaves_;

        Reset();
    }
}
