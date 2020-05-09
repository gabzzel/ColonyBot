import time

import numpy as np
import sys
import random
import math

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig, EngineConfigurationChannel
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel

env_name = "C:\\Users\\Gabi van der Kooij\\Documents\\ColonyBot\\Builds\\ColonyBot.exe"
train_mode = True  # Whether to run the environment in training or inference mode

engine_configuration_channel = EngineConfigurationChannel()
side_channel = FloatPropertiesChannel()
side_channel.set_property("number_of_players", 1)
env = UnityEnvironment(base_port=5006, file_name=env_name, side_channels=[side_channel, engine_configuration_channel])
engine_configuration_channel.set_configuration_parameters(time_scale=1.0, height=800, width=800)

env.reset()
bn = env.get_behavior_names()
print(bn)
time.sleep(5)
env.close()




