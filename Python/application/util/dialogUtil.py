from PyQt5.QtWidgets import QMessageBox
from PyQt5.QtGui import QIcon
from PyQt5.QtCore import Qt


def modalDialog(title, message,):
    dialog = QMessageBox(QMessageBox.NoIcon, title, message)
    dialog.setWindowIcon(QIcon(':/images/window_icon.png'))
    dialog.exec()


def nonModalDialog(title, message, parentWidget, showButton=True):
    if not parentWidget:
        raise ValueError("Parent widget cannot be null for non-modal dialog")

    dialog = QMessageBox(QMessageBox.NoIcon, title, message, parent=parentWidget)
    if not showButton:
        dialog.setStandardButtons(QMessageBox.NoButton)

    dialog.setWindowModality(Qt.NonModal)
    dialog.show()
    return dialog


def confirmModal(title, message):
    dialog = QMessageBox(QMessageBox.NoIcon, title, message)
    dialog.setWindowIcon(QIcon(':/images/window_icon.png'))
    dialog.setStandardButtons(QMessageBox.Ok | QMessageBox.Cancel)
    dialog.setEscapeButton(QMessageBox.Cancel)
    dialog.setDefaultButton(QMessageBox.Ok)
    pressedButton = dialog.exec()
    return pressedButton == QMessageBox.Ok

def optionsModal(title, message, *args):
    dialog = QMessageBox(QMessageBox.NoIcon, title, message)
    dialog.setWindowIcon(QIcon(':/images/window_icon.png'))
    dialog.setStandardButtons(QMessageBox.Cancel)
    dialog.setEscapeButton(QMessageBox.Cancel)
    dialog.setDefaultButton(QMessageBox.Cancel)

    for text in args:
        dialog.addButton(text, QMessageBox.YesRole)

    return dialog.exec()
