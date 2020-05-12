import time

import numpy as np
import sys
import random
import math
from RandomAgent import RandomAgent

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig, EngineConfigurationChannel
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel

env_name = "C:\\Users\\Gabi\\Documents\\GitHub\\ColonyBot\\Builds\\ColonyBot.exe"
train_mode = True  # Whether to run the environment in training or inference mode

engine_configuration_channel = EngineConfigurationChannel()
side_channel = FloatPropertiesChannel()
side_channel.set_property("number_of_players", 4)
side_channel.set_property("step_time", 0.1)
env = UnityEnvironment(base_port=5006, file_name=env_name, side_channels=[side_channel, engine_configuration_channel])
engine_configuration_channel.set_configuration_parameters(time_scale=1.0, height=800, width=800)

env.reset()
bn = env.get_behavior_names()
print("Different behaviours: ", bn)
agents = dict()

done = False
while not done:
    for name in bn:
        step_info = env.get_steps(name)  # The step info for this behaviour
        behaviour_spec = env.get_behavior_spec(behavior_name=name)
        decision_steps = step_info[0]  # The decision steps (the agents info which need a decision this step)
        terminal_steps = step_info[1]  # The Terminal steps (agents that are done)
        agent_ids = decision_steps.agent_id  # The global ID's (ndarray) of the agent in need of decision
        # local_id = decision_step.agent_id_to_index[agent_id[0]]  # The index of this agent in the decision step info
        for agent_id in agent_ids:
            branch_shapes = behaviour_spec.discrete_action_branches
            #print("Expected branch shapes : " + str(branch_shapes))
            #print("Expected action size: " + str(behaviour_spec.action_size))
            #print(decision_steps.obs[0][0])
            # Create a new agent if no agent exists
            if agents.keys().__contains__(agent_id) is False:
                random_agent = RandomAgent(id=agent_id,
                                           behavior_name=name,
                                           action_branches=branch_shapes,
                                           obs_shape=behaviour_spec.observation_shapes[0])
                agents[agent_id] = random_agent

            current_agent = agents[agent_id]
            a0 = current_agent.take_action(branch_id=0, obs=decision_steps.obs[0][0])
            a1 = current_agent.take_action(branch_id=1, obs=decision_steps.obs[0][0])
            #print("An ", type(current_agent), " (id=", agent_id, ") action is ", str(action_first_branch))
            #print("An ", type(current_agent), " (id=", agent_id, ") action is ", str(action_second_branch))
            action = np.array([a0, a1], dtype=np.int32)
            env.set_action_for_agent(behavior_name=current_agent.behavior_name,
                                     agent_id=current_agent.id,
                                     action=action)

    env.step()
env.close()
