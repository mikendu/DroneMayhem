# -*- coding: utf-8 -*-
#
#     ||          ____  _ __
#  +------+      / __ )(_) /_______________ _____  ___
#  | 0xBC |     / __  / / __/ ___/ ___/ __ `/_  / / _ \
#  +------+    / /_/ / / /_/ /__/ /  / /_/ / / /_/  __/
#   ||  ||    /_____/_/\__/\___/_/   \__,_/ /___/\___/
#
#  Copyright (C) 2017 Bitcraze AB
#
#  Crazyflie Nano Quadcopter Client
#
#  This program is free software; you can redistribute it and/or
#  modify it under the terms of the GNU General Public License
#  as published by the Free Software Foundation; either version 2
#  of the License, or (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
#  You should have received a copy of the GNU General Public License
#  along with this program; if not, write to the Free Software
#  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
#  MA  02110-1301, USA.
"""
A script to fly 5 Crazyflies in formation. One stays in the center and the
other four fly around it in a circle. Mainly intended to be used with the
Flow deck.
The starting positions are vital and should be oriented like this
     >
^    +    v
     <
The distance from the center to the perimeter of the circle is around 0.5 m
"""
import math
import time
import os

import cflib.crtp
from cflib.crazyflie.swarm import CachedCfFactory
from cflib.crazyflie.swarm import Swarm
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie import Crazyflie

"""
radio://*/55/2M/E7E7E7E701?safelink=1&autoping=1
-- Drone 1 	|	 Connecting to address radio://*/55/2M/E7E7E7E704?safelink=1&autoping=1
-- Drone 2 	|	 Connecting to address radio://*/55/2M/E7E7E7E705?safelink=1&autoping=1
-- Drone 3 	|	 Connecting to address radio://*/55/2M/E7E7E7E706?safelink=1&autoping=1
"""

os.environ["USE_CFLINK"] = "cpp"
cflib.crtp.init_drivers()


# Change uris according to your setup
URI0 = 'radio://0/80/2M/E7E7E7E701'
URI1 = 'radio://0/40/2M/E7E7E7E704'
URI2 = 'radio://0/1/2M/E7E7E7E702'
URI3 = 'radio://0/1/2M/E7E7E7E701'

# d: diameter of circle
# z: altitude
params0 = {'y': 0.6, 'delay': 0.0}
params1 = {'y': 0.2, 'delay': 0.5}
params3 = {'y': -0.2, 'delay': 1.0}
params4 = {'y': -0.6, 'delay': 1.5}


uris = {
    URI0,
    URI1,
    URI2,
    URI3,
}

params = {
    URI0: [params0],
    URI1: [params1],
    URI2: [params3],
    URI3: [params4],
}


def reset_estimator(scf):

    scf.cf.light_controller.set_color(255, 0, 0, 0.0, True)
    cf = scf.cf
    cf.param.set_value('lighthouse.method', '0')
    cf.param.set_value('lighthouse.systemType', '2')
    cf.param.set_value('stabilizer.controller', '1')
    cf.param.set_value('stabilizer.estimator', '2')
    cf.param.set_value('commander.enHighLevel', '1')
    time.sleep(1.5)


    cf.param.set_value('kalman.resetEstimation', '1')
    time.sleep(0.1)
    cf.param.set_value('kalman.resetEstimation', '0')
    time.sleep(2.0)

    scf.cf.light_controller.set_color(0, 255, 0, 0.0, True)


NUM_CYCLES = 6
HEIGHT = 1.0
X = -0.25

def run_sequence(scf, params):

    cf = scf.cf
    delay = params['delay']
    y = params['y']

    # Takeoff
    cf.high_level_commander.takeoff(HEIGHT, 4.5)
    time.sleep(4.75)

    # Initial Position
    cf.high_level_commander.go_to(X, y, HEIGHT, 0.0, 2.0)
    time.sleep(2.0)
    scf.cf.light_controller.set_color(0, 127, 255, 0.0, True)

    # Delay
    time.sleep(delay)

    # Wave Sequence
    for i in range(0, NUM_CYCLES):
        # Step 1 forward
        cf.high_level_commander.go_to(X + 0.5, y, HEIGHT, 0.0, 1.25)
        time.sleep(1.25)

        # Step 2 backward
        cf.high_level_commander.go_to(X - 0.5, y, HEIGHT, 0.0, 1.25)
        time.sleep(1.25)

    # Final Position
    cf.high_level_commander.go_to(X, y, HEIGHT, 0.0, 1.25)
    time.sleep(1.25)

    # Landing
    cf.high_level_commander.land(0.0, 4.5)
    time.sleep(4.75)

    # Stop
    cf.high_level_commander.stop()


if __name__ == '__main__':
    cflib.crtp.init_drivers()

    factory = CachedCfFactory(rw_cache='./cache')
    with Swarm(uris, factory=factory) as swarm:
        swarm.parallel(reset_estimator)
        time.sleep(5.0)
        swarm.parallel(run_sequence, args_dict=params)
