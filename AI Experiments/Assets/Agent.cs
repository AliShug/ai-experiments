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
    public int shiftTime = 10000;
    [Range(0f, 3f)]
    public double learningRate = 0.5f;
    [Range(0f, 1f)]
    public double gamma = 0.99f;

    public Text textDisplay;

    private double epsilon_;
    private State currentState_ = new State(0, 0);
    private State nextState_ = new State(0, 0);
    private Environment e_;
    private TabQ q_;
    private int episodes_ = 0;
    private int wins_ = 0;
    private int fails_ = 0;
    private double reward_ = 0;
    private double lastReward_ = 0;
    private double expected_ = 0;
    private double bestReward_ = Mathf.NegativeInfinity;
    private double sumReward_ = 0;

    private StringBuilder sb_ = new StringBuilder();

	// Use this for initialization
	void Start()
    {
        e_ = GetComponent<Environment>();
        q_ = new TabQ(e_, 0.0f);
        epsilon_ = startEpsilon;
        Reset();
    }
	
    void Reset()
    {
        e_.GetStartState(currentState_);
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
        e_.GetTransition(currentState_, nextState_, a, out reward, out done);

        if (learningRate > 0)
        {
            // Learn from our mistakes (and successes)
            Learn(currentState_, nextState_, a, reward, done);
        }
        reward_ += reward;

        // Episodic logic
        if (done)
        {
            if (e_.IsReward(nextState_))
            {
                wins_++;
            }
            else if (e_.IsPunishment(nextState_))
            {
                fails_++;
            }
            episodes_++;
            lastReward_ = reward_;
            if (reward_ > bestReward_) bestReward_ = reward_;
            sumReward_ += reward_;
            epsilon_ = Mathf.Lerp(startEpsilon, endEpsilon, (float)episodes_ / shiftTime);
            Reset();
        }
        else
        {
            currentState_.Set(nextState_);
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

        // UI
        sb_.Length = 0;
        sb_.AppendFormat("Episodes: {0}", episodes_).AppendLine();
        sb_.AppendFormat("Wins: {0}", wins_).AppendLine();
        sb_.AppendFormat("Losses: {0}", fails_).AppendLine();
        sb_.AppendFormat("Win/loss ratio: {0:F3}", (double)wins_/fails_).AppendLine();
        sb_.AppendFormat("Current reward: {0:F2}", reward_).AppendLine();
        sb_.AppendFormat("Expected reward: {0:F2}", expected_ + reward_).AppendLine();
        sb_.AppendFormat("Last reward: {0:F2}", lastReward_).AppendLine();
        sb_.AppendFormat("Best reward: {0:F2}", bestReward_).AppendLine();
        sb_.AppendFormat("Avg reward: {0:F2}", sumReward_ / episodes_).AppendLine();
        sb_.AppendFormat("Epsilon: {0:F3}", epsilon_).AppendLine();
        textDisplay.text = sb_.ToString();

        e_.RefreshFloorTexture(q_);
    }
}
