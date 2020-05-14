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

    def take_action(self, obs, mask) -> np.array:
        possible_actions = []
        for i in range(len(mask)):
            value = mask[i]
            if not value:
                possible_actions.append(i)

        return random.choice(possible_actions)

    def preprocess_grid_obs(self, obs):
        resource_obs = obs[:5]  # The resources currently in hand
        grid_obs = obs[5:]
        processed_grid_obs = np.array(grid_obs)
        processed_grid_obs.reshape((6, 54))
        return processed_grid_obs
