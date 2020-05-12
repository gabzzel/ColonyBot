import random
import numpy as np

class RandomAgent:
    def __init__(self, id, behavior_name, action_branches, obs_shape):
        self.reward = 0
        self.id = id
        self.behavior_name = behavior_name
        self.action_branches = action_branches
        self.obs_shape = obs_shape
        self.done = self.is_done()

    def is_done(self) -> int:
        return self.reward >= 12

    def take_action(self, branch_id, obs) -> np.array:
        # zeros = np.zeros(shape=(self.action_branches[branch_id], ))  # Create an array filled with zeros
        # random_index = np.random.randint(0, self.action_branches[branch_id])
        # zeros[random_index] = 1
        grid_obs = self.preprocess_grid_obs(obs=obs)
        return np.random.randint(0, self.action_branches[branch_id])

    def preprocess_grid_obs(self, obs):
        resource_obs = obs[:5]  # The resources currently in hand
        grid_obs = obs[5:]
        processed_grid_obs = np.array(grid_obs)
        processed_grid_obs.reshape((6, 54))
        return processed_grid_obs
