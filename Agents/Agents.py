import math

import numpy as np
from keras.models import Model
from keras.layers import Dense, Input
from keras.optimizers import Adam
from tensorflow.python.keras.models import load_model
import time


def get_random_action(mask):
    possible_actions = []
    for i in range(len(mask)):
        value = mask[i]
        if not value:
            possible_actions.append(i)
    return np.random.choice(possible_actions)


all_agent_types = ["R", "P", "G", "S"]


class Agent:
    def __init__(self, agent_id, action_shape, obs_shape):
        self.reward = 0
        self.agent_id = agent_id
        self.behavior_name = "Building?team=0"
        self.action_shape = action_shape
        self.obs_shape = obs_shape
        self.action_space = range(self.action_shape[0])
        self.prev_mask = np.zeros(shape=self.action_shape)

        self.states = []
        self.actions = []
        self.new_states = []
        self.rewards = []
        self.dones = []

    def choose_action(self, obs, mask) -> np.array:
        raise Exception("Do not call choose_action on Agent class directly!")

    def remember3(self, state, action, reward, new_state, done):
        self.states.append(state)
        self.actions.append(action)
        self.rewards.append(reward)
        self.new_states.append(new_state)
        self.dones.append(done)


    def remember(self, state, action, reward, done):

        if done:
            self.new_states.append(state)
            self.rewards.append(reward)

            value = (len(self.states) + len(self.new_states) + len(self.dones) + len(self.rewards) + len(
                self.actions)) / 5
            if value != len(self.states):
                i = 0  # Something is wrong

            return

        if len(self.states) > 0:
            self.new_states.append(state)
            self.rewards.append(reward)

        self.states.append(state)
        self.actions.append(action)
        self.dones.append(done)

    def remember2(self, state, action, reward, done):
        self.states.append(state)  # The current observation
        self.actions.append(action)  # The current action taken given that observation

        if done:
            self.new_states.append(state)  # From the previous step, that was NOT DONE yet

            if len(self.states) > len(self.new_states):
                self.new_states.append(state)

                # If our finishing reward is bigger than 0 (so we won), our previous reward must have been 1
                # Else, we lost and our previous reward was 0
                self.rewards.append(int(reward > 0))
                self.dones.append(False)  # The previous step we weren't done...

            self.rewards.append(reward)  # The winning or losing reward
            self.dones.append(True)  # This step we are!
            if len(self.states) != len(self.new_states):
                raise Exception("The amount of states and new_states should be the same after we are done!")

        # If we pass to end our turn; We have a deficit we need to fill!
        elif len(self.actions) > 1 and action == 0 and self.actions[-2] != 0 and len(self.states) > len(
                self.new_states) + 1:
            self.new_states.append(state)  # Add the current observation so it corresponds with the previous step
            self.new_states.append(state)  # Fill the deficit
            self.rewards.append(reward)  # Add the current reward that corresponds to the previous step
            self.rewards.append(0)  # If we pass, our current reward is 0
            self.dones.append(False)  # We were not done last step
            self.dones.append(False)  # And we are not done this step, otherwise we would be in the previous statement
            if len(self.states) != len(self.new_states):
                raise Exception("The amount of states and new_states should be the same after passing!")

        # If we pass and it's the only action we take; We don't have a deficit
        elif len(self.actions) > 1 and action == 0 and self.actions[-2] == 0 and len(self.states) - 1 == len(
                self.new_states):
            self.new_states.append(state)
            self.rewards.append(reward)
            self.dones.append(False)
            if len(self.states) != len(self.new_states):
                raise Exception("The amount of states and new_states should be the same after passing!")

        # Always make the new_states, done and rewards lists lack one behind
        elif len(self.states) > len(self.new_states) + 1 and action != 0:
            self.new_states.append(state)  # The current state is the new state of the previous one
            self.rewards.append(reward)  # The current reward is the result of the previous action
            self.dones.append(False)  # We are not done, otherwise we would have gotten the first if statement
            if len(self.states) <= len(self.new_states):
                raise Exception("The amount of states and new_states should be different after taking an action that' "
                                "not passing!")

        else:
            i = 0  # Something went very wrong

        # TODO Als de huidige speler niet de degene is die heeft gewonnen,
        #  dan gebeurd dit natuurlijk niet, dus bij learn moet de speler de laatste zelf aanvullen als dat kan

    def reset(self):
        self.reward = 0
        self.states.clear()
        self.actions.clear()
        self.new_states.clear()
        self.rewards.clear()
        self.dones.clear()

    def learn(self):
        return

    def getType(self):
        return "BaseAgent"


class RandomAgent(Agent):
    def __init__(self, agent_id, action_shape, obs_shape):
        super().__init__(agent_id, action_shape, obs_shape)

    def choose_action(self, obs, mask):
        possible_actions = []
        for i in range(len(mask)):
            value = mask[i]
            if not value:
                possible_actions.append(i)
        return np.random.choice(possible_actions)

    def getType(self):
        return "RandomAgent"


class ActorCritic(Agent):
    def __init__(self, agent_id, action_shape, obs_shape, alpha=0.001, beta=0.0001, gamma=0.99):
        super(ActorCritic, self).__init__(agent_id, action_shape, obs_shape)
        self.alpha = alpha
        self.beta = beta
        self.gamma = gamma
        self.actor, self.critic = self.build_actor_critic_network()

    def build_actor_critic_network(self):
        input = Input(shape=self.obs_shape)
        delta = Input([1])
        dense1 = Dense(125, activation='relu')(input)
        #dense2 = Dense(300, activation='relu')(dense1)
        #dense3 = Dense(300, activation='relu')(dense2)
        last = Dense(75, activation='relu')(dense1)
        probs = Dense(self.action_shape[0], activation='softmax')(last)
        values = Dense(1, activation='linear')(last)

        actor = Model(inputs=input, outputs=probs)
        actor.compile(optimizer=Adam(lr=self.alpha), loss='mean_squared_error')
        critic = Model(inputs=input, outputs=values)
        critic.compile(optimizer=Adam(lr=self.beta), loss='mean_squared_error')

        return actor, critic

    def discount_rewards(self, reward):
        # Compute the gamma-discounted rewards over an episode
        self.gamma = 0.99  # discount rate
        running_add = 0
        discounted_r = np.zeros_like(reward)
        for i in reversed(range(0, len(reward))):
            if reward[i] != 0:  # reset the sum, since this was a game boundary (pong specific!)
                running_add = 0
            running_add = running_add * self.gamma + reward[i]
            discounted_r[i] = running_add

        discounted_r -= np.mean(discounted_r)  # normalizing the result
        discounted_r /= np.std(discounted_r)  # divide by standard deviation
        return discounted_r

    def choose_action(self, obs, mask) -> np.array:
        state = obs[np.newaxis, :]
        probabilities = self.actor.predict(state)[0]
        # for i in range(len(probabilities)):
        #    if mask[i]:
        #        probabilities[i] = 0.0

        # s = sum(probabilities)
        # if s == 0.0 or s == 0:
        #    return 0

        # for i in range(len(probabilities)):
        #    if math.isnan(probabilities[i]):
        #        probabilities[i] = 0.0

        # norm_probs = [p / s for p in probabilities]
        action = np.random.choice(self.action_space, p=probabilities)
        return action

    def load(self):
        self.actor = load_model('actor', compile=False)
        self.critic = load_model('critic', compile=False)

    def save(self):
        self.actor.save('actor.h5')
        self.critic.save('critic.h5')

    def learn(self):
        # reshape memory to appropriate shape for training
        states = np.vstack(self.states)
        actions = np.vstack(self.actions)

        # Compute discounted rewards
        discounted_r = self.discount_rewards(self.rewards)

        # Get Critic network predictions
        values = self.critic.predict(states)[:, 0]
        # Compute advantages
        advantages = discounted_r - values
        # training Actor and Critic networks
        ah = self.actor.fit(states, actions, sample_weight=advantages, verbose=0)
        ch = self.critic.fit(states, discounted_r, verbose=0)
        return ah, ch


class MCTS(Agent):
    def __init__(self, agent_id, action_shape, obs_shape):
        super(MCTS, self).__init__(agent_id, action_shape, obs_shape)
        self.nodes = {}
        self.C = 2
        self.current_node = None
        self.rollout = False

    def choose_action(self, obs, mask) -> np.array:
        if self.current_node is None:
            self.current_node = MCTS.Node(obs=obs[:], parent=None)  # Create the root node
            self.nodes[obs] = self.current_node
            self.rollout = True
            self.expand(node=self.current_node, mask=mask)

        if len(self.current_node.childs) is 0:
            # If we don't have any children in this node, expand this node
            self.expand(node=self.current_node, mask=mask)

        if self.rollout:
            return get_random_action(mask=mask)

    def reset(self):
        super(MCTS, self).reset()
        self.current_node = None
        self.rollout = False

    def remember(self, state, action, reward, done):
        if done and self.rollout:
            self.current_node.n = self.reward

        if not done:
            self.current_node = self.current_node.best_child()

    def expand(self, node, mask):

        chosen = None

        for i in range(mask):
            if mask[i] or i == 0:
                continue

            building = (i - 1) % 3
            gp = math.floor((i - 1) / 3)
            obs = node.obs[:]
            if building is 0:
                obs[0] -= 1
                obs[1] -= 1
            elif building is 1:
                for res in range(4):
                    obs[res] -= 1
                index = 5 + 8 * gp
                obs[index] = 1
            else:
                obs[3] -= 2
                obs[4] -= 3
                index = 5 + 8 * gp
                obs[index] = 2

            new_node = MCTS.Node(obs=obs, parent=node)
            node.childs.append(new_node)
            self.nodes[obs] = new_node
            if chosen is None:
                chosen = new_node

        self.current_node = chosen

    class Node:
        def __init__(self, obs, parent):
            self.obs = obs
            self.parent = parent
            self.n = 0  # Number of visits
            self.t = 0  # total score
            self.childs = []

        def best_child(self):

            highest = -1
            bestchild = None
            for child in self.childs:

                if child.n is 0:
                    bestchild = child
                    break

                value = child.UCB1()
                if value > highest:
                    highest = value
                    bestchild = child

            return bestchild

        def UCB1(self):
            i = 0
