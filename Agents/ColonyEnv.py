import random

import numpy as np
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel

import ScriptedAgents

env_name = "C:\\Users\\Gabi\\Documents\\GitHub\\ColonyBot\\Builds\\ColonyBot.exe"
train_mode = True  # Whether to run the environment in training or inference mode

engine_configuration_channel = EngineConfigurationChannel()
side_channel = EnvironmentParametersChannel()
side_channel.set_float_parameter("step_time", 0.01)
side_channel.set_float_parameter("show_ui", 0)
side_channel.set_float_parameter("standard_board", 1)
side_channel.set_float_parameter("points_to_win", 5)
engine_configuration_channel.set_configuration_parameters(time_scale=1.0, height=1000, width=1000)
env = UnityEnvironment(base_port=5006, file_name=env_name, side_channels=[side_channel, engine_configuration_channel])

# env.reset()

agents = dict()
max_episode = 10
steps = 0
max_steps = 3000
agent_types = ["R", "P", "G", "S"]

for episode in range(max_episode):
    print("Start episode", episode)
    env.reset()
    bn = env.get_behavior_names()
    behaviour_name = bn[0]
    done = False
    steps = 0
    agents = dict()

    for agent in agents.keys():
        agents[agent].reset()

    while not done:

        env.step()
        steps += 1
        
        if steps > max_steps:
            done = True
            break

        step_info = env.get_steps(behaviour_name)  # The step info for this behaviour
        behaviour_spec = env.get_behavior_spec(behavior_name=behaviour_name)
        decision_steps = step_info[0]  # The decision steps (the agents info which need a decision this step)
        terminal_steps = step_info[1]  # The Terminal steps (agents that are done)
        agent_ids = decision_steps.agent_id  # The global ID's (ndarray) of the agent in need of decision
        # local_id = decision_step.agent_id_to_index[agent_id[0]]  # The index of this agent in the decision step info

        if len(terminal_steps) > 0:
            reward_string = "| "
            for agent_id in terminal_steps.agent_id:
                terminal_step = terminal_steps[agent_id]
                reward_string += agents[agent_id].__getType__() + " (" + str(agent_id) + ") = " + str(terminal_step.reward + agents[agent_id].reward) + " | "
            print("Episode", str(episode), "ended in ", steps, "steps :", reward_string)
            done = True

        else:
            for agent_id in agent_ids:

                branch_shapes = behaviour_spec.discrete_action_branches
                # print("Expected branch shapes : " + str(branch_shapes))
                # print("Expected action size: " + str(behaviour_spec.action_size))
                # print(decision_steps.obs[0][0])
                # Create a new agent if no agent exists
                if agents.keys().__contains__(agent_id) is False:
                    agent = None
                    random_type = random.choice(agent_types)
                    if random_type == "R":
                        agent = ScriptedAgents.RandomAgent(agent_id=agent_id,
                                                           behavior_name=behaviour_name,
                                                           action_shape=branch_shapes[0],
                                                           obs_shape=behaviour_spec.observation_shapes[0])
                    elif random_type == "P":
                        agent = ScriptedAgents.PassiveAgent(agent_id=agent_id,
                                                            behavior_name=behaviour_name,
                                                            action_shape=branch_shapes[0],
                                                            obs_shape=behaviour_spec.observation_shapes[0],
                                                            pass_chance=0.99)
                    elif random_type == "G":
                        agent = ScriptedAgents.GreedyAgent(agent_id=agent_id,
                                                           behavior_name=behaviour_name,
                                                           action_shape=branch_shapes[0],
                                                           obs_shape=behaviour_spec.observation_shapes[0])

                    elif random_type == "S":
                        agent = ScriptedAgents.StreetBuilderAgent(agent_id=agent_id,
                                                                  behavior_name=behaviour_name,
                                                                  action_shape=branch_shapes[0],
                                                                  obs_shape=behaviour_spec.observation_shapes[0])

                    agents[agent_id] = agent

                current_agent = agents[agent_id]
                current_agent.reward += decision_steps[agent_id].reward
                obs = decision_steps.obs[0][0]
                mask = decision_steps.action_mask[0][0]
                a = current_agent.__take_action__(obs=obs, mask=mask)
                action = np.array(a, ndmin=1)
                # print("Agent", agent_id, "takes action", action, ":", ScriptedAgents.action_to_string(a))
                env.set_action_for_agent(behavior_name=current_agent.behavior_name,
                                         agent_id=current_agent.agent_id,
                                         action=action)

env.close()
