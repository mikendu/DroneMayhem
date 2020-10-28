import sys
from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from view import *
from model import *
from controllers import *

RESET_WINDOW_GEOMETRY = False

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
        self.setupWindow()

    def initializeControllers(self):
        sequenceController = SequenceController()
        appSettings = AppSettings()
        self.manager = SwarmManager(sequenceController, appSettings)


    def setupWindow(self):       
        settings = self.manager.appSettings.settings       
        if not settings.value("windowGeometry") == None and not RESET_WINDOW_GEOMETRY: 
            self.restoreGeometry(settings.value("windowGeometry"))
        else:
            self.resize(1920, 1080)

    def closeEvent(self, event):
        settings = self.manager.appSettings.settings
        settings.setValue("windowGeometry", self.saveGeometry())
        super().closeEvent(event)
        


app = QApplication(sys.argv)
app.setStyleSheet(open('./application/css/main.css').read())

window = MainWindow()
window.show()


app.exec_()