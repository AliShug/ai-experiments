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
    public int steps;
    public double epsilon, alpha;

    public EpisodeData(double r, bool w, int s, double e, double a)
    {
        reward = r;
        win = w;
        steps = s;
        epsilon = e;
        alpha = a;
    }
}

public class StatTracker : MonoBehaviour
{
    private List<EpisodeData> data_ = new List<EpisodeData>();
    private Agent agent_;
    private DateTime startTime_;

    public void StartRun(Agent agent)
    {
        data_.Clear();
        agent_ = agent;
        startTime_ = DateTime.Now;
    }

    public void EndRun()
    {
        // Construct file string in memory
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("epsilon: {0}, {1}, {2}", agent_.startEpsilon, agent_.endEpsilon, agent_.epsilonDecay).AppendLine();
        sb.AppendFormat("alpha: {0}, {1}, {2}", agent_.startLearningRate, agent_.endLearningRate, agent_.learningDecay).AppendLine();
        sb.AppendFormat("gamma: {0}", agent_.gamma).AppendLine();
        sb.AppendFormat("training_time: {0}", DateTime.Now - startTime_).AppendLine();
        sb.AppendFormat("training_episodes: {0}", agent_.stopAfter).AppendLine();

        sb.AppendLine("Episode, reward, win, steps");
        int i = 0;
        foreach (EpisodeData episode in data_)
        {
            sb.AppendFormat("{0}, {1:F4}, {2}, {3}, {4}, {5}",
                i, episode.reward, episode.win ? 1 : 0, episode.steps,
                episode.epsilon, episode.alpha);
            sb.AppendLine();
            i++;
        }

        // Write the file
        string sceneName = EditorSceneManager.GetActiveScene().name;
        string time = DateTime.Now.ToString("hh-mm-ss");
        string date = DateTime.Now.ToString("dd-MM-yy");
        System.IO.File.WriteAllText(sceneName + "_" + date + "_" + time + ".txt", sb.ToString());
    }

    public void SaveEpisode(double reward, bool win, int steps, double epsilon, double alpha)
    {
        EpisodeData episode = new EpisodeData(reward, win, steps, epsilon, alpha);
        data_.Add(episode);
    }
}

