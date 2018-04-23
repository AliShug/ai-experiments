import sys
import re
import matplotlib.pyplot as plt
import numpy as np


def movingAvg(data, window):
    cumsum = np.cumsum(np.insert(data, 0, 0))
    return (cumsum[window:] - cumsum[:-window]) / window

def plotFile(path, title, window, val_window):
    file = open(path, "r")
    data = file.read().split("\n")

    n_eps = int(data[4].split(": ")[1])
    print("Training", n_eps)
    n_val = int(data[5].split(": ")[1])
    print("Validation", n_val)
    train_range = slice(7, 7+n_eps)
    val_range = slice(8+n_eps, 8+n_eps+n_val)
    reward_ords = [int(row.split(", ")[0]) for row in data[train_range]]
    training_rewards = [float(row.split(", ")[1]) for row in data[train_range]]
    epsilons = [float(row.split(", ")[4]) for row in data[train_range]]
    val_ords = [int(row.split(", ")[0]) for row in data[val_range]]
    val_rewards = [float(row.split(", ")[1]) for row in data[val_range]]

    ma_rewards = movingAvg(training_rewards, window)
    reward_ords = reward_ords[window//2-1:-window//2]
    ma_val = movingAvg(val_rewards, val_window)
    val_ords = val_ords[val_window//2-1:-val_window//2]
    linewidth = 0.5

    ax1.plot(val_ords, ma_val, c="red",
             label="On-Policy Reward ({0}-episode moving average)".format(val_window),
             linewidth=linewidth)
    ax1.plot(reward_ords, ma_rewards, c="blue",
             label="Off-Policy Reward ({0}-episode moving average)".format(window),
             linewidth=linewidth)
    ax1.set_xlabel("Episode")
    ax1.set_ylabel("Reward at termination")
    ax1.grid(True)
    fig.suptitle(title)
    # ax2.plot(range(len(epsilons)), epsilons, c="orange", label="Epsilon value")
    # ax2.set_ylim([0, 1])
    ax1.legend()
    # ax2.legend()

# Plot file
fig = plt.figure()
ax1 = fig.add_subplot(111)
ax2 = ax1.twinx()
plotFile(sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]))
plt.show()
