import os
import struct

import cflib.crtp

from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from cflib.crtp.radiodriver import RadioDriver
from cflib.crtp.crtpstack import CRTPPacket
from cflib.crtp.crtpstack import CRTPPort
from cflib.crazyflie import Crazyflie
from cflib.crazyflie import HighLevelCommander
from cflib.crazyflie.mem import LighthouseBsGeometry
from cflib.crazyflie.mem import LighthouseMemHelper
from cflib.crazyflie.mem import MemoryElement
from cflib.crazyflie.mem import Poly4D
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.syncLogger import SyncLogger

from threading import Event


import logging
import time
import signal
import sys
import openvr
import math
import numpy as np



def signal_handler(sig, frame):
    print('Interrupt signal received, exiting!')
    sys.exit(0)



os.environ["USE_CFLINK"] = "cpp"
# if 'USE_CFLINK' in os.environ:
#     del os.environ['USE_CFLINK']

uri = "radio://*/80/2M/E7E7E7E701"
# uri = "radio://0/1/2M/E7E7E7E701"
cflib.crtp.init_drivers()


with SyncCrazyflie( "radio://*/80/2M/E7E7E7E701", cf=Crazyflie(ro_cache='./cache',rw_cache='./cache')) as scf:
    cf = scf.cf
    cf.param.set_value('lighthouse.method', '0')
    cf.param.set_value('lighthouse.systemType', '2')
    cf.param.set_value('stabilizer.controller', '1')
    cf.param.set_value('stabilizer.estimator', '2')
    cf.param.set_value('commander.enHighLevel', '1')
    time.sleep(0.5)

    commander = cf.high_level_commander

    print('Taking off...')
    commander.takeoff(0.5, 1.5)
    time.sleep(1.5)

    commander.go_to(0, 0.5, 0.5, 0, 1.0)
    time.sleep(1.0)

    commander.go_to(0, -0.5, 0.5, 0, 1.0)
    time.sleep(1.0)

    commander.go_to(0, 0.0, 0.5, 0, 1.0)
    time.sleep(1.0)

    print('Landing')
    commander.land(0.025, 1.5)
    time.sleep(1.5)
    commander.stop()

    print('Sequence done!!')