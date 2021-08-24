from PyQt5.QtWidgets import QStatusBar
from PyQt5.QtCore import QTimer, QChildEvent


class CustomStatusBar(QStatusBar):

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.frozenMessage = None

        self.messageChanged.connect(self.onMessageChanged)
        self.messageTimer = QTimer()
        self.messageTimer.setSingleShot(True)
        self.messageTimer.timeout.connect(self.unfreeze)


    def onMessageChanged(self, message):
        if self.frozenMessage and message != self.frozenMessage:
            self.showMessage(self.frozenMessage)
            return

    def highlightMessage(self, message, timeout = 1500):
        timeout = max(timeout, 250)
        self.showMessage(message, timeout)
        self.setProperty("class", "highlighted")
        self.style().polish(self)
        self.messageTimer.start(max(100, timeout - 100))
        self.freeze(message)


    def freeze(self, message):
        self.frozenMessage = message

    def unfreeze(self):
        self.frozenMessage = None
        self.setProperty("class", [])
        self.style().polish(self)
        self.clearMessage()