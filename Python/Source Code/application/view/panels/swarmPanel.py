from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

class SwarmPanel(QFrame):
    
    def __init__(self, manager, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.manager = manager
