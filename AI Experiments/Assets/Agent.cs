using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class Agent : MonoBehaviour
{
    public int stopAfter = 1000000;
    public int epsilonZeroPeriod = 50000;

    [Range(1, 50000)]
    public int superSpeed = 50;

    [Range(0f, 1f)]
    public float startEpsilon = 1.0f;
    [Range(0f, 1f)]
    public float endEpsilon = 0.02f;
    [Range(0f, 1f)]
    public float epsilonDecay = 0.001f;
    [Range(0f, 1f)]
    public float startLearningRate = 0.5f;
    [Range(0f, 1f)]
    public float endLearningRate = 0.0f;
    [Range(0f, 1f)]
    public float learningDecay = 0.0001f;
    [Range(0f, 1f)]
    public double gamma = 0.99f;

    public Text textDisplay;
    public Environment env;

    private double epsilon_, alpha_;
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

    private const int nSaves_ = 1000;
    private double[] savedRewards_ = new double[nSaves_];
    private bool[] savedWins_ = new bool[nSaves_];
    private int saveInd_ = 0;
    private bool stopped = false;

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

        if (alpha_ > 0f)
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
            if (reward_ < -50)
            {
                // Early termination
                Learn(currentState_, nextState_, a, env.punishmentCost, true);
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
            q_[s0, a] = (1 - alpha_) * q_[s0, a] + alpha_ * r;
        }
        else
        {
            q_[s0, a] = (1 - alpha_) * q_[s0, a] + alpha_ * (r + gamma * q_.Max(s1));
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
            wins += (savedWins_[i] ? 1 : 0);
        }
        int fails = nSaves_ - wins;

        // UI
        sb_.Length = 0;
        sb_.AppendFormat("Episodes: {0}", episodes_).AppendLine();
        sb_.AppendFormat("Wins: {0}", wins_).AppendLine();
        sb_.AppendFormat("Losses: {0}", fails_).AppendLine();
        sb_.AppendFormat("Current reward: {0:F2}", reward_).AppendLine();
        sb_.AppendFormat("Expected reward: {0:F2}", expected_ + reward_).AppendLine();
        sb_.AppendFormat("Qmax expected future reward: {0:F2}", expected_).AppendLine();
        sb_.AppendFormat("Last reward: {0:F2}", lastReward_).AppendLine();
        sb_.AppendFormat("Best reward: {0:F2}", bestReward_).AppendLine();
        sb_.AppendFormat("Last {1} W/L: {0:F3}", (double)wins/fails, nSaves_).AppendLine();
        sb_.AppendFormat("Last {1} average: {0:F2}", sum / nSaves_, nSaves_).AppendLine();
        sb_.AppendFormat("Alpha: {0:F3}", alpha_).AppendLine();
        sb_.AppendFormat("Epsilon: {0:F3}", epsilon_).AppendLine();
        if (stopped)
        {
            sb_.AppendLine("TRAINED!");
        }
        textDisplay.text = sb_.ToString();

        env.RefreshFloorTexture(q_, currentState_);
    }

    private void EndEpisode(bool win, double reward)
    {
        epsilon_ = endEpsilon + (startEpsilon-endEpsilon) * Mathf.Pow(1f-epsilonDecay, episodes_);
        alpha_ = endLearningRate + (startLearningRate - endLearningRate) * Mathf.Pow(1f - learningDecay, episodes_);
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

        // Learning stop logic
        if (!stopped && episodes_ > stopAfter)
        {
            alpha_ = endLearningRate = startLearningRate = 0;
            epsilon_ = startEpsilon = endEpsilon = 0;
            superSpeed = 1;
            stopped = true;
        }

        Reset();
    }
}
