import sys 

from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *
from PyQt5.QtSvg import *


class FileMenu(QMenuBar):

    fileMenu = None
    manager = None

    def __init__(self, manager, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.manager = manager
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
        print("----- TEST ------")


    def exit(self):
        sys.exit()
