import math
from typing import Union, List, Tuple

import numpy as np
from keras.layers import Dense, Input, Concatenate
from keras.models import Model
from keras.optimizers import RMSprop
from tensorflow.python.keras.models import load_model


def normalize(x: list) -> list:
    if x is None:
        raise TypeError("The list to normalize cannot be null!")

    for i in x:
        if math.isnan(i):
            raise Exception("NaN values in input are forbidden!")

    total = sum(x)

    if total == 0.0:
        return x

    return [i / total for i in x]


def use_mask(x: list, mask: List[bool]):

    if len(x) != len(mask):
        raise Exception("The length of the input list and mask are not the same!")

    result = []
    for i in range(len(x)):
        if mask[i] is True or math.isnan(x[i]):
            result.append(0.0)
        else:
            result.append(x[i])
    return result


def simple_type_check(name, variable, expected_type):
    if isinstance(expected_type, list):
        for t in expected_type:
            if isinstance(variable, t):
                return
        raise Exception("Expected " + name + " to have one of the following types:" + str(expected_type)
                        + " but got " + str(type(variable)))

    if not isinstance(variable, expected_type):
        raise Exception(name + " does not have the expected type " + str(expected_type)
                        + " but got " + str(type(variable)))


def get_random_action(mask):
    possible_actions = []
    for i in range(len(mask)):
        value = mask[i]
        if not value:
            possible_actions.append(i)
    return np.random.choice(possible_actions)


class Agent:
    def __init__(self, agent_id, action_shape, obs_shape, debug_mode):

        if not isinstance(debug_mode, bool):
            raise Exception("Expected debug_mode parameter to have type bool, got " + str(type(debug_mode)))

        self.debug_mode = debug_mode
        self.agent_id = agent_id
        self.action_shape = action_shape
        self.obs_shape = obs_shape
        self.action_space = list(range(self.action_shape[0]))


        # Episodic Stats
        self.states = []  # The states are represented by vectors with shape obs_shape
        self.actions = []  # The one-hot action vectors with shape action_shape
        self.actions_single = []  # The actions taken as single integers
        self.new_states = []  # The next states, represented by vectors with shape obs_shape
        self.rewards = []  # The rewards we have gotten every step this episode
        self.dones = []  # A list that holds if we were done for every step

        # Run stats
        self.r_wins = []  # The wins over multiple runs
        self.r_rewards = []  # The rewards over multiple runs

    def choose_action(self, obs, mask) -> Union[np.int32, int]:
        raise Exception("Do not call choose_action on Agent class directly!")

    def remember(self, state, action, reward, new_state, done):

        if self.debug_mode:
            self.check_states(state=state, new_state=new_state)
            self.check_action(action=action)
            simple_type_check("reward", reward, [float, int])
            simple_type_check("done", done, bool)

        self.states.append(state)

        # If our action is just an int, convert it to a one-hot representation
        if isinstance(action, int) or isinstance(action, np.int32) or isinstance(action, np.int64):
            a = np.zeros(shape=self.action_shape)
            a[action] = 1.0
            self.actions.append(a)
            self.actions_single.append(action)
        # If our action is a list, get the action index as the action taken
        elif isinstance(action, list):
            self.actions.append(action)
            self.actions_single.append(action.index(1.0))

        self.rewards.append(reward)
        self.new_states.append(new_state)
        self.dones.append(done)

    def reset(self):
        self.states.clear()
        self.actions.clear()
        self.new_states.clear()
        self.rewards.clear()
        self.dones.clear()
        self.actions_single.clear()

    def check_states(self, state, new_state):
        if isinstance(state, list) and isinstance(new_state, list):
            if len(state) != len(new_state):
                raise Exception("The length of the state (" + str(len(state))
                                + ") does not match the length of the new_state (" + str(len(new_state)) + ")!")
            elif len(state) != self.obs_shape[0] or len(new_state) != self.obs_shape[0]:
                raise Exception("The length of the state " + str(len(state)) + " or of the new_state "
                                + str(len(new_state)) + " does not match the expected obs_shape "
                                + str(self.obs_shape[0]))
        elif isinstance(state, np.ndarray) and isinstance(new_state, np.ndarray):
            if state.shape != new_state.shape:
                raise Exception("The shapes of the state " + str(state.shape) + " and the new state "
                                + str(new_state.shape) + "differ from each other!")
            elif state.shape != self.obs_shape or new_state.shape != self.obs_shape:
                raise Exception("The shape of the state " + str(state.shape) + " and/or of the new_state "
                                + str(new_state.shape) + " are not the same as the given obs_shape!")
        else:
            raise Exception("State and/or new_state have unexpected types. Expected list or np.ndarray, got "
                            + str(type(state)) + " and " + str(type(new_state)) + " instead.")

    def check_action(self, action):
        if isinstance(action, int) or isinstance(action, np.int32):
            if action < 0 or action > self.action_shape[0]:
                raise Exception("Expected action to be between 0 (incl.) and " + str(self.action_shape[0])
                                + " (excl.) but got " + str(action) + " instead.")
        elif isinstance(action, list):
            if abs(sum(action) - 1) > 0.00001:
                raise Exception("If the action is a list, please provide a one-hot representation! " + str(action))
            elif len(action) != self.action_shape[0]:
                raise Exception("Length of action vector invalid. Expected " + str(self.action_shape[0])
                                + ", got " + str(len(action)))
        else:
            raise Exception("Action has invalid type. Expected int or list, got " + str(type(action)))

    def get_stats_header(self) -> List[str]:
        return ["Wins", "Rewards"]

    def get_stats(self) -> list:
        return [self.r_wins, self.r_rewards]

    def finish(self, folder: str, run_id: int):
        return

    def get_config(self) -> dict:
        config = dict()
        config["Agent: "] = str(self.agent_id)
        config["Type: "] = str(type(self))
        return config


class RandomAgent(Agent):
    def __init__(self, agent_id, action_shape, obs_shape, debug_mode):
        super().__init__(agent_id, action_shape, obs_shape, debug_mode)

    def choose_action(self, obs, mask) -> Tuple[Union[np.int32, int], Union[list, None]]:
        possible_actions = []
        for i in range(len(mask)):
            value = mask[i]
            if not value:
                possible_actions.append(i)
        return np.random.choice(possible_actions), None

    def get_stats(self) -> list:
        return super(RandomAgent, self).get_stats()

    def get_stats_header(self) -> List[str]:
        return super(RandomAgent, self).get_stats_header()


class ActorCritic(Agent):
    def __init__(self, agent_id, action_shape, obs_shape, debug_mode, actor_learning_rate=0.001,
                 actor_loss_type='mean_squared_error', critic_loss_type='mean_squared_error',
                 critic_learning_rate=0.001, discount_factor=0.99, batch_size=100):

        super(ActorCritic, self).__init__(agent_id, action_shape, obs_shape, debug_mode)
        # General learning parameters
        self.discount_factor = discount_factor
        self.batch_size = batch_size
        # Actor parameters
        self.actor_learning_rate = actor_learning_rate
        self.actor_loss_type = actor_loss_type
        # Critic parameters
        self.critic_learning_rate = critic_learning_rate
        self.critic_loss_type = critic_loss_type

        self.actor, self.critic = self.build_actor_critic_network()

        # Run stats
        self.actor_losses = []
        self.critic_losses = []

    def build_actor_critic_network(self):
        inp = Input(shape=self.obs_shape)
        dense1 = Dense(300, activation='relu')(inp)
        dense2 = Dense(200, activation='relu')(dense1)
        dense3 = Dense(150, activation='relu')(dense2)
        last = Dense(75, activation='relu')(dense3)
        probs = Dense(self.action_shape[0], activation='softmax')(last)
        values = Dense(1, activation='linear')(last)

        actor = Model(inputs=inp, outputs=probs)
        actor.compile(optimizer=RMSprop(lr=self.actor_learning_rate), loss=self.actor_loss_type)
        critic = Model(inputs=inp, outputs=values)
        critic.compile(optimizer=RMSprop(lr=self.critic_learning_rate), loss=self.critic_loss_type)

        return actor, critic

    def discount_rewards(self, reward):
        # Compute the gamma-discounted rewards over an episode
        running_add = 0
        discounted_r = np.zeros_like(reward, dtype=np.float)
        for i in reversed(range(0, len(reward))):
            # if reward[i] != 0:  # reset the sum, since this was a game boundary (pong specific!)
            #   running_add = 0
            running_add = running_add * self.discount_factor + reward[i]
            discounted_r[i] = running_add

        # discounted_r -= np.mean(discounted_r)  # normalizing the result
        mean = np.mean(discounted_r)
        mean = -1 * mean if not np.isnan(mean).any() else 0
        discounted_r = np.add(discounted_r, mean)
        std = np.std(discounted_r)
        if np.isnan(std).any() or std == 0:
            std = 1
        discounted_r /= std  # divide by standard deviation
        return discounted_r

    def choose_action(self, obs, mask) -> Tuple[int, Union[None, list]]:
        state = obs[np.newaxis, :]
        original_probabilities = self.actor.predict(state)[0]

        if len(original_probabilities) != len(mask):
            raise Exception("The length of the mask and the action-probabilities do no match!")

        masked_probabilities = use_mask(x=original_probabilities, mask=mask)
        normalized_probabilities = normalize(x=masked_probabilities)

        sum_norm_prob = sum(normalized_probabilities)
        if abs(sum_norm_prob - 1) > 0.000001 and sum_norm_prob != 0.0:
            raise Exception("The normalized probabilities do not sum to 1 and are not 0.0! Sum:" + str(sum_norm_prob))
        elif sum_norm_prob == 0.0:
            action = get_random_action(mask=mask)
        else:
            action = np.random.choice(a=self.action_space, p=normalized_probabilities)

        return action, original_probabilities[:]

    def load(self, run_id, folder=None):
        if folder is None:
            self.actor = load_model('actor' + str(run_id) + ".h5", compile=False)
            self.critic = load_model('critic' + str(run_id) + ".h5", compile=False)
        else:
            self.actor = load_model(filepath=folder + "\\actor" + run_id + ".h5", compile=False)
            self.critic = load_model(filepath=folder + "\\actor" + run_id + ".h5", compile=False)

    def save(self, run_id, folder=None):
        if folder is None:
            self.actor.save('actor' + str(run_id) + str(self.agent_id) + '.h5')
            self.critic.save('critic' + str(run_id) + str(self.agent_id) + '.h5')
        else:
            self.actor.save(filepath=folder + "\\" + 'actor' + str(run_id) + str(self.agent_id) + '.h5')
            self.critic.save(filepath=folder + "\\" + 'critic' + str(run_id) + str(self.agent_id) + '.h5')

    def finish(self, folder: str, run_id: int):
        self.learn()
        self.save(run_id=run_id, folder=folder)

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
        ah = self.actor.fit(states, actions, sample_weight=advantages, verbose=0, batch_size=self.batch_size)
        ch = self.critic.fit(states, discounted_r, verbose=0, batch_size=self.batch_size)

        self.actor_losses.append(ah.history['loss'][0])
        self.critic_losses.append(ch.history['loss'][0])

    def get_stats_header(self) -> list:
        base = super(ActorCritic, self).get_stats_header()
        base.extend(["ActorLoss", "CriticLoss"])
        return base

    def get_stats(self) -> list:
        base = super(ActorCritic, self).get_stats()
        base.extend([self.actor_losses, self.critic_losses])
        return base

    def get_config(self) -> dict:
        config = super(ActorCritic, self).get_config()
        config["Discount factor: "] = str(self.discount_factor)
        config["Batch Size: "] = str(self.batch_size)

        config["\n Actor Parameters: "] = ""
        config["Actor Learning Rate: "] = str(self.actor_learning_rate)
        config["Actor Optimizer: "] = str(self.actor.optimizer)
        config["Actor Loss Metric: "] = str(self.actor_loss_type)

        config["\n Critic Parameters"] = ""
        config["Critic Learning Rate: "] = str(self.critic_learning_rate)
        config["Critic Optimizer: "] = str(self.critic.optimizer)
        config["Critic Loss Metric: "] = str(self.critic_loss_type)

        return config


class DoubleActorCritic(Agent):
    def __init__(self, agent_id, action_shape, obs_shape, debug_mode, master_actor_lr=0.001, slave_actor_lr=0.001,
                 actor_loss_type='categorical_crossentropy', critic_loss_type='mean_squared_error',
                 critic_learning_rate=0.001, discount_factor=0.99, batch_size=100):
        super(DoubleActorCritic, self).__init__(agent_id, action_shape, obs_shape, debug_mode)
        # General learning parameters
        self.discount_factor = discount_factor
        self.batch_size = batch_size
        # Actor parameters
        self.master_actor_learning_rate = master_actor_lr
        self.slave_actor_learning_rate = slave_actor_lr
        self.actor_loss_type = actor_loss_type
        # Critic parameters
        self.critic_learning_rate = critic_learning_rate
        self.critic_loss_type = critic_loss_type

        self.master_actor, self.slave_actor, self.critic = self.build_actor_critic_network()

        # Run stats
        self.master_actor_losses = []
        self.slave_actor_losses = []
        self.critic_losses = []

        self.master_actor_actions = []
        self.slave_actor_actions = []

    def build_actor_critic_network(self):
        resource_input = Input(shape=(3,))
        gp_input = Input(shape=(144,))
        complete_input = Concatenate(axis=1)([resource_input, gp_input])

        dense1 = Dense(300, activation='relu')(complete_input)
        dense2 = Dense(200, activation='relu')(dense1)
        dense3 = Dense(150, activation='relu')(dense2)
        last = Dense(75, activation='relu')(dense3)

        master_probs = Dense(3, activation='softmax')(last)
        slave_probs = Dense(24, activation='softmax')(last)
        values = Dense(1, activation='linear')(last)

        master_actor = Model(inputs=resource_input, outputs=master_probs)
        master_actor.compile(optimizer=RMSprop(lr=self.master_actor_learning_rate), loss=self.actor_loss_type)

        slave_actor = Model(inputs=gp_input, outputs=slave_probs)
        slave_actor.compile(optimizer=RMSprop(lr=self.slave_actor_learning_rate), loss=self.actor_loss_type)

        critic = Model(inputs=complete_input, outputs=values)
        critic.compile(optimizer=RMSprop(lr=self.critic_learning_rate), loss=self.critic_loss_type)

        return master_actor, slave_actor, critic

    def discount_rewards(self, reward):
        # Compute the gamma-discounted rewards over an episode
        running_add = 0
        discounted_r = np.zeros_like(reward, dtype=np.float)
        for i in reversed(range(0, len(reward))):
            # if reward[i] != 0:  # reset the sum, since this was a game boundary (pong specific!)
            #   running_add = 0
            running_add = running_add * self.discount_factor + reward[i]
            discounted_r[i] = running_add

        # discounted_r -= np.mean(discounted_r)  # normalizing the result
        mean = np.mean(discounted_r)
        mean = -1 * mean if not np.isnan(mean).any() else 0
        discounted_r = np.add(discounted_r, mean)
        std = np.std(discounted_r)
        if np.isnan(std).any() or std == 0:
            std = 1
        discounted_r /= std  # divide by standard deviation
        return discounted_r

    def choose_action(self, obs, mask) -> Tuple[int, Union[None, list]]:
        # If we can only pass, return pass
        if sum(mask) == len(mask) - 1:
            if not mask[0]:
                return 0, None
            else:
                return get_random_action(mask=mask), None

        state = obs[np.newaxis, :]
        resources = state[:3]
        gp_input = state[3:]
        master_probabilities = self.master_actor.predict(resources)[0]
        slave_probabilities = self.slave_actor.predict(gp_input)[0]

        master_action = np.random.choice([0, 1, 2], p=master_probabilities)
        slave_action = np.random.choice(list(range(24)), p=slave_probabilities)

        if master_action == 0:
            action = 0
        else:
            action = round(slave_action * 2 + master_action)

        if mask[action]:
            return get_random_action(mask=mask), list(master_probabilities).extend(list(slave_probabilities))
        else:
            return action, list(master_probabilities).extend(list(slave_probabilities))

    def load(self, run_id, folder=None):
        if folder is None:
            self.master_actor = load_model('master_actor' + str(run_id) + ".h5", compile=False)
            self.slave_actor = load_model('slave_actor' + str(run_id) + ".h5", compile=False)
            self.critic = load_model('critic' + str(run_id) + ".h5", compile=False)
        else:
            self.master_actor = load_model(filepath=folder + "\\master_actor" + run_id + ".h5", compile=False)
            self.slave_actor = load_model(filepath=folder + "\\slave_actor" + run_id + ".h5", compile=False)
            self.critic = load_model(filepath=folder + "\\actor" + run_id + ".h5", compile=False)

    def save(self, run_id, folder=None):
        if folder is None:
            self.master_actor.save('master_actor' + str(run_id) + str(self.agent_id) + '.h5')
            self.slave_actor.save('slave_actor' + str(run_id) + str(self.agent_id) + '.h5')
            self.critic.save('critic' + str(run_id) + str(self.agent_id) + '.h5')
        else:
            self.master_actor.save(filepath=folder + "\\" + 'master_actor' + str(run_id) + str(self.agent_id) + '.h5')
            self.slave_actor.save(filepath=folder + "\\" + 'slave_actor' + str(run_id) + str(self.agent_id) + '.h5')
            self.critic.save(filepath=folder + "\\" + 'critic' + str(run_id) + str(self.agent_id) + '.h5')

    def finish(self, folder: str, run_id: int):
        self.learn()
        self.save(run_id=run_id, folder=folder)

    def remember(self, state, action, reward, new_state, done):
        super(DoubleActorCritic, self).remember(state, action, reward, new_state, done)
        if action == 0:
            master_action = [1.0, 0.0, 0.0]
            slave_action = [1.0 / 24.0] * 24
        else:
            index = int(action % 2)
            master_action = [0.0, 0.0, 0.0]
            master_action[index] = 1.0
            slave_action = [0.0] * 24
            index = int(math.floor((action - 1) / 2))
            slave_action[index] = 1.0
        self.master_actor_actions.append(master_action)
        self.slave_actor_actions.append(slave_action)

    def learn(self):
        # reshape memory to appropriate shape for training

        master_actor_input = []
        slave_actor_input = []
        for i in range(len(self.states)):
            state = self.states[i]
            master_actor_input.append(state[:3])
            slave_actor_input.append(state[3:])

        states = np.vstack(self.states)
        master_actor_input = np.vstack(master_actor_input)
        slave_actor_input = np.vstack(slave_actor_input)

        # actions = np.vstack(self.actions)
        master_actor_labels = np.vstack(self.master_actor_actions)
        slave_actor_labels = np.vstack(self.slave_actor_actions)

        # Compute discounted rewards
        discounted_r = self.discount_rewards(self.rewards)

        # Get Critic network predictions
        values = self.critic.predict(states)[:, 0]
        # Compute advantages
        advantages = discounted_r - values
        # training Actor and Critic networks
        mah = self.master_actor.fit(master_actor_input, master_actor_labels, sample_weight=advantages, verbose=0,
                                    batch_size=self.batch_size)
        sah = self.slave_actor.fit(slave_actor_input, slave_actor_labels, sample_weight=advantages, verbose=0,
                                   batch_size=self.batch_size)
        ch = self.critic.fit(states, discounted_r, verbose=0, batch_size=self.batch_size)

        self.master_actor_losses.append(mah.history['loss'][0])
        self.slave_actor_losses.append(sah.history['loss'][0])
        self.critic_losses.append(ch.history['loss'][0])

    def get_stats_header(self) -> list:
        base = super(DoubleActorCritic, self).get_stats_header()
        base.extend(["MasterActorLoss", "SlaveActorLoss", "CriticLoss"])
        return base

    def get_stats(self) -> list:
        base = super(DoubleActorCritic, self).get_stats()
        base.extend([self.master_actor_losses, self.slave_actor_losses, self.critic_losses])
        return base

    def get_config(self) -> dict:
        config = super(DoubleActorCritic, self).get_config()
        config["Discount factor: "] = str(self.discount_factor)
        config["Batch Size: "] = str(self.batch_size)

        config["\n Actor Parameters: "] = ""
        config["Actor Learning Rates: "] = str(self.master_actor_learning_rate) + " & " + str(
            self.slave_actor_learning_rate)
        config["Actor Optimizer: "] = str(self.master_actor.optimizer) + " & " + str(self.slave_actor.optimizer)
        config["Actor Loss Metric: "] = str(self.actor_loss_type)

        config["\n Critic Parameters"] = ""
        config["Critic Learning Rate: "] = str(self.critic_learning_rate)
        config["Critic Optimizer: "] = str(self.critic.optimizer)
        config["Critic Loss Metric: "] = str(self.critic_loss_type)

        return config
