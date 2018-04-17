import sys
import re
import matplotlib.pyplot as plt
import numpy as np

file = open(sys.argv[1], "r")
data = file.read().split("\n")

episodes = [int(row.split(", ")[0]) for row in data[6:-1]]
rewards = [float(row.split(", ")[1]) for row in data[6:-1]]
epsilons = [float(row.split(", ")[4]) for row in data[6:-1]]

window = 50

cumsum_rewards = np.cumsum(np.insert(rewards, 0, 0))
ma_rewards = (cumsum_rewards[window:] - cumsum_rewards[:-window]) / window

fig = plt.figure()
ax1 = fig.add_subplot(111)
ax2 = ax1.twinx()

ax1.plot(range(len(ma_rewards)), ma_rewards, c="blue", label="Reward (50-episode moving average)")
ax2.plot(range(len(epsilons)), epsilons, c="orange", label="Epsilon value")
ax2.set_ylim([0, 1])
ax1.legend()
ax2.legend()

plt.show()
