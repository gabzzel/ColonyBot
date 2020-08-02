import numpy as np
from keras.models import Model
from keras.layers import Dense, Input
from keras.optimizers import Adam
import keras.backend as K


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
            self.new_states.append(state)  # Fill the deficit
            self.rewards.append(int(reward > 0))  # If our finishing reward is bigger than 0 (so we won)
            # our previous reward must have been 1
            # Else, we lost and our previous reward was 0

            self.rewards.append(reward)  # The winning or losing reward
            self.dones.append(False)  # The previous step we weren't done...
            self.dones.append(True)  # This step we are!

        # If we pass and our previous action wasn't a pass,
        # our observation will not change and we have no reward and we are not done
        elif action == 0 and self.actions[-2] != 0:
            self.new_states.append(state)  # Add the current observation so it corresponds with the previous step
            self.new_states.append(state)  # Fill the deficit
            self.rewards.append(reward)  # Add the current reward that corresponds to the previous step
            self.rewards.append(0)  # If we pass, our current reward is 0
            self.dones.append(False)  # We were not done last step
            self.dones.append(False)  # And we are not done this step, otherwise we would be in the previous statement
        elif action == 0 and self.actions[-2] == 0:
            self.new_states.append(state)
            self.rewards.append(reward)
            self.dones.append(False)

        else:
            # Always make the new_states, done and rewards lists lack one behind
            if len(self.states) > len(self.new_states) + 1:
                self.new_states.append(state)  # The current state is the new state of the previous one
                self.rewards.append(reward)  # The current reward is the result of the previous action
                self.dones.append(False)  # We are not done, otherwise we would have gotten the first if statement

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
        return "Base Agent class"


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
        self.actor, self.critic, self.policy = self.build_actor_critic_network()

    def build_actor_critic_network(self):
        input = Input(shape=self.obs_shape)
        delta = Input([1])
        dense1 = Dense(437, activation='relu')(input)
        dense2 = Dense(300, activation='relu')(dense1)
        dense3 = Dense(300, activation='relu')(dense2)
        last = Dense(163, activation='relu')(dense3)
        probs = Dense(self.action_shape[0], activation='softmax')(last)
        values = Dense(1, activation='linear')(last)

        def custom_loss(y_true, y_pred):
            out = K.clip(y_pred, 1e-08, 1 - 1e-8)
            log_lik = y_true * K.log(out)
            loss = K.sum(-log_lik * delta)
            print(loss)
            return loss

        actor = Model(inputs=[input, delta], outputs=[probs])
        actor.compile(optimizer=Adam(lr=self.alpha), loss=custom_loss)
        critic = Model(inputs=[input], outputs=[values])
        critic.compile(optimizer=Adam(lr=self.beta), loss='mean_squared_error')

        policy = Model(inputs=[input], outputs=[probs])
        return actor, critic, policy

    def choose_action(self, obs, mask) -> np.array:
        state = obs[np.newaxis, :]
        probabilities = self.policy.predict(state)[0]
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

    def learn(self):
        actor_inputs = []
        actions_set = []
        critic_input = []
        targets = []

        for i in range(len(self.states)):
            state = np.reshape(a=self.states[i], newshape=(1, 437))
            new_state = np.reshape(a=self.new_states[i], newshape=(1, 437))
            critic_value_ = self.critic.predict(new_state)
            critic_value = self.critic.predict(state)
            target = self.rewards[i] + self.gamma * critic_value_ * (1 - int(self.dones[i]))
            delta = target - critic_value
            actor_inputs.append([state, delta])  # Actor Input

            actions = np.zeros([1, self.action_space[0]])
            actions[np.arange(1), self.actions[i]] = 1.0
            actions_set.append(actions)  # Actor output

            critic_input.append(state)
            targets.append(target)

        ah = self.actor.fit(actor_inputs, actions_set, verbose=0)
        ch = self.critic.fit(critic_input, targets, verbose=0)
        return ah, ch
