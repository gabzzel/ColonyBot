import math
import random
from typing import Union, Tuple, Any

import numpy as np

import Agents

# Resource Types
LUMBER = 0
BRICK = 1
GRAIN = 2
DESERT = 3
VILLAGE = 1
STREET = 0


class Player:
    def __init__(self, ID: int, agent: Agents.Agent):
        self.ID = ID
        self.resources = [0, 0, 0]
        self.villages = []  # The gp indexes where there are villages
        self.streets = set()  # All gp indexes that are currently reached by streets of this player
        self.street_count = 0
        self.agent = agent
        self.valueable_steps = []

    def reset(self):
        self.resources = [0, 0, 0]
        self.villages.clear()
        self.streets.clear()
        self.street_count = 0
        self.valueable_steps.clear()

    def give_res(self, step, res_id):
        self.resources[res_id] += 1
        if not (step in self.valueable_steps):
            self.valueable_steps.append(step)


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

    def choose_action(self, obs, mask) -> int:
        return self.agent.choose_action(obs=obs, mask=mask)

    def get_stats_header(self):
        return self.agent.get_stats_header()

    def get_stats(self):
        return self.agent.get_stats()


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
            obs.append(0.0)
        else:
            obs.append(1.0 if self.building == player.ID else -1.0)
        obs.append(1.0 if self.index in player.streets else 0.0)
        obs.append(int(self.robber) * -1.0)
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


def actionIsVillage(x: int) -> bool:
    if x == 0:
        return False
    return (x - 1) % 2 == VILLAGE


class Board:
    def __init__(self, agent1: Agents.Agent, agent2: Agents.Agent, winning_reward=10, points_to_win=3,
                 auto_reward_per_step=-0.01, losing_reward=-10):
        self.players = [Player(ID=agent1.agent_id, agent=agent1), Player(ID=agent2.agent_id, agent=agent2)]
        self.current_player_index = 0  # The index of the player that's currently in turn
        self.current_player = self.players[self.current_player_index]  # The actual player that is currently in turn
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

                if gp.building != self.players[self.current_player_index].ID:
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

    def dice_roll(self, step):
        """ Roll the dice and distribute the resources """
        roll = random.randint(1, 3) + random.randint(1, 3)  # Roll a number with 2d4

        if roll == 4:
            i = 1
            # self.place_robber()
            # self.handle_robber()
        else:
            # Go through all tiles
            for tile in self.tiles:
                # If we come across a tile with as number the same as the roll
                if tile.number == roll and tile.robber == 0:
                    for gp_index in tile.connected_gridpoints:
                        gp = self.gridpoints[gp_index]
                        if gp.building != 0:
                            #self.players[gp.building - 1].resources[tile.resource] += 1
                            self.players[gp.building - 1].give_res(step=step, res_id=tile.resource)

    def get_mask(self, player: Player) -> list:

        # If our player has not build any villages yet, we are in the initial phase of the game
        if len(player.villages) < 1:
            mask = [True]
            for action_number in range(1, 49):

                # If we want to build a street, we can't! We need to build a village first.
                if not actionIsVillage(action_number):
                    mask.append(True)
                # If we want to build a village...
                else:
                    gi = math.floor((action_number - 1) / 2)
                    mask.append(not self.eligible_for_village(gridpoint=self.gridpoints[gi], free=True))

            return mask
        elif player.street_count < 1:
            mask = [True]
            for action_number in range(1, 49):
                # If we want to build a village, we can't in the initial phase
                if actionIsVillage(x=action_number):
                    mask.append(True)
                else:
                    gi = math.floor((action_number - 1) / 2)
                    mask.append(not self.eligible_for_street(gridpoint=self.gridpoints[gi]))
            return mask

        else:
            mask = [False]
            for action_number in range(1, 49):
                gi = math.floor((action_number - 1) / 2)
                if action_number in player.agent.actions_single:
                    mask.append(True)
                # We want to build a village!
                elif actionIsVillage(x=action_number):
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
                    elif player.street_count >= 7:
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
        if not free and not (gridpoint.index in self.players[self.current_player_index].streets):
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
            if neighbour.building == self.players[self.current_player_index].ID \
                    or neighbour_index in self.players[self.current_player_index].streets:
                return True

        return False

    def find_connection(self, player: Player, gridpoint: GridPoint) -> GridPoint:
        connection = None
        for neighbour_index in gridpoint.connected_gridpoints.keys():
            neighbour = self.gridpoints[neighbour_index]
            if neighbour.building == player.ID or neighbour.index in player.streets:
                connection = neighbour
        if connection is None:
            raise Exception("No connection found for player " + str(player.ID) + " to GP " + gridpoint.index)
        return connection

    def build_building(self, village: bool, gridpoint: int, player: Player) -> float:
        """Builds a building and returns the reward"""
        if village:
            player.villages.append(gridpoint)  # Append the gridpoint to the players' curriculum
            self.gridpoints[gridpoint].building = player.ID

            # Remove the resources
            if len(player.villages) > 1:
                player.resources = [r - 1 for r in player.resources]
            return 1.0  # Return the gained resource

        # We want to build a street.
        else:
            player.street_count += 1
            player.streets.add(gridpoint)
            gp = self.gridpoints[gridpoint]
            connection = self.find_connection(player=player, gridpoint=gp)
            player.streets.add(connection.index)
            # Connect the gridpoints
            gp.connected_gridpoints[connection.index] = connection.connected_gridpoints[gridpoint] = player.ID
            if player.street_count > 1:
                player.resources[0] -= 1
                player.resources[1] -= 1
            return 0.0

    def winner(self) -> Union[Player, None]:
        for player in self.players:
            if len(player.villages) >= self.points_to_win:
                return player
        return None

    def next_player(self, step):
        """Gives the turn to the next player and automatically rolls the dice and starts the auto trade"""
        winner = self.winner()
        if not (winner is None):
            raise Exception("Cannot go to next player if the game is already done! Winner is player " + str(winner.ID))

        # Get the next index
        self.current_player_index += 1
        if self.current_player_index >= len(self.players):
            self.current_player_index = 0

        # Set the current_player to the right value
        self.current_player = self.players[self.current_player_index]
        # Roll the dice
        self.dice_roll(step=step)
        # Initiate auto trade
        self.current_player.auto_trade()

    def step(self) -> Tuple[np.ndarray, Any, float, Union[np.ndarray, Any], bool, list, Any]:

        # The mask and observation of the current player
        mask = self.get_mask(player=self.current_player)
        obs = np.array(self.get_obs(player=self.current_player))

        # Let the current player choose an action
        action, probs = self.current_player.choose_action(obs=obs, mask=mask)

        # Initialize the reward
        reward = self.auto_reward_per_step

        if mask[action]:
            legal_actions = [c for c in range(len(mask)) if not mask[c]]
            raise Exception("Something went wrong. An illegal action was chosen. Action taken:" + str(action)
                            + ". Possible legal actions:" + str(legal_actions))

        if action != 0:
            # The gp where the player wants to build the village or build the street towards
            gi = math.floor((action - 1) / 2)
            reward += self.build_building(village=actionIsVillage(x=action), gridpoint=gi, player=self.current_player)

        winner = self.winner()
        done = not (winner is None)

        if done:
            if action == 0:
                raise Exception(
                    "Something weird happened. The game was ended with a Pass of player " + str(self.current_player.ID))
            elif not (winner is self.current_player):
                raise Exception("A game cannot be ended by the losing player!")
            reward += self.winning_reward

        next_obs = obs.copy() if action == 0 else np.array(self.get_obs(player=self.current_player))

        return obs, action, reward, next_obs, done, mask, probs

    def reset(self):
        self.robber = 0
        self.create_tgc()
        self.current_player_index = round(random.random())
        self.current_player = self.players[self.current_player_index]
        for player in self.players:
            player.reset()
        for gp in self.gridpoints:
            gp.reset()
        for tile in self.tiles:
            tile.reset()
