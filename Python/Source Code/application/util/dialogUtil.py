from PyQt5.QtWidgets import *
from PyQt5.QtGui import *
from PyQt5.QtCore import *


def showDialog(title, message, parentWidget = None, modal = True, showButton = True):

    dialog = QMessageBox(
        QMessageBox.NoIcon, title, message,
        buttons = QMessageBox.NoButton,
        parent = parentWidget)

    if (modal):
        dialog.setWindowIcon(QIcon(':/images/window_icon.png'))    
        dialog.exec()
    else:
        if not showButton:
            dialog.setStandardButtons(QMessageBox.NoButton)
            
        dialog.setWindowModality(Qt.NonModal)
        dialog.show()
        return dialog
    