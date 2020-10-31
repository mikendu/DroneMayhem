import sys
from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

import app_resources
from view import *
from model import *
from controllers import *

from util.threadUtil import *
from util.dialogUtil import *

RESET_WINDOW_GEOMETRY = False

# Subclass QMainWindow to customise your application's main window
class MainWindow(QMainWindow):

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

        self.setWindowTitle("Swarm Controller")  
        self.setWindowIcon(QIcon(':/images/window_icon.png'))       

        self.initializeController()
        menuBar = FileMenu(self.appController, self)
        dashboard = MainDashboard(self.appController)
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
        self.appController = ApplicationController(self, appSettings)


    def setupWindow(self):       
        self.setMinimumSize(1600, 900)
        settings = self.appController.appSettings     
        if not settings.getValue("windowGeometry") == None and not RESET_WINDOW_GEOMETRY: 
            self.restoreGeometry(settings.getValue("windowGeometry"))
        else:
            self.resize(1920, 1080)

        

    def closeEvent(self, event):
        event.ignore()
        if self.appController.sequencePlaying:
            self.showStatusMessage("Please stop sequence before exiting!")
        
        else:
            showDialog(self, " ", "Cleaning up & exiting...")
            QTimer.singleShot(1, self.cleanupAndExit)

    def cleanupAndExit(self):
        self.appController.cleanup()
        settings = self.appController.appSettings
        settings.setValue("windowGeometry", self.saveGeometry())
        QThreadPool.globalInstance().waitForDone()
        self.teardownFinished = True
        sys.exit(0)


app = QApplication(sys.argv)

styleSheet = QFile(":/stylesheets/main.css")
styleSheet.open(QFile.ReadOnly | QFile.Text)
styleContent = QTextStream(styleSheet).readAll()

app.setStyleSheet(styleContent)
app.setAttribute(Qt.AA_DisableWindowContextHelpButton)

window = MainWindow()
window.show()

app.exec_()