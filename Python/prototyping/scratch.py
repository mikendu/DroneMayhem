import struct
import time
from enum import Enum

from cflib.crtp.cflinkcppdriver import CfLinkCppDriver
from cflib.crtp.radiodriver import Crazyradio
from cflib.crtp.crtpstack import CRTPPacket
from cflib.crtp.crtpstack import CRTPPort
from cflib.crazyflie import HighLevelCommander
from cflib.crazyflie.param import WRITE_CHANNEL

class RingEffect(Enum):
    OFF = 0
    FADE_EFFECT = 14
    TIMING_EFFECT = 17

# Hard coding TOC param ids for now, based on the current firmware
class ParameterID(Enum):
    RING_EFFECT = 181       # uint8_t, <B
    FADE_COLOR = 190        # uint32_t, <L
    FADE_TIME = 191         # float, <f

class LightController:
    def __init__(self, cf):
        self.cf = cf

    def set_effect(self, effect):
        if not isinstance(effect, RingEffect):
            raise ValueError("Invalid effect given: " + str(effect))

        packet = self._effect_change(effect.value)
        self.cf.send_packet(packet)

    def set_color(self, r, g, b, time = 0.0):
        color = (int(r) << 16) | (int(g) << 8) | int(b)
        color_packet = self._fade_color(color)
        time_packet = self._fade_time(time)
        self.cf.send_packet(color_packet)
        self.cf.send_packet(time_packet)

    def _fade_time(self, duration):
        packet = CRTPPacket()
        packet.set_header(CRTPPort.PARAM, WRITE_CHANNEL)
        packet.data = struct.pack('<H', int(ParameterID.FADE_TIME.value))
        packet.data += struct.pack('<f', float(duration))
        return packet

    def _fade_color(self, color):
        packet = CRTPPacket()
        packet.set_header(CRTPPort.PARAM, WRITE_CHANNEL)
        packet.data = struct.pack('<H', int(ParameterID.FADE_COLOR.value))
        packet.data += struct.pack('<L', int(color))
        return packet

    def _effect_change(self, effect):
        packet = CRTPPacket()
        packet.set_header(CRTPPort.PARAM, WRITE_CHANNEL)
        packet.data = struct.pack('<H', int(ParameterID.RING_EFFECT.value))
        packet.data += struct.pack('<B', int(effect))
        return packet

class Broadcaster():

    def __init__(self, channel, datarate = Crazyradio.DR_2MPS):
        self._validate_channel(channel)
        self._validate_datarate(datarate)

        self._uri = self._construct_uri(channel, datarate)
        self._link = CfLinkCppDriver()
        self._is_link_open = False

        self.high_level_commander = HighLevelCommander(self)
        self.light_controller = LightController(self)

    def open_link(self):
        if (self.is_link_open()):
            raise Exception('Link already open')

        print('Connecting to %s' % self._uri)
        self._link.connect(self._uri, None, None)
        self._is_link_open = True

    def close_link(self):
        if (self.is_link_open()):
            self._link.close()
            self._is_link_open = False

    def __enter__(self):
        self.open_link()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close_link()

    def is_link_open(self):
        return self._is_link_open

    def send_packet(self, packet):
        if not self.is_link_open():
            raise Exception('Link is not open')

        self._link.send_packet(packet)

    def __str__(self):
        return "BroadcastLink <" + str(self._uri)  + ">"

    def _construct_uri(self, channel, datarate):
        return "radiobroadcast://*/" + str(channel) + "/" + self._get_data_rate_string(datarate)

    def _validate_channel(self, channel):
        if channel and (isinstance(channel, int) or channel.is_integer()):
            if channel >= 0 and channel <= 127:
                return
        raise ValueError("Invalid channel: " + str(channel))

    def _validate_datarate(self, datarate):
        if not(datarate == Crazyradio.DR_250KPS or \
            datarate == Crazyradio.DR_1MPS or \
            datarate == Crazyradio.DR_2MPS):
            raise ValueError("Invalid data rate: " + str(datarate))

    def _get_data_rate_string(self, datarate):
        if datarate == Crazyradio.DR_250KPS:
            return '250K'
        elif datarate == Crazyradio.DR_1MPS:
            return '1M'
        elif datarate == Crazyradio.DR_2MPS:
            return '2M'


link = Broadcaster(55)
link.open_link()

link.light_controller.set_color(255, 0, 0, 0.5)
time.sleep(0.5)
link.light_controller.set_color(0, 0, 0, 0.5)
time.sleep(0.5)

link.light_controller.set_color(0, 255, 0, 0.5)
time.sleep(0.5)
link.light_controller.set_color(0, 0, 0, 0.5)
time.sleep(0.5)

link.light_controller.set_color(0, 0, 255, 0.5)
time.sleep(0.5)
link.light_controller.set_color(0, 0, 0, 0.5)
time.sleep(1.0)

print("Taking off...")
link.high_level_commander.takeoff(0.5, 3.0)
time.sleep(3.2)

print("Landing...")
link.high_level_commander.land(0.05, 3.0)
time.sleep(3.2)

link.high_level_commander.stop()
time.sleep(1.0)

link.close_link()
