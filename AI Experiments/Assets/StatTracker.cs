using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

struct EpisodeData
{
    public double reward;
    public bool win;
    public int steps, ord;
    public double epsilon, alpha;

    public EpisodeData(int o, double r, bool w, int s, double e, double a)
    {
        ord = o;
        reward = r;
        win = w;
        steps = s;
        epsilon = e;
        alpha = a;
    }
}

public class StatTracker : MonoBehaviour
{
    private List<EpisodeData> trainingEps_ = new List<EpisodeData>();
    private List<EpisodeData> validationEps_ = new List<EpisodeData>();
    private Agent agent_;
    private DateTime startTime_;
    private int ord;

    public void StartRun(Agent agent)
    {
        trainingEps_.Clear();
        agent_ = agent;
        startTime_ = DateTime.Now;
        ord = 0;
    }

    public void EndRun()
    {
        // Construct file string in memory
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("epsilon: {0}, {1}, {2}", agent_.startEpsilon, agent_.endEpsilon, agent_.epsilonDecay).AppendLine();
        sb.AppendFormat("alpha: {0}, {1}, {2}", agent_.startLearningRate, agent_.endLearningRate, agent_.learningDecay).AppendLine();
        sb.AppendFormat("gamma: {0}", agent_.gamma).AppendLine();
        sb.AppendFormat("training_time: {0}", DateTime.Now - startTime_).AppendLine();
        sb.AppendFormat("training_episodes: {0}", trainingEps_.Count).AppendLine();
        sb.AppendFormat("validation_episodes: {0}", validationEps_.Count).AppendLine();

        sb.AppendLine("Training: ord, reward, win, steps");
        for (int i = 0; i < trainingEps_.Count; i++)
        {
            EpisodeData episode = trainingEps_[i];
            sb.AppendFormat("{0}, {1:F4}, {2}, {3}, {4}, {5}",
                episode.ord, episode.reward, episode.win ? 1 : 0, episode.steps,
                episode.epsilon, episode.alpha);
            sb.AppendLine();
        }
        sb.AppendLine("Validation: ord, reward, win, steps");
        for (int i = 0; i < validationEps_.Count; i++)
        {
            EpisodeData episode = validationEps_[i];
            sb.AppendFormat("{0}, {1:F4}, {2}, {3}, {4}, {5}",
                episode.ord, episode.reward, episode.win ? 1 : 0, episode.steps,
                episode.epsilon, episode.alpha);
            sb.AppendLine();
        }

        // Write the file
        string sceneName = EditorSceneManager.GetActiveScene().name;
        string time = DateTime.Now.ToString("hh-mm-ss");
        string date = DateTime.Now.ToString("dd-MM-yy");
        System.IO.File.WriteAllText(sceneName + "_" + date + "_" + time + ".txt", sb.ToString());
    }

    public void SaveEpisode(double reward, bool win, int steps, double epsilon, double alpha)
    {
        EpisodeData episode = new EpisodeData(ord, reward, win, steps, epsilon, alpha);
        trainingEps_.Add(episode);
        ord++;
    }

    public void SaveValidationEpisode(double reward, bool win, int steps)
    {
        EpisodeData episode = new EpisodeData(ord, reward, win, steps, 0, 0);
        validationEps_.Add(episode);
    }
}

