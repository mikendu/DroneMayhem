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

        self.initializeController()
        menuBar = FileMenu(self.manager, self)
        dashboard = MainDashboard(self.manager)
        statusBar = QStatusBar()
        statusBar.messageChanged.connect(self.onStatusMessageChange)
        
        self.setMenuBar(menuBar)
        self.setCentralWidget(dashboard)
        self.setStatusBar(statusBar)
        self.setupWindow()


    def onStatusMessageChange(self, message):
        self.statusBar().setProperty("class", [])
        self.statusBar().style().polish(self.statusBar())

    def showStatusMessage(self, message, timeout = 3000):
        self.statusBar().showMessage(message, timeout)
        self.statusBar().setProperty("class", "highlighted")
        self.statusBar().style().polish(self.statusBar())

    def initializeController(self):
        appSettings = AppSettings()
        self.manager = ApplicationManager(appSettings)


    def setupWindow(self):       
        self.setMinimumSize(1600, 900)
        settings = self.manager.appSettings     
        if not settings.getValue("windowGeometry") == None and not RESET_WINDOW_GEOMETRY: 
            self.restoreGeometry(settings.getValue("windowGeometry"))
        else:
            self.resize(1920, 1080)

    def closeEvent(self, event):
        settings = self.manager.appSettings
        settings.setValue("windowGeometry", self.saveGeometry())
        super().closeEvent(event)
        


app = QApplication(sys.argv)
app.setStyleSheet(open('./application/css/main.css').read())

window = MainWindow()
window.show()


app.exec_()