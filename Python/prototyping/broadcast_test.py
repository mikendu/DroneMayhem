import time
import os
import json
import struct

import cflib.crtp
from cflib.crazyflie.broadcaster import Broadcaster
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie import Crazyflie
from cflib.crtp.crtpstack import CRTPPacket, CRTPPort
from cflib.crazyflie.param import WRITE_CHANNEL
from cflib.crazyflie.light_controller import RingEffect
from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from cflib.crazyflie import State
from cflib.crazyflie.mem import MemoryElement

"""
broadcaster = Broadcaster("radio://*/55/2M/broadcast")
broadcaster.open_link()

print("Taking off...")
commander = broadcaster.high_level_commander
commander.takeoff(0.5, 3.0)
time.sleep(0.25)

print("Landing...")
commander.takeoff(0.0, 3.0)
time.sleep(0.25)

print("Stopping...")
commander.stop()
time.sleep(1.0)

broadcaster.close_link()
print("Done!")



unicast = CfLinkCppDriver()
unicast.connect('radio://*/55/2M/E7E7E7E7E7?safelink=1&autoping=1', None, None)
time.sleep(1.0)

unicast.send_packet(LedUtils.ring_effect(14))
time.sleep(1.0)

unicast.send_packet(LedUtils.fade_time(0.1))
unicast.send_packet(LedUtils.fade_color(get_color(255, 0, 0)))
time.sleep(1.0)

unicast.send_packet(LedUtils.fade_time(0.1))
unicast.send_packet(LedUtils.fade_color(get_color(0, 255, 0)))
time.sleep(1.0)

unicast.send_packet(LedUtils.fade_time(0.1))
unicast.send_packet(LedUtils.fade_color(get_color(0, 0, 255)))
time.sleep(1.0)

unicast.send_packet(LedUtils.fade_time(0.1))
unicast.send_packet(LedUtils.fade_color(get_color(255, 255, 255)))
time.sleep(1.0)

unicast.send_packet(LedUtils.fade_time(0.1))
unicast.send_packet(LedUtils.fade_color(get_color(0, 0, 0)))
time.sleep(1.0)

unicast.close()






with SyncCrazyflie("radio://*/55/2M/E7E7E7E7E7?safelink=1&autoping=1") as scf:
    while scf.cf.state != State.SETUP_FINISHED:
        time.sleep(0.1)

with SyncCrazyflie("radio://*/55/2M/E7E7E7E7E8?safelink=1&autoping=1") as scf:
    time.sleep(2.5)



broadcaster = Broadcaster("radio://*/55/2M/broadcast")
broadcaster.open_link()
time.sleep(1.0)

broadcaster.send_packet(LedUtils.ring_effect(14))
time.sleep(1.0)


broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(255, 0, 0)))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(0, 255, 0)))
time.sleep(1.0)


broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(0, 0, 255)))
time.sleep(1.0)


broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(255, 255, 255)))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(0, 0, 0)))
time.sleep(1.0)

broadcaster.close_link()
print("Done!")


cf = scf.cf
cf.send_packet(LedUtils.ring_effect(14))
time.sleep(1.0)

cf.send_packet(LedUtils.fade_time(0.1))
cf.send_packet(LedUtils.fade_color(get_color(255, 0, 0)))
time.sleep(1.0)

cf.send_packet(LedUtils.fade_time(0.1))
cf.send_packet(LedUtils.fade_color(get_color(0, 255, 0)))
time.sleep(1.0)

cf.send_packet(LedUtils.fade_time(0.1))
cf.send_packet(LedUtils.fade_color(get_color(0, 0, 255)))
time.sleep(1.0)

cf.send_packet(LedUtils.fade_time(0.1))
cf.send_packet(LedUtils.fade_color(get_color(255, 255, 255)))
time.sleep(1.0)

cf.send_packet(LedUtils.fade_time(0.1))
cf.send_packet(LedUtils.fade_color(get_color(0, 0, 0)))
time.sleep(1.0)


cf = scf.cf
print("Crazyflie state: ", str(cf.state))
time.sleep(1.0)

cf.param.set_value('ring.effect', '14')

setLED(cf, 255, 255, 255, 0.1)
time.sleep(1.0)

setLED(cf, 255, 0, 0, 0.1)
time.sleep(1.0)


setLED(cf, 0, 255, 0, 0.1)
time.sleep(1.0)


setLED(cf, 0, 0, 255, 0.1)
time.sleep(1.0)

setLED(cf, 0, 0, 0, 0.1)
time.sleep(1.0)

commander = cf.high_level_commander

cf.param.set_value('lighthouse.method', '0')
cf.param.set_value('stabilizer.controller', '2') # Mellinger controller
cf.param.set_value('commander.enHighLevel', '1')
time.sleep(1.0)

print("Taking off...")
commander.takeoff(0.5, 3.0)
time.sleep(0.25)

print("Landing...")
commander.takeoff(0.0, 3.0)
time.sleep(0.25)

print("Stopping...")
commander.stop()
time.sleep(1.0)

    print("Done!")

os.environ["USE_CFLINK"] = "cpp"
cflib.crtp.init_drivers()

with SyncCrazyflie("radio://*/55/2M/E7E7E7E7E7?safelink=1&autoping=1") as scf:
    while scf.cf.state != State.SETUP_FINISHED:
        time.sleep(0.1)

    print("Auto pinging")
    time.sleep(1.0)

    print("Disabling auto ping")
    scf.cf.auto_ping = False
    time.sleep(1.0)

print("Running color sequence")
broadcaster = Broadcaster("radio://*/55/2M/broadcast")
broadcaster.open_link()
time.sleep(1.0)

broadcaster.send_packet(LedUtils.ring_effect(14))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(255, 0, 0)))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(0, 255, 0)))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(0, 0, 255)))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(255, 255, 255)))
time.sleep(1.0)

broadcaster.send_packet(LedUtils.fade_time(0.1))
broadcaster.send_packet(LedUtils.fade_color(get_color(0, 0, 0)))
time.sleep(1.0)

broadcaster.close_link()
print("Done!")



possible = [
    'radio://*/55/2M/E7E7E7E7E7',
    'radio://*/55/2M/E7E7E7E7E8'
]
print("Possible:", possible)
uris = CfLinkCppDriver.scan_selected(possible)
print("Found:", uris)


broadcaster = Broadcaster(55)
broadcaster.open_link()

print("Taking off...")
commander = broadcaster.high_level_commander
commander.takeoff(0.5, 3.0)
time.sleep(0.25)

print("Landing...")
commander.takeoff(0.0, 3.0)
time.sleep(0.25)

print("Stopping...")
commander.stop()
time.sleep(1.0)

broadcaster.close_link()
print("Done!")


"""
#
# ITERATION_COUNT = 10
# fadeTime = 0.25
# sleepTime = 0.35
# pauseTime = 1.0
#
# os.environ["USE_CFLINK"] = "cpp"
# cflib.crtp.init_drivers()
#
# link = Broadcaster(55)
# link.open_link()
# time.sleep(2.0)
#
# def setParam(cf, paramId, paramValue):
#     packet = CRTPPacket()
#     packet.set_header(CRTPPort.PARAM, WRITE_CHANNEL)
#     packet.data = struct.pack('<H', int(paramId))
#     packet.data += struct.pack('<L', int(paramValue))
#     cf.send_packet(packet)


# with SyncCrazyflie("radio://*/55/2M/E7E7E7E701?safelink=1&autoping=1", cf=Crazyflie(ro_cache='../cache', rw_cache='../cache')) as scf:
    # time.sleep(1.25)
    # scf.cf.param.set_value('commander.enHighLevel', '1')
    # time.sleep(1.25)
    # scf.cf.param.set_value('ring.effect', 14)
    # scf.cf.param.set_value('ring.fadeTime', 1.0)
    # scf.cf.param.set_value('ring.fadeColor', 0)

    # time.sleep(1.0)
    # controller = scf.cf.light_controller
    # scf.cf.auto_ping = False
    # time.sleep(3.0)
    # print("\n\n\n")

# for i in range(0, ITERATION_COUNT):
#     # scf.cf.param.set_value('ring.effect', 2)
#     setParam(link, 180, 2)
#     time.sleep(1.25)
#     # scf.cf.param.set_value('ring.effect', 7)
#     setParam(link, 180, 7)
#     time.sleep(1.25)
#
#     for i in range(0, ITERATION_COUNT):
#         controller.set_effect(RingEffect.OFF)
#         time.sleep(0.5)
#
#         controller.set_effect(RingEffect.FADE_EFFECT)
#         controller.set_color(255, 0, 0, fadeTime)
#         time.sleep(sleepTime)
#         controller.set_color(0, 0, 0, fadeTime)
#         time.sleep(sleepTime)
#
#         controller.set_color(0, 255, 0, fadeTime)
#         time.sleep(sleepTime)
#         controller.set_color(0, 0, 0, fadeTime)
#         time.sleep(sleepTime)
#
#         controller.set_color(0, 0, 255, fadeTime)
#         time.sleep(sleepTime)
#         controller.set_color(0, 0, 0, fadeTime)
#
#         time.sleep(sleepTime + pauseTime)
#
#     controller.set_color(255, 255, 255, 0.15)
#     time.sleep(0.25)
#     controller.set_color(0, 0, 0, 0.15)
#     time.sleep(0.25)
#
#     controller.set_color(255, 255, 255, 0.15)
#     time.sleep(0.25)
#     controller.set_color(0, 0, 0, 0.15)
#     time.sleep(0.25)
#     time.sleep(1.5)
#
# with SyncCrazyflie("radio://*/55/2M/E7E7E7E701?safelink=1&autoping=1", cf=Crazyflie(ro_cache='../cache', rw_cache='../cache')) as scf:
#     time.sleep(1.0)
#     time.sleep(0.5)
#     controller = link.light_controller
#     controller.set_effect(RingEffect.FADE_EFFECT)
#     time.sleep(0.5)
#
#     commander = link.high_level_commander
#     for i in range(0, ITERATION_COUNT):
#
#         controller.set_color(255, 0, 0, fadeTime)
#         time.sleep(sleepTime)
#         controller.set_color(0, 0, 0, fadeTime)
#         time.sleep(sleepTime)
#
#         controller.set_color(0, 255, 0, fadeTime)
#         time.sleep(sleepTime)
#         controller.set_color(0, 0, 0, fadeTime)
#         time.sleep(sleepTime)
#
#         controller.set_color(0, 0, 255, fadeTime)
#         time.sleep(sleepTime)
#         controller.set_color(0, 0, 0, fadeTime)
#
#         time.sleep(sleepTime + pauseTime)
#         print("Taking off...")
#         commander.takeoff(0.1, 3.0)
#         time.sleep(0.2)
#
#         print("Stopping...")
#         commander.stop()
#
#
# link.close_link()


#
# for i in range(0, 3):
#     controller = link.light_controller
#     controller.set_effect(RingEffect.OFF)
#     time.sleep(0.5)
#
#     fadeTime = 0.5
#     controller.set_effect(RingEffect.FADE_EFFECT)
#     controller.set_color(255, 0, 0, fadeTime)
#     time.sleep(1.0)
#     controller.set_color(0, 0, 0, fadeTime)
#     time.sleep(1.0)
#
#     controller.set_color(0, 255, 0, fadeTime)
#     time.sleep(1.0)
#     controller.set_color(0, 0, 0, fadeTime)
#     time.sleep(1.0)
#
#     controller.set_color(0, 0, 255, fadeTime)
#     time.sleep(1.0)
#     controller.set_color(0, 0, 0, fadeTime)
#     time.sleep(3.0)
#




color_data = None
with open('../../Sequences/Straight Line.json') as jsonFile:
    seq_data = json.load(jsonFile)
    color_data = seq_data['Tracks'][0]['LedTimings']

def get_color(r, g, b):
    return (int(r) << 16) | (int(g) << 8) | int(b)

def write_finished(*args):
    print("Args: ", args)
    print("\n\n------- WRITE FINISHED --------\n\n")


os.environ["USE_CFLINK"] = "cpp"
cflib.crtp.init_drivers()

with SyncCrazyflie("radio://*/55/2M/E7E7E7E701?safelink=1&autoping=1", cf=Crazyflie(ro_cache='../cache', rw_cache='../cache')) as scf:
    cf = scf.cf

    time.sleep(0.5)
    cf.light_controller.set_effect(RingEffect.OFF)
    time.sleep(0.5)

    fadeTime = 0.25
    sleepTime = 0.5
    cf.light_controller.set_effect(RingEffect.FADE_EFFECT)
    cf.light_controller.set_color(255, 0, 0, fadeTime)
    time.sleep(sleepTime)
    cf.light_controller.set_color(0, 0, 0, fadeTime)
    time.sleep(sleepTime)

    cf.light_controller.set_color(0, 255, 0, fadeTime)
    time.sleep(sleepTime)
    cf.light_controller.set_color(0, 0, 0, fadeTime)
    time.sleep(sleepTime)

    cf.light_controller.set_color(0, 0, 255, fadeTime)
    time.sleep(sleepTime)
    cf.light_controller.set_color(0, 0, 0, fadeTime)
    time.sleep(sleepTime)

    # Get LED memory and write to it
    mems = cf.mem.get_mems(MemoryElement.TYPE_DRIVER_LEDTIMING)
    if len(mems) > 0:
        mem = mems[0]

        # mem.add(0.0, r=255, g=0, b=0)
        # mem.add(1.0, r=255, g=0, b=0)
        # mem.add(0.01, r=0, g=0, b=0)
        # mem.add(0.25, r=0, g=0, b=0)
        # mem.add(0.0, r=255, g=255, b=0)
        # mem.add(1.0, r=255, g=255, b=0)
        # mem.add(0.01, r=0, g=0, b=0)
        # mem.add(0.25, r=0, g=0, b=0)
        # mem.add(0.0, r=255, g=255, b=255)
        # mem.add(1.0, r=255, g=255, b=255)
        # mem.write_data(None)

        mem.write_raw(bytearray(color_data), write_finished)
    else:
        print('No LED ring present')

    # Set virtual mem effect effect
    # cf.param.set_value('ring.effect', '0')
    # time.sleep(0.5)
    # cf.param.set_value('ring.effect', '17')
    # time.sleep(9)


    cf.light_controller.set_effect(RingEffect.OFF)
    time.sleep(0.5)
    cf.light_controller.set_effect(RingEffect.TIMING_EFFECT)
    time.sleep(10.0)
