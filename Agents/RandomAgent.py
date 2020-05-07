import time

import numpy as np
import sys
import random
import math

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig, EngineConfigurationChannel
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel

env_name = "C:\\Users\\Gabi\\Documents\\GitHub\\ColonyBot\\Builds\\ColonyBot.exe"
train_mode = True  # Whether to run the environment in training or inference mode

print("Python version:")
print(sys.version)

# check Python version
if sys.version_info[0] < 3:
    raise Exception("ERROR: ML-Agents Toolkit (v0.3 onwards) requires Python 3")

engine_configuration_channel = EngineConfigurationChannel()
side_channel = FloatPropertiesChannel()
side_channel.set_property("number_of_players", 3)
env = UnityEnvironment(base_port=5006, file_name=env_name, side_channels=[side_channel, engine_configuration_channel])

# Reset the environment
# Set the time scale of the engine

engine_configuration_channel.set_configuration_parameters(time_scale=1.0, height=800, width=800)

env.reset()
print("Hallo")

for episode in range(100):
    print("Starting episode:", episode)
    time.sleep(1)
    env.reset()
env.close()



