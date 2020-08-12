import numpy as np
from keras.models import Model
from keras.layers import Dense, Input, LSTM, Dropout
from keras.optimizers import Adam
import keras.backend as K
import tensorflow as tf
import math

from ScriptedAgents import Agent


# determines how to assign values to each state, i.e. takes the state
# and action (two-input model) and determines the corresponding value
class ActorCritic(Agent):
    def __init__(self, agent_id, behavior_name, action_shape, obs_shape, alpha, beta, gamma=0.99):
        super().__init__(agent_id, behavior_name, action_shape, obs_shape)
        self.tensorboard_callback = tf.keras.callbacks.TensorBoard(log_dir="./logs")
        self.gamma = gamma
        self.alpha = alpha
        self.beta = beta
        #self.fc1_dims = 1024
        #self.fc2_dims = 512
        self.n_actions = action_shape[0]
        self.memory = []

        self.actor, self.critic, self.policy = self.build_actor_critic_network()
        self.action_space = [i for i in range(action_shape[0])]

    def reset(self):
        super(ActorCritic, self).reset()
        self.memory.clear()


    def build_actor_critic_network(self):
        input = Input(shape=self.obs_shape)
        delta = Input([1])
        dense1 = Dense(125, activation='relu')(input)
        #dense2 = Dense(150, activation='relu')(dense1)
        #dense3 = Dense(300, activation='relu')(dense2)
        last = Dense(75, activation='relu')(dense1)
        probs = Dense(self.action_shape[0], activation='softmax')(last)
        values = Dense(1, activation='linear')(last)

        def custom_loss(y_true, y_pred):
            out = K.clip(y_pred, 1e-08, 1 - 1e-8)
            log_lik = y_true * K.log(out)
            return K.sum(-log_lik * delta)

        actor = Model(inputs=[input, delta], outputs=[probs])
        actor.compile(optimizer=Adam(lr=self.alpha), loss='mean_squared_error')
        critic = Model(inputs=[input], outputs=[values])
        critic.compile(optimizer=Adam(lr=self.beta), loss='mean_squared_error')

        policy = Model(inputs=[input], outputs=[probs])
        return actor, critic, policy

    def choose_action(self, obs, mask):
        state = obs[np.newaxis, :]
        probabilities = self.policy.predict(state)[0]

        #for i in range(len(probabilities)):
        #    if mask[i]:
        #        probabilities[i] = 0.0

        #s = sum(probabilities)
        #if s == 0.0 or s == 0:
        #    return 0

        #for i in range(len(probabilities)):
        #    if math.isnan(probabilities[i]):
        #        probabilities[i] = 0.0

        #norm_probs = [p / s for p in probabilities]
        action = np.random.choice(self.action_space, p=probabilities)
        return action

    def remember(self, state, action, reward, state_, done):
        self.memory.append((state, action, reward, state_, done))

    def learn_stepbased(self, state, action, reward, state_, done):
        state = state[np.newaxis, :]
        state_ = state_[np.newaxis, :]

        critic_value_ = self.critic.predict(state_)
        critic_value = self.critic.predict(state)

        target = reward + self.gamma * critic_value_ * (1 - int(done))
        delta = target - critic_value

        actions = np.zeros([1, self.n_actions])
        actions[np.arange(1), action] = 1.0

        actor_history = self.actor.fit([state, delta], actions, verbose=0)
        critic_history = self.critic.fit(state, target, verbose=0)
        return [actor_history.history, critic_history.history]

    def learn(self):

        actor_inputs = []
        actions_set = []
        critic_input = []
        targets = []

        for state, action, reward, state_, done in self.memory:
            #state_ = np.reshape(a=state_[np.newaxis, :], newshape=(1, 123))
            #state = np.reshape(a=state, newshape=(1, 123))
            state = state[np.newaxis, :]
            state_ = state_[np.newaxis, :]
            critic_value_ = self.critic.predict(state_)
            critic_value = self.critic.predict(state)
            target = reward + self.gamma * critic_value_ * (1 - int(done))
            delta = target - critic_value
            actor_inputs.append([state, delta])  # Actor Input

            actions = np.zeros([1, self.n_actions])
            actions[np.arange(1), action] = 1.0
            actions_set.append(actions)  # Actor output

            critic_input.append(state)
            targets.append(target)

        ah = self.actor.fit(actor_inputs, actions_set, verbose=1)
        ch = self.critic.fit(critic_input, targets, verbose=1)
        return ah, ch

    def getType(self):
        return "ActorCritic"
