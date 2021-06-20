from cflib.crazyflie.mem import LighthouseBsCalibration

# Default Calibration Values

CalibrationOne = LighthouseBsCalibration()
CalibrationOne.sweeps[0].phase = -0.0247802734375
CalibrationOne.sweeps[0].tilt = 0.0080108642578125
CalibrationOne.sweeps[0].curve = -0.0009412765502929688
CalibrationOne.sweeps[0].gibmag = -0.00315093994140625
CalibrationOne.sweeps[0].gibphase = -5.6796875
CalibrationOne.sweeps[0].ogeemag = 0.0
CalibrationOne.sweeps[0].ogeephase = 0.0
CalibrationOne.sweeps[1].phase = -0.03021240234375
CalibrationOne.sweeps[1].tilt = 0.005035400390625
CalibrationOne.sweeps[1].curve = -0.0046539306640625
CalibrationOne.sweeps[1].gibmag = 0.005664825439453125
CalibrationOne.sweeps[1].gibphase = -3.54296875
CalibrationOne.sweeps[1].ogeemag = 0.0
CalibrationOne.sweeps[1].ogeephase = 0.0
CalibrationOne.uid = 0xE8F8B934
CalibrationOne.valid = True

CalibrationTwo = LighthouseBsCalibration()
CalibrationTwo.sweeps[0].phase = 0.005596160888671875
CalibrationTwo.sweeps[0].tilt = 0.0167694091796875
CalibrationTwo.sweeps[0].curve = -0.005855560302734375
CalibrationTwo.sweeps[0].gibmag = -0.01409912109375
CalibrationTwo.sweeps[0].gibphase = 1.3173828125
CalibrationTwo.sweeps[0].ogeemag = 0.0
CalibrationTwo.sweeps[0].ogeephase = 0.0
CalibrationTwo.sweeps[1].phase = 0.0172882080078125
CalibrationTwo.sweeps[1].tilt = 0.01342010498046875
CalibrationTwo.sweeps[1].curve = -0.00334930419921875
CalibrationTwo.sweeps[1].gibmag = 0.01078033447265625
CalibrationTwo.sweeps[1].gibphase = 1.720703125
CalibrationTwo.sweeps[1].ogeemag = 0.0
CalibrationTwo.sweeps[1].ogeephase = 0.0
CalibrationTwo.uid = 0x32EF5DFB
CalibrationTwo.valid = True


CALIBRATION_DATA = { 0: CalibrationOne, 1: CalibrationTwo }