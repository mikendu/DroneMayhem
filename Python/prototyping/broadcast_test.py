import time
import os

import cflib.crtp
from cflib.crazyflie.broadcaster import Broadcaster
from cflib.crazyflie.utils import LedUtils
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie import Crazyflie
from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from cflib.crazyflie import State
from cflib.crazyflie.mem import MemoryElement

def get_color(r, g, b):
    return (int(r) << 16) | (int(g) << 8) | int(b)

def setLED(cf, r, g, b, time):
    color = get_color(r, g, b)
    cf.param.set_value('ring.fadeTime', str(time))
    cf.param.set_value('ring.fadeColor', str(color))
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





os.environ["USE_CFLINK"] = "cpp"
cflib.crtp.init_drivers()

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
"""

os.environ["USE_CFLINK"] = "cpp"
cflib.crtp.init_drivers()


with SyncCrazyflie("radio://*/55/2M/E7E7E7E7E7?safelink=1&autoping=1", cf=Crazyflie(rw_cache='../cache')) as scf:
    cf = scf.cf

    # Get LED memory and write to it
    mems = cf.mem.get_mems(MemoryElement.TYPE_DRIVER_LEDTIMING)
    if len(mems) > 0:
        mem = mems[0]
        mem.add(10.0, r=255, g=0, b=0)
        mem.add(1.0, r=255, g=255, b=0)
        mem.add(1.0, r=255, g=255, b=255)
        mem.write_data(None)
    else:
        print('No LED ring present')

    # Set virtual mem effect effect
    cf.param.set_value('ring.effect', '0')
    time.sleep(0.5)
    cf.param.set_value('ring.effect', '17')
    time.sleep(5)
