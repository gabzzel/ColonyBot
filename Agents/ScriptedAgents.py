import random
import numpy as np


def action_to_string(action):
    if action == 0:
        return "Pass"
    elif action % 3 == 1:
        return "Street"
    elif action % 3 == 2:
        return "Village"
    else:
        return "City"


def preprocess_grid_obs(obs):
    grid_obs = obs[5:]
    processed_grid_obs = np.array(grid_obs)
    return np.reshape(processed_grid_obs, (54, 8))


def get_random_action(mask):
    possible_actions = []
    for i in range(len(mask)):
        value = mask[i]
        if not value:
            possible_actions.append(i)
    return random.choice(possible_actions)


all_agent_types = ["R", "P", "G", "S"]


class Agent:
    def __init__(self, agent_id, behavior_name, action_shape, obs_shape):
        self.reward = 0
        self.agent_id = agent_id
        self.behavior_name = behavior_name
        self.action_shape = action_shape
        self.obs_shape = obs_shape

    def take_action(self, obs, mask) -> np.array:
        print("Do not call take_action on ScriptedAgent directly!")
        return np.zeros(shape=self.action_shape)

    def reset(self):
        self.reward = 0

    def getType(self):
        return "Default"


class RandomAgent(Agent):
    def __init__(self, agent_id, behavior_name, action_shape, obs_shape):
        super().__init__(agent_id, behavior_name, action_shape, obs_shape)

    def __take_action__(self, obs, mask):
        return get_random_action(mask=mask)

    def __getType__(self):
        return "RandomAgent"


class PassiveAgent(Agent):
    def __init__(self, agent_id, behavior_name, action_shape, obs_shape, pass_chance=0.99):
        self.pass_chance = pass_chance
        super().__init__(agent_id, behavior_name, action_shape, obs_shape)

    def __take_action__(self, obs, mask):
        if not mask[0] and random.random() < self.pass_chance:
            return 0

        possible_actions = []
        for i in range(len(mask)):
            value = mask[i]
            if not value:
                possible_actions.append(i)
        return random.choice(possible_actions)

    def __getType__(self):
        return "PassiveAgent"


class GreedyAgent(Agent):

    def __take_action__(self, obs, mask):
        possible_actions = []
        for i in range(len(mask)):
            value = mask[i]
            if not value:
                possible_actions.append(i)

        if obs[3] > 1 and obs[4] > 2:
            for r in range(54):
                action = 3 + r * 3
                if not mask[action]:
                    return action

        if obs[0] > 0 and obs[1] > 0 and obs[2] > 0 and obs[3] > 0:
            for r in range(54):
                action = 2 + r * 3
                if not mask[action]:
                    return action

        if obs[0] > 0 and obs[1] > 0:
            for r in range(54):
                action = 1 + r * 3
                if not mask[action]:
                    return action

        return random.choice(possible_actions)

    def __getType__(self):
        return "GreedyAgent"


class StreetBuilderAgent(Agent):
    def __init__(self, agent_id, behavior_name, action_shape, obs_shape):
        self.villages_build = 0
        super().__init__(agent_id, behavior_name, action_shape, obs_shape)

    def __take_action__(self, obs, mask):

        # If we cannot pass, we are in the initial phase.
        # Choose the best brick and wood placements
        if mask[0]:
            pos = self.get_best_village_placement(obs=obs, mask=mask)
            if pos is -1:
                return get_random_action(mask=mask)
            return 2 + 3 * pos

        # If we are not in the initial phase, always try to build a street
        elif obs[0] > 0 and obs[1] > 0:
            for r in range(54):
                action = 1 + r * 3
                if not mask[action]:
                    return action

        # Otherwise, pass with 50% chance
        if random.random() > 0.5:
            return get_random_action(mask=mask)
        return 0

    def get_best_village_placement(self, obs, mask):
        grid_obs = preprocess_grid_obs(obs=obs)
        best_index = -1
        best_value = -1
        for i in range(54):
            value = grid_obs[i][3] + grid_obs[i][4]
            if value > best_value and not mask[2 + 3 * i]:
                best_value = value
                best_index = i
        return best_index

    def __getType__(self):
        return "StreetBuilder"
