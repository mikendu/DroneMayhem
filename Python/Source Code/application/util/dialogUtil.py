from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *

def showDialog(title, message):
    errorDialog = QErrorMessage()
    errorDialog.setModal(True)
    errorDialog.showMessage(message)
    errorDialog.exec()