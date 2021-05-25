from cflib.crazyflie.mem import LighthouseBsCalibration

CalibrationOne = LighthouseBsCalibration()
CalibrationOne.sweeps[0].phase = 1.0
CalibrationOne.sweeps[0].tilt = 2.0
CalibrationOne.sweeps[0].curve = 3.0
CalibrationOne.sweeps[0].gibmag = 4.0
CalibrationOne.sweeps[0].gibphase = 5.0
CalibrationOne.sweeps[0].ogeemag = 6.0
CalibrationOne.sweeps[0].ogeephase = 7.0
CalibrationOne.sweeps[1].phase = 1.1
CalibrationOne.sweeps[1].tilt = 2.1
CalibrationOne.sweeps[1].curve = 3.1
CalibrationOne.sweeps[1].gibmag = 4.1
CalibrationOne.sweeps[1].gibphase = 5.1
CalibrationOne.sweeps[1].ogeemag = 6.1
CalibrationOne.sweeps[1].ogeephase = 7.1
CalibrationOne.uid = 1234
CalibrationOne.valid = True

CalibrationTwo = LighthouseBsCalibration()
CalibrationTwo.sweeps[0].phase = 1.5
CalibrationTwo.sweeps[0].tilt = 2.5
CalibrationTwo.sweeps[0].curve = 3.5
CalibrationTwo.sweeps[0].gibmag = 4.5
CalibrationTwo.sweeps[0].gibphase = 5.5
CalibrationTwo.sweeps[0].ogeemag = 6.5
CalibrationTwo.sweeps[0].ogeephase = 7.5
CalibrationTwo.sweeps[1].phase = 1.51
CalibrationTwo.sweeps[1].tilt = 2.51
CalibrationTwo.sweeps[1].curve = 3.51
CalibrationTwo.sweeps[1].gibmag = 4.51
CalibrationTwo.sweeps[1].gibphase = 5.51
CalibrationTwo.sweeps[1].ogeemag = 6.51
CalibrationTwo.sweeps[1].ogeephase = 7.51
CalibrationTwo.uid = 9876
CalibrationTwo.valid = True


CALIBRATION_DATA = { 0: CalibrationOne, 1: CalibrationTwo }