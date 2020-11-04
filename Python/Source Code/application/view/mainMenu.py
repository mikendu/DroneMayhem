import sys 
from os import path
from pathlib import Path

from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

from util import *

class FileMenu(QMenuBar):

    def __init__(self, appController, mainWindow, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.mainWindow = mainWindow
        self.appController = appController
        self.fileMenu = None
        self.setupMenus()
        

    def setupMenus(self):
        quitAction = QAction("&Exit", self)
        quitAction.setShortcut("Ctrl+Q")
        quitAction.setStatusTip("Exit the application")
        quitAction.triggered.connect(self.exit)

        openAction = QAction("&Open Sequence", self)
        openAction.setShortcut("Ctrl+O")
        openAction.setStatusTip("Open a sequence file")
        openAction.triggered.connect(self.openSequence)

        self.fileMenu = self.addMenu("&File")
        self.fileMenu.addAction(openAction)
        self.fileMenu.addSeparator()
        self.fileMenu.addAction(quitAction)

    def openSequence(self):
        if self.appController.sequencePlaying:
            self.mainWindow.showStatusMessage("Cannot open file while a sequence is playing.")
            return

        dialogLocation = './'
        savedLocation = self.appController.appSettings.getValue("openFileLocation", None, str)
        if savedLocation is not None and path.exists(savedLocation):
            dialogLocation = savedLocation

        fileInfo = QFileDialog.getOpenFileName(self, 'Open Sequence File', dialogLocation, "Sequence files (*.json)")
        selectedFile = fileInfo[0]

        if selectedFile:
            pathObject = Path(selectedFile)
            containingDirectory = pathObject.parent
            self.appController.appSettings.setValue("openFileLocation", str(containingDirectory))
            if self.appController.openSequence(selectedFile):
                pass
            else:
                #self.mainWindow.showStatusMessage("ERROR - Could not load sequence file!")
                showDialog("Invalid Sequence", "ERROR - Could not load sequence file!", None, True, True)



    def exit(self):
        self.mainWindow.close()
