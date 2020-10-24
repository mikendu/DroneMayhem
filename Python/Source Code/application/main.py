import sys
from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from view import *
from model import *
from controllers import *

# Subclass QMainWindow to customise your application's main window
class MainWindow(QMainWindow):

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.setWindowTitle("Swarm Controller")  
        self.setWindowIcon(QIcon('./application/resources/images/icon_black.png'))       

        self.initializeControllers()
        menuBar = FileMenu(self.manager)
        dashboard = MainDashboard(self.manager)
        statusBar = QStatusBar()
        
        self.setMenuBar(menuBar)
        self.setCentralWidget(dashboard)
        self.setStatusBar(statusBar)

    def initializeControllers(self):
        sequenceController = SequenceController()
        appSettings = AppSettings()
        print("Sequences: ", appSettings.getSequences())
        self.manager = SwarmManager(sequenceController, appSettings)
        


app = QApplication(sys.argv)
app.setStyleSheet(open('./application/css/main.css').read())

window = MainWindow()
window.resize(1920, 1080)
window.show()
app.exec_()