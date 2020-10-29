from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from util import *
from ..widgets import *

class SequencePanel(QFrame):
    
    
    def __init__(self, appController, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.appController = appController

        self.layout = createLayout(LayoutType.VERTICAL, self)
        #self.layout.addWidget(self.createTitle())    

    def createTitle(self):
        title = QLabel("SequenceRunner")
        title.setProperty("class", "titleText")
        return title