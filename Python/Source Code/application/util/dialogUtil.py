from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *


def showDialog(parentWidget, title, message):
    dialog = QMessageBox(
        QMessageBox.NoIcon, title, message,
        buttons = QMessageBox.NoButton,
        parent = parentWidget)
    dialog.setStandardButtons(QMessageBox.NoButton)
    dialog.setWindowModality(Qt.NonModal)
    dialog.show()
    return dialog
    