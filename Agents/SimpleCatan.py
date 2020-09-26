import time
import Agents
from Board import Board
from Logger import Logger as Logger

action_shape = (49,)
obs_shape = (147,)

averages = []

runs = [108]

for run_id in runs:
    AGENTS = [Agents.ActorCritic(agent_id=1, action_shape=action_shape, obs_shape=obs_shape, debug_mode=True, actor_loss_type='mse'),
              Agents.RandomAgent(agent_id=2, action_shape=action_shape, obs_shape=obs_shape, debug_mode=True)]

    env = Board(agent1=AGENTS[0], agent2=AGENTS[1], auto_reward_per_step=-0.02, winning_reward=10, losing_reward=0)

    max_episodes = 200
    max_steps = 400
    episode_log_frequency = 1  # Per how many episode a whole episode should be logged

    total_time = 0
    steps_list = []
    total_turns = 0

    logger = Logger(run_id=run_id)
    logger.write_config(run_id=run_id, max_episodes=max_episodes, max_steps=max_steps, env=env, agents=AGENTS)
    for episode in range(max_episodes):
        start_time = time.time()
        # print("Starting episode", episode)
        steps = 0
        done = False
        env.reset()
        starting_player_id = env.current_player.ID

        for a in AGENTS:
            a.reset()

        while not done:
            obs, action, reward, next_obs, done, mask, probs = env.step()
            # If action = 0, in the env the current player has changed. Use current_player instead!
            steps += 1
            total_turns += int(action == 0)

            if steps >= max_steps:
                done = True

            # Remember this step-info for learning and/or stats
            env.current_player.agent.remember(state=obs, action=action, reward=reward, new_state=next_obs, done=done)

            if done:
                winner = env.winner()

                # Loop trough all players
                for player in env.players:

                    # Make sure that the last is on done
                    player.agent.dones[-1] = True

                    # If we are done, but not the winner, we need to compensate.
                    if player != winner and winner is not None:
                        player.agent.rewards[-1] += env.losing_reward

                    player.agent.r_wins.append(int(player == winner))
                    player.agent.r_rewards.append(sum(player.agent.rewards))

            if episode % episode_log_frequency == 0:
                logger.write_step(episode=episode, step=steps, current_player=env.current_player.ID, action=action,
                                  resources=env.current_player.resources, points=sum(env.current_player.agent.rewards),
                                  probs=probs)

            if action == 0:
                env.next_player(steps)

        duration = round(time.time() - start_time, 2)
        print("Episode", episode, "of run", str(run_id), "done in", duration, "sec. and", steps, "steps. Result:",
              str([(p.ID, round(p.agent.r_rewards[-1], 2)) for p in env.players]))
        total_time += duration
        steps_list.append(steps)

        for agent in AGENTS:
            agent.finish(folder=logger.log_path, run_id=run_id)

        logger.write_episode(episode=episode, spi=starting_player_id, players=env.players, steps=steps_list)

        for player in env.players:
            deltas = []
            for i in range(len(player.valueable_steps) - 1):
                x = i + 1
                deltas.append(player.valueable_steps[x] - player.valueable_steps[x - 1])
            #print("Average steps between resources for player " + str(player.ID) + " is " + str(sum(deltas) / len(deltas)))
            averages.append(sum(deltas) / len(deltas))


    print("Average steps between resources = " + str(sum(averages) / len(averages)))
    print("Completed run", run_id, "with ", max_episodes, "episodes.  Elapsed :", str(round(total_time)),
          "sec. (avg", str(round(total_time / max_episodes, 3)), "p.e.). Total steps:", sum(steps_list), "(avg",
          str(round(sum(steps_list) / max_episodes)), "p.e.). Turns:", total_turns, "/", str(total_turns / 2),
          "Rounds (avg",
          str(round(total_turns / max_episodes, 3)), "p.e.)")
