import math
import random
import time
import numpy as np

import colonybot
import ScriptedAgents
import Agents
import tensorflow as tf

# Resource Types
LUMBER = 0
BRICK = 1
GRAIN = 2
DESERT = 3

action_shape = (49,)
obs_shape = (147,)

tf.config.threading.set_inter_op_parallelism_threads(4)
tf.config.threading.set_intra_op_parallelism_threads(4)

# AGENTS = [colonybot.ActorCritic(agent_id=1, action_shape=action_shape, obs_shape=obs_shape, behavior_name="", alpha=0.0001, beta=0.0001),
#          ScriptedAgents.RandomAgent(agent_id=2, action_shape=action_shape, obs_shape=obs_shape, behavior_name="")]
AGENTS = [Agents.ActorCritic(agent_id=0, action_shape=action_shape, obs_shape=obs_shape, alpha=0.001, beta=0.001),
          Agents.RandomAgent(agent_id=1, action_shape=action_shape, obs_shape=obs_shape)]


def SlidingAverage1000(x : list):
    if len(x) < 1000:
        return sum(x) / len(x)
    else:
        return sum(x[-1000:]) / 1000

class Player:
    def __init__(self, ID: int, agent: Agents.Agent):
        self.ID = ID
        self.resources = [0, 0, 0]
        self.villages = []  # The gp indexes where there are villages
        self.streets = set()  # All gp indexes that are currently reached by streets of this player
        self.street_count = 0
        self.agent = agent

    def reset(self):
        self.resources = [0, 0, 0]
        self.villages.clear()
        self.streets.clear()
        self.street_count = 0

    def auto_trade(self):
        """Automatically trade excess resources for resources you have the least of"""
        h = max(self.resources)
        l = min(self.resources)
        hres = self.resources.index(h)
        lres = self.resources.index(l)

        while h > 3 and l < h - 3:
            self.resources[hres] -= 3
            self.resources[lres] += 1
            h = max(self.resources)
            l = min(self.resources)
            hres = self.resources.index(h)
            lres = self.resources.index(l)

    def choose_action(self, obs, mask):
        return self.agent.choose_action(obs=obs, mask=mask)


class GridPoint:
    def __init__(self, index, tiles, gps):
        self.index = index
        self.resource_values = [0, 0, 0]

        for tile in tiles:
            if tile.resource > 2:
                continue
            self.resource_values[tile.resource] += tile.probability

        self.robber = 0
        self.building = 0
        self.connected_tiles = tiles
        self.connected_gridpoints = {}
        for gp in gps:
            self.connected_gridpoints[gp] = 0

    def reset(self):
        self.robber = 0
        self.building = 0
        for gp_index in self.connected_gridpoints.keys():
            self.connected_gridpoints[gp_index] = 0

    def get_obs(self, player):
        obs = self.resource_values[:]
        if self.building == 0:
            obs.append(float(0))
        else:
            obs.append(float(1) if self.building == player.ID else float(-1))
        obs.append(float(1) if self.index in player.streets else float(0))
        obs.append(float(int(self.robber) * -1))
        return obs


class Tile:
    def __init__(self, index, res, number, gps):

        if res < 0 or res > 3:
            raise Exception("Invalid resource" + str(res) + "number when creating Tile!")
        elif len(gps) != 6:
            raise Exception(
                "Error creating Tile. Not the right amount to GridPoints (6 required, " + str(len(gps)) + " given)")

        self.index = index  # The index of this Tile in the Board.tiles list
        self.resource = res  # The resource that this tile contains
        self.number = number  # The number that needs to be thrown with dice to get res from this tile
        self.probability = 0.25 - 0.0625 * abs(5 - number)
        self.robber = 0
        self.connected_gridpoints = gps

    def reset(self):
        self.robber = 0


class Board:
    def __init__(self, agent1: Agents.Agent, agent2: Agents.Agent, winning_reward=10, points_to_win=3, auto_reward_per_step=-0.01, losing_reward=-10):
        self.players = [Player(ID=1, agent=agent1), Player(ID=2, agent=agent2)]
        self.current_player = 0  # The index of the player that's currently in turn
        self.points_to_win = points_to_win
        self.winning_reward = winning_reward
        self.losing_reward = losing_reward
        self.auto_reward_per_step = auto_reward_per_step
        self.tiles = []
        self.gridpoints = []  # A GP is a tuple : (building, connectd_tile_indexes[])
        self.connections = {}  # A 2D array that contains information about the gp-connections
        self.robber = 0

    def create_tgc(self):
        """Creates the tiles, GPs and connections for the board"""
        # self.tiles = [(0, DESERT, 0), (4, LUMBER, 0.1875), (2, BRICK, 0.0625), (7, BRICK, 0.125), (6, GRAIN, 0.1875),
        #              (8, LUMBER, 0.0625), (3, GRAIN, 0.125)]

        self.tiles = [Tile(index=0, res=DESERT, number=0, gps=[7, 8, 11, 12, 15, 16]),
                      Tile(index=1, res=LUMBER, number=5, gps=[0, 1, 3, 4, 7, 8]),
                      Tile(index=2, res=BRICK, number=2, gps=[4, 5, 8, 9, 12, 13]),
                      Tile(index=3, res=BRICK, number=5, gps=[12, 13, 16, 17, 20, 21]),
                      Tile(index=4, res=GRAIN, number=3, gps=[15, 16, 19, 20, 22, 23]),
                      Tile(index=5, res=LUMBER, number=6, gps=[10, 11, 14, 15, 18, 19]),
                      Tile(index=6, res=GRAIN, number=3, gps=[2, 3, 6, 7, 10, 11])]

        self.gridpoints = [GridPoint(index=0, tiles=[self.tiles[1]], gps=[1, 3]),
                           GridPoint(index=1, tiles=[self.tiles[1]], gps=[0, 4]),
                           GridPoint(index=2, tiles=[self.tiles[6]], gps=[3, 6]),
                           GridPoint(index=3, tiles=[self.tiles[1], self.tiles[6]], gps=[0, 2, 7]),
                           GridPoint(index=4, tiles=[self.tiles[1], self.tiles[2]], gps=[1, 5, 8]),
                           GridPoint(index=5, tiles=[self.tiles[2]], gps=[4, 9]),
                           GridPoint(index=6, tiles=[self.tiles[6]], gps=[2, 10]),
                           GridPoint(index=7, tiles=[self.tiles[0], self.tiles[1], self.tiles[6]], gps=[3, 8, 11]),
                           GridPoint(index=8, tiles=[self.tiles[0], self.tiles[1], self.tiles[2]], gps=[4, 7, 12]),
                           GridPoint(index=9, tiles=[self.tiles[2]], gps=[5, 13]),
                           GridPoint(index=10, tiles=[self.tiles[5], self.tiles[6]], gps=[6, 11, 14]),
                           GridPoint(index=11, tiles=[self.tiles[0], self.tiles[5], self.tiles[6]], gps=[7, 10, 15]),
                           GridPoint(index=12, tiles=[self.tiles[0], self.tiles[2], self.tiles[3]], gps=[8, 13, 16]),
                           GridPoint(index=13, tiles=[self.tiles[2], self.tiles[3]], gps=[9, 12, 17]),
                           GridPoint(index=14, tiles=[self.tiles[5]], gps=[10, 18]),
                           GridPoint(index=15, tiles=[self.tiles[0], self.tiles[4], self.tiles[5]], gps=[11, 16, 19]),
                           GridPoint(index=16, tiles=[self.tiles[0], self.tiles[3], self.tiles[4]], gps=[12, 15, 20]),
                           GridPoint(index=17, tiles=[self.tiles[3]], gps=[13, 21]),
                           GridPoint(index=18, tiles=[self.tiles[5]], gps=[14, 19]),
                           GridPoint(index=19, tiles=[self.tiles[4], self.tiles[5]], gps=[15, 18, 22]),
                           GridPoint(index=20, tiles=[self.tiles[3], self.tiles[4]], gps=[16, 21, 23]),
                           GridPoint(index=21, tiles=[self.tiles[3]], gps=[17, 20]),
                           GridPoint(index=22, tiles=[self.tiles[4]], gps=[19, 23]),
                           GridPoint(index=23, tiles=[self.tiles[4]], gps=[20, 22])]

    def place_robber(self):

        best_tile = None
        highest = -100

        for tile in self.tiles:
            if tile.robber == 1:
                continue

            tile_value = 0
            for gp_index in tile.connected_gridpoints:
                gp = self.gridpoints[gp_index]

                if gp.building == 0:
                    continue

                if gp.building != self.players[self.current_player].ID:
                    tile_value += tile.probability

            if tile_value > highest:
                best_tile = tile
                highest = tile_value

        self.tiles[self.robber].robber = 0  # Set the old robber location to robber = 0
        self.robber = best_tile.index  # Set the robber to the new location
        best_tile.robber = 1  # Make sure the new robber tile has a good reference

    def handle_robber(self):
        for player in self.players:
            if sum(player.resources) <= 5:
                continue

            throw_away = math.floor(sum(player.resources) / 2)
            while throw_away > 0:

                total_res = sum(player.resources)
                so_far = 0
                random_index = random.randint(1, total_res - 1)
                for res_index in range(len(player.resources)):
                    so_far += player.resources[res_index]
                    if so_far >= random_index:
                        player.resources[res_index] -= 1
                        break

                throw_away -= 1

    def dice_roll(self):
        """ Roll the dice and distribute the resources """
        roll = random.randint(1, 3) + random.randint(1, 3)  # Roll a number with 2d4

        if roll == 4:
            i = 1
            #self.place_robber()
            #self.handle_robber()
        else:
            # Go through all tiles
            for tile in self.tiles:
                # If we come across a tile with as number the same as the roll
                if tile.number == roll and tile.robber == 0:
                    for gp_index in tile.connected_gridpoints:
                        gp = self.gridpoints[gp_index]
                        if gp.building != 0:
                            self.players[gp.building - 1].resources[tile.resource] += 1

        # print("Dice roll", roll, "! P1-res=", str(self.players[0].resources), " |P2-res=", str(self.players[1].resources))

    def get_mask(self, player: Player) -> list:

        # If our player has not build any villages yet, we are in the initial phase of the game
        if len(player.villages) < 1:
            mask = [True]
            for action_number in range(1, 49):

                # If we want to build a street, we can't! We need to build a village first.
                if action_number % 2 == 1:
                    mask.append(True)
                # If we want to build a village...
                else:
                    gi = math.floor((action_number - 1) / 2)
                    mask.append(not self.eligible_for_village(gridpoint=self.gridpoints[gi], free=True))

            return mask
        elif player.street_count < 1:
            mask = [True]
            for action_number in range(1, 49):
                if action_number % 2 == 0:
                    mask.append(True)
                else:
                    gi = math.floor((action_number - 1) / 2)
                    mask.append(not self.eligible_for_street(gridpoint=self.gridpoints[gi]))
            return mask

        else:
            mask = [False]
            for action_number in range(1, 49):

                gi = math.floor((action_number - 1) / 2)
                # We want to build a village!
                if (action_number - 1) % 2 == 1:
                    if player.resources[0] < 1:
                        mask.append(True)
                    elif player.resources[1] < 1:
                        mask.append(True)
                    elif player.resources[2] < 1:
                        mask.append(True)
                    elif not self.eligible_for_village(gridpoint=self.gridpoints[gi], free=False):
                        mask.append(True)
                    else:
                        mask.append(False)
                # We want to build a street!
                else:
                    if player.resources[0] < 1 or player.resources[1] < 1:
                        mask.append(True)
                    elif player.street_count >= 10:
                        mask.append(True)
                    elif not self.eligible_for_street(gridpoint=self.gridpoints[gi]):
                        mask.append(True)
                    else:
                        mask.append(False)

            return mask

    def get_obs(self, player: Player) -> list:
        obs = player.resources[:]
        for gp in self.gridpoints:
            obs.extend(gp.get_obs(player=player))
        return obs

    def eligible_for_village(self, gridpoint: GridPoint, free: bool) -> bool:

        # If this gridpoint is already occupied, we can't place a village here
        if gridpoint.building != 0:
            return False

        # If one of our neighbours is occupied, we can't build here
        for neighbour_index in gridpoint.connected_gridpoints.keys():
            neighbour = self.gridpoints[neighbour_index]
            if neighbour.building != 0:
                return False

        # If we don't have free placement and we are not connected to the street network, return False
        if not free and not (gridpoint.index in self.players[self.current_player].streets):
            return False

        return True

    def eligible_for_street(self, gridpoint: GridPoint) -> bool:

        for neighbour_index in gridpoint.connected_gridpoints.keys():
            # If this connection is already saturated with a street, this neighbour does not have to be checked
            if gridpoint.connected_gridpoints[neighbour_index] != 0:
                continue

            neighbour = self.gridpoints[neighbour_index]
            # If our neighbour has a building on it that belongs to the current player,
            # OR if our neighbour is connected to the street network of the current player...
            # we have found a connection
            if neighbour.building == self.players[self.current_player].ID \
                    or neighbour_index in self.players[self.current_player].streets:
                return True

        return False

    def step(self):
        current_player = self.players[self.current_player]
        mask = self.get_mask(player=current_player)
        obs = np.array(self.get_obs(player=current_player))
        action = current_player.choose_action(obs=obs, mask=mask)
        reward = self.auto_reward_per_step
        if mask[action]:
            if isinstance(current_player.agent, Agents.RandomAgent) or isinstance(current_player.agent, ScriptedAgents.RandomAgent):
                raise Exception("Something went wrong. A randomagent choose an illegal action...")
            action = Agents.get_random_action(mask=mask)

        # If a player passes, give to turn to the other player
        if action == 0:
            self.current_player = 0 if self.current_player == 1 else 1
            # print("Player", current_player.ID, "passes")
            self.dice_roll()
            self.players[self.current_player].auto_trade()
        else:
            building = (action - 1) % 2
            # The gp where the player wants to build the village or build the street towards
            gi = math.floor((action - 1) / 2)
            if building == 1:
                current_player.villages.append(gi)  # Indicate for the player itself that we build a village on this gp
                reward += 1
                self.gridpoints[gi].building = current_player.ID  # Actually build it
                if len(current_player.villages) > 1:
                    current_player.resources[0] -= 1
                    current_player.resources[1] -= 1
                    current_player.resources[2] -= 1
                # print("Player", current_player.ID, "build Village on", str(gi))
            elif building == 0:
                current_player.street_count += 1
                current_player.streets.add(gi)

                gridpoint = self.gridpoints[gi]
                connection = None
                for neighbour_index in gridpoint.connected_gridpoints.keys():
                    neighbour = self.gridpoints[neighbour_index]
                    if neighbour.building == current_player.ID or neighbour.index in current_player.streets:
                        connection = neighbour
                if connection is None:
                    raise Exception("This is not good...")
                if current_player.street_count > 1:
                    current_player.resources[0] -= 1
                    current_player.resources[1] -= 1
                current_player.streets.add(connection.index)
                # print("Player", current_player.ID, "builds Street between", gi, "and", connection.index)

        done = len(current_player.villages) >= self.points_to_win
        if done:
            reward += self.winning_reward
        next_obs = obs.copy() if action == 0 else np.array(self.get_obs(player=current_player))
        current_player.agent.reward += reward
        return obs, action, reward, next_obs, done

    def reset(self):
        self.robber = 0
        self.create_tgc()
        self.current_player = round(random.random())
        for player in self.players:
            player.reset()
        for gp in self.gridpoints:
            gp.reset()
        for tile in self.tiles:
            tile.reset()


run_id = 14
ct = time.localtime(time.time())
log_name = "Simplified\\Log-" + str(run_id) + " @ " + str(ct.tm_mday) + "-" + str(ct.tm_mon) + "-" + str(ct.tm_year) + " " + str(ct.tm_hour) + "-" + \
           str(ct.tm_min) + "-" + str(ct.tm_sec) + ".txt"
log = open(file=log_name, mode="w+")
log.write("Time,Episode,Steps,Steps-RA,Steps-SA,ActorLoss,ActorLoss-RA,ActorLoss-SA,CriticLoss,CriticLoss-RA,CriticLoss-SA,"
          "AgentAC_reward,Reward-RA,Reward-SA,AgentAC_Win,Wins-RA,Wins-SA,Agent0_reward\n")
log.close()

env = Board(agent1=AGENTS[0], agent2=AGENTS[1], auto_reward_per_step=-0.05, winning_reward=10, losing_reward=0)

max_episodes = 100000
max_steps = 400

total_time = 0
steps_list = []
total_turns = 0
actor_losses = []
critic_losses = []
rewards = []
wins = []

for episode in range(max_episodes):
    start_time = time.time()
    # print("Starting episode", episode)
    steps = 0
    done = False
    env.reset()

    for agent in AGENTS:
        agent.reset()

    while not done:
        obs, action, reward, next_obs, done = env.step()
        steps += 1
        if action == 0:
            total_turns += 1
        if steps >= max_steps:
            done = True
            wins.append(0)
        # If we are done in a normal fashion...
        elif done:
            if env.current_player == 0:
                wins.append(1)
            else:
                wins.append(0)

            for agent in AGENTS:
                # If we are not the agent currently in play, we have apparently lost...
                if agent.agent_id != env.current_player:
                    agent.rewards[-1] += env.losing_reward
                    agent.reward += env.losing_reward
                if not agent.dones[-1]:
                    agent.dones[-1] = True

        for agent in AGENTS:
            if agent.agent_id == env.current_player:
                agent.remember(state=obs, action=action, reward=reward, new_state=next_obs, done=done)

    duration = round(time.time() - start_time, 2)
    print("Episode", episode, "done in", duration, "sec. and", steps, "steps. Result:",
          str([(p.ID, p.agent.reward) for p in env.players]))
    total_time += duration
    steps_list.append(steps)
    rewards.append(AGENTS[0].reward)

    for agent in AGENTS:
        if isinstance(agent, colonybot.ActorCritic) or isinstance(agent, Agents.ActorCritic):
            ah, ch = agent.learn()
            actor_loss = ah.history['loss'][0]
            critic_loss = ch.history['loss'][0]
            actor_losses.append(actor_loss)
            critic_losses.append(critic_loss)
            log = open(file=log_name, mode='a')

            log_string = str(time.time()) + "," \
                         + str(episode) + "," \
                         + str(steps) + "," \
                         + str(sum(steps_list) / (episode + 1)) + "," \
                         + str(SlidingAverage1000(steps_list)) + "," \
                         + str(actor_loss) + "," \
                         + str(sum(actor_losses) / (episode + 1)) + "," \
                         + str(SlidingAverage1000(actor_losses)) + "," \
                         + str(critic_loss) + "," \
                         + str(sum(critic_losses) / (episode + 1)) + "," \
                         + str(SlidingAverage1000(critic_losses)) + "," \
                         + str(AGENTS[0].reward) + "," \
                         + str(sum(rewards) / (episode + 1)) + "," \
                         + str(SlidingAverage1000(rewards)) + "," \
                         + str(int(env.current_player == 0)) + "," \
                         + str(sum(wins) / (episode + 1)) + "," \
                         + str(SlidingAverage1000(wins)) + "," \
                         + str(AGENTS[1].reward)
            log.write(log_string + "\n")
            log.close()
            agent.save(folder="Simplified", id=10)


print("Completed", max_episodes, "episodes. Elapsed :", str(round(total_time)),
      "sec. (avg", str(round(total_time / max_episodes, 3)), "p.e.). Total steps:", sum(steps_list), "(avg",
      str(round(sum(steps_list) / max_episodes)), "p.e.). Turns:", total_turns, "/", str(total_turns/2), "Rounds (avg",
      str(round(total_turns / max_episodes, 3)), "p.e.)")
