import os
import time

from typing import List

import Board
import Agents


def sliding_avg(x: list, window=1000, soft=True):
    if len(x) < window:
        if soft:
            return sum(x) / len(x)
        else:
            return sum(x) / window
    else:
        return sum(x[-window:]) / window


class Logger:
    def __init__(self, run_id, sliding_window_size=1000):
        self.run_id = run_id
        self.log_path = "Logs\\Run " + str(run_id)
        self.prepare_dirs()
        self.sliding_window_size = sliding_window_size

    def prepare_dirs(self):
        # log_dir = "Logs\\" + str(self.run_id)
        if not os.path.exists("Logs"):
            os.mkdir("Logs")

        if os.path.exists(self.log_path):
            raise Exception("Run " + str(self.run_id) + " does already exist. Cannot log.")
        else:
            os.mkdir(self.log_path)

    def write_episode(self, episode: int, steps: list, spi: int, players: List[Board.Player], sep=";"):
        log_name = self.log_path + "\\" + "Run" + str(self.run_id) + ".txt"
        log = open(file=log_name, mode='a')
        if os.stat(path=log_name).st_size == 0:
            initial_string = "Time" + sep + "Episode" + sep + "StartingPlayer" + sep \
                             + "Steps" + sep + "Steps-RA" + sep + "Steps-SA"
            for player in players:
                for header_element in player.get_stats_header():
                    he = header_element + str(player.ID)
                    initial_string += sep + str(he) + sep + str(he) + "-RA" \
                                      + sep + str(he) + "-SA"

            log.write(initial_string + "\n")

        string = str(time.time()) + sep + str(episode) + sep + str(spi) + sep \
                 + str(steps[-1]) + sep + str(sum(steps) / (episode + 1)) + sep \
                 + str(sliding_avg(steps, window=self.sliding_window_size))

        for player in players:
            for stat in player.get_stats():
                string += sep + str(stat[-1]) + sep + str(sum(stat) / (episode + 1)) + sep \
                          + str(sliding_avg(stat, window=self.sliding_window_size))

        log.write(string + "\n")
        log.close()

    def write_step(self, episode: int, step: int, current_player: int, action: int, resources: list, points: float,
                   sep=";", probs=None):

        # Check whether the subfolder of the run for episodes exists
        episode_logs_folder_path = self.log_path + "\\Episodes"
        if not os.path.exists(episode_logs_folder_path):
            os.mkdir(path=episode_logs_folder_path)

        # The path to the actual log
        new_path = episode_logs_folder_path + "\\" + str(episode) + ".txt"
        log = open(file=new_path, mode='a')
        if os.stat(path=new_path).st_size == 0:
            initial_string = "Time" + sep + "Step" + sep + "CurrentPlayer" \
                             + sep + "Action" + sep + "Resources" + sep + "Points" + sep + "Probs"
            log.write(initial_string + "\n")

        string = str(time.time()) + sep + str(step) + sep + str(current_player) + sep + str(action) \
                 + sep + str(resources) + sep + str(points)

        if probs is None:
            string += sep + "None"
        else:
            string += sep + str(list(probs))

        log.write(string + "\n")
        log.close()

    def write_config(self, run_id: int, max_episodes: int, max_steps: int, env: Board.Board,
                     agents: List[Agents.Agent]):
        log = open(file=self.log_path + "\\Config.txt", mode='w')
        string = "Run: " + str(run_id) + "\n"
        string += "Max. Episodes: " + str(max_episodes) + "\n"
        string += "Max. Steps: " + str(max_steps) + "\n"
        string += "Number of Agents: " + str(len(env.players)) + "\n"
        string += "Points to win: " + str(env.points_to_win) + "\n"
        string += "Winning/losing rewards: " + str(env.winning_reward) + "/" + str(env.losing_reward) + "\n"
        string += "Auto Step reward: " + str(env.auto_reward_per_step) + "\n"
        for agent in agents:
            string += " \n"
            config = agent.get_config()
            for key in config.keys():
                string += str(key) + str(config[key]) + "\n"
        log.write(string)
        log.close()
