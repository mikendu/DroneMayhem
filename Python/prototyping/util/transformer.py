
from threading import Event

from PyQt5.QtWidgets import QFrame, QPushButton, QLabel, QSizePolicy
from PyQt5.QtCore import Qt

from cflib.localization import LighthouseBsGeoEstimator
from cflib.localization import LighthouseSweepAngleAverageReader

sweep_angles = None

def estimate_base_stations(cf):
    collect_angles(cf)

    geometries = {}
    estimator = LighthouseBsGeoEstimator()
    for id in sorted(sweep_angles.keys()):
        average_data = sweep_angles[id]
        sensor_data = average_data[1]
        rotation_bs_matrix, position_bs_vector = estimator.estimate_geometry(sensor_data)
        is_valid = estimator.sanity_check_result(position_bs_vector)

        if is_valid:
            geometries[id] = (rotation_bs_matrix, position_bs_vector)

    return geometries

def collect_angles(cf):
    global sweep_angles
    event = Event()
    sweep_angle_reader = LighthouseSweepAngleAverageReader(cf, lambda angles: collection_finished(event, angles))
    sweep_angle_reader.start_angle_collection()
    event.wait()

def collection_finished(event, angles):
    global sweep_angles
    sweep_angles = angles
    event.set()