import random
import time

import numpy as np
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
import tensorflow as tf

import ScriptedAgents
import colonybot

env_name = "C:\\Users\\Gabi\\Documents\\GitHub\\ColonyBot\\Builds\\ColonyBot.exe"
train_mode = True  # Whether to run the environment in training or inference mode
points_to_win = 8

engine_configuration_channel = EngineConfigurationChannel()
side_channel = EnvironmentParametersChannel()
side_channel.set_float_parameter("step_time", 0.001)
side_channel.set_float_parameter("show_ui", 0)
side_channel.set_float_parameter("standard_board", 1)
side_channel.set_float_parameter("points_to_win", points_to_win)
engine_configuration_channel.set_configuration_parameters(time_scale=1.0, height=1000, width=1000)
env = UnityEnvironment(base_port=5006, file_name=env_name, side_channels=[side_channel, engine_configuration_channel])

BHV_NAME_1 = "Building?team=0"
OBS_SHAPE_1 = (437,)
ACT_SHAPE_1 = (163,)
AGENT_TYPES = ["R", "R", "R", "AC"]
AGENTS = [None, None, None, None]

# Create the agents
for i in range(4):
    current_type = AGENT_TYPES[i]

    if current_type == "?":
        current_type = random.choice(ScriptedAgents.all_agent_types)
    if current_type == "R":
        AGENTS[i] = ScriptedAgents.RandomAgent(agent_id=i,
                                               behavior_name=BHV_NAME_1,
                                               action_shape=ACT_SHAPE_1,
                                               obs_shape=OBS_SHAPE_1)
    elif current_type == "P":
        AGENTS[i] = ScriptedAgents.PassiveAgent(agent_id=i,
                                                behavior_name=BHV_NAME_1,
                                                action_shape=ACT_SHAPE_1,
                                                obs_shape=OBS_SHAPE_1)
    elif current_type == "G":
        AGENTS[i] = ScriptedAgents.GreedyAgent(agent_id=i,
                                               behavior_name=BHV_NAME_1,
                                               action_shape=ACT_SHAPE_1,
                                               obs_shape=OBS_SHAPE_1)
    elif current_type == "S":
        AGENTS[i] = ScriptedAgents.StreetBuilderAgent(agent_id=i,
                                                      behavior_name=BHV_NAME_1,
                                                      action_shape=ACT_SHAPE_1,
                                                      obs_shape=OBS_SHAPE_1)
    elif current_type == "AC":
        AGENTS[i] = colonybot.ActorCritic(agent_id=i, behavior_name=BHV_NAME_1, action_shape=ACT_SHAPE_1,
                                          obs_shape=OBS_SHAPE_1, alpha=0.0001, beta=0.0001)

MAX_STEPS = 2000
MAX_EPISODES = 1000
steps = 0

tf.config.experimental_run_functions_eagerly(True)
ct = time.localtime(time.time())
log_name = "Log-" + str(ct.tm_mday) + "-" + str(ct.tm_mon) + "-" + str(ct.tm_year) + " " + str(ct.tm_hour) + "-" + \
           str(ct.tm_min) + "-" + str(ct.tm_sec) + ".txt"
log = open(file=log_name, mode="w+")
log.write("Episode,Steps,ActorLoss,CriticLoss,AgentAC_reward,Agent0_reward,Agent1_reward,Agent2_reward\n")
log.close()

for episode in range(MAX_EPISODES):
    # print("Start episode", episode, " -> wins :", str(wins))
    env.reset()
    done = False
    steps = 0

    for agent in AGENTS:
        agent.reset()

    while not done:

        env.step()
        steps += 1

        decision_steps, terminal_steps = env.get_steps(
            BHV_NAME_1)  # The decision steps (the agents info which need a decision this step)

        # The Game has ended because one of the agents won
        if len(terminal_steps.agent_id) > 0:
            done = True  # We are done
            for agent_id in terminal_steps.agent_id:
                terminal_step = terminal_steps[agent_id]  # The terminal step information of this agent
                current_agent = AGENTS[agent_id]
                current_agent.reward += terminal_step.reward  # Give their rewards

                if AGENT_TYPES[agent_id] == "AC":
                    current_agent.remember(state=current_agent.prev_observation, action=current_agent.prev_action,
                                           reward=terminal_step.reward,
                                           state_=terminal_step.obs[0],
                                           done=True)

        else:
            done = False
            decision_agent_id = decision_steps.agent_id[0]  # There can only be 1 agent that wants a decision
            current_agent = AGENTS[decision_agent_id]  # The agent that currently wants a decision
            decision_step = decision_steps[decision_agent_id]  # The step info for that agent

            observation = decision_step.obs[0]
            mask = decision_step.action_mask[0]
            action = current_agent.choose_action(obs=observation, mask=mask)
            reward = decision_step.reward - 0.1 * current_agent.prev_mask[current_agent.prev_action]
            # print(reward)
            current_agent.reward += reward

            if current_agent.__getType__() is "ActorCritic":
                #print(current_agent.prev_mask[current_agent.prev_action], reward)
                current_agent.remember(state=current_agent.prev_observation, action=current_agent.prev_action,
                                       reward=reward, state_=observation,
                                       done=False)

            if isinstance(action, list):
                current_agent.prev_action = action[0]
            else:
                current_agent.prev_action = action
            current_agent.prev_observation = observation
            current_agent.prev_reward = reward
            current_agent.prev_mask = mask

            if mask[action]:
                action = ScriptedAgents.get_random_action(mask=mask)
            # Tell the environment which action our agent wants to take
            action = np.array(action, ndmin=1)
            env.set_action_for_agent(behavior_name=BHV_NAME_1, agent_id=decision_agent_id, action=action)

    log = open(file=log_name, mode='a')
    agent = AGENTS[3]
    ah, ch = agent.learn()
    actor_loss = ah.history['loss'][0]
    critic_loss = ch.history['loss'][0]
    log_string = str(episode) + "," \
                 + str(steps) + "," \
                 + str(actor_loss) + "," \
                 + str(critic_loss) + "," \
                 + str(agent.reward) + "," \
                 + str(AGENTS[0].reward) + "," \
                 + str(AGENTS[1].reward) + "," \
                 + str(AGENTS[2].reward)
    log.write(log_string + "\n")
    log.close()

env.close()
