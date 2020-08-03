import numpy as np
from keras.models import Model
from keras.layers import Dense, Input
from keras.optimizers import Adam
import keras.backend as K
from tensorflow.python.keras.models import load_model


def get_random_action(mask):
    possible_actions = []
    for i in range(len(mask)):
        value = mask[i]
        if not value:
            possible_actions.append(i)
    return np.random.choice(possible_actions)


all_agent_types = ["R", "P", "G", "S"]


class Agent:
    def __init__(self, agent_id):
        self.reward = 0
        self.agent_id = agent_id
        self.behavior_name = "Building?team=0"
        self.action_shape = (163,)
        self.obs_shape = (437,)
        self.action_space = range(self.action_shape[0])
        self.prev_mask = np.zeros(shape=self.action_shape)

        self.states = []
        self.actions = []
        self.new_states = []
        self.rewards = []
        self.dones = []

    def choose_action(self, obs, mask) -> np.array:
        raise Exception("Do not call choose_action on Agent class directly!")

    def remember(self, state, action, reward, done):
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
        elif len(self.actions) > 1 and action == 0 and self.actions[-2] != 0:
            self.new_states.append(state)  # Add the current observation so it corresponds with the previous step
            self.new_states.append(state)  # Fill the deficit
            self.rewards.append(reward)  # Add the current reward that corresponds to the previous step
            self.rewards.append(0)  # If we pass, our current reward is 0
            self.dones.append(False)  # We were not done last step
            self.dones.append(False)  # And we are not done this step, otherwise we would be in the previous statement
            if len(self.states) != len(self.new_states):
                raise Exception("The amount of states and new_states should be the same after passing!")

        # If we pass and it's the only action we take; We don't have a deficit
        elif len(self.actions) > 1 and action == 0 and self.actions[-2] == 0:
            self.new_states.append(state)
            self.rewards.append(reward)
            self.dones.append(False)
            if len(self.states) != len(self.new_states):
                raise Exception("The amount of states and new_states should be the same after passing!")

        # Always make the new_states, done and rewards lists lack one behind
        elif len(self.states) > len(self.new_states) + 1:
            self.new_states.append(state)  # The current state is the new state of the previous one
            self.rewards.append(reward)  # The current reward is the result of the previous action
            self.dones.append(False)  # We are not done, otherwise we would have gotten the first if statement
            if len(self.states) <= len(self.new_states):
                raise Exception("The amount of states and new_states should be different after taking an action that's not passing!")

        i = 0

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
    def __init__(self, agent_id):
        super().__init__(agent_id)

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
    def __init__(self, agent_id, alpha=0.001, beta=0.0001, gamma=0.99):
        super(ActorCritic, self).__init__(agent_id)
        self.alpha = alpha
        self.beta = beta
        self.gamma = gamma
        self.actor, self.critic = self.build_actor_critic_network()

    def build_actor_critic_network(self):
        input = Input(shape=self.obs_shape)
        delta = Input([1])
        dense1 = Dense(437, activation='relu')(input)
        dense2 = Dense(300, activation='relu')(dense1)
        dense3 = Dense(300, activation='relu')(dense2)
        last = Dense(163, activation='relu')(dense3)
        probs = Dense(self.action_shape[0], activation='softmax')(last)
        values = Dense(1, activation='linear')(last)

        actor = Model(inputs=[input, delta], outputs=[probs])
        actor.compile(optimizer=Adam(lr=self.alpha), loss='mean_squared_error')
        critic = Model(inputs=[input], outputs=[values])
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
