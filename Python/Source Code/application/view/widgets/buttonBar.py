from PyQt5.QtWidgets import QFrame, QLabel, QPushButton, QSizePolicy
from PyQt5.QtCore import Qt

from application.util import layoutUtil
from application.view import LayoutType

class ButtonBar(QFrame):

    def __init__(self, title, items, callback, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.layout = layoutUtil.createLayout(LayoutType.HORIZONTAL, self)
        self.createTitle(title)
        self.createButtons(items)
        self.callback = callback

    def createTitle(self, title):
        if title is None:
            return
        titleLabel = QLabel(title)
        titleLabel.setProperty("class", "buttonBarTitle")
        self.layout.addWidget(titleLabel)

    def createButtons(self, items):
        self.buttons = []
        for i, text in enumerate(items):
            button = QPushButton(text)
            button.setCursor(Qt.PointingHandCursor)
            button.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
            button.clicked.connect(lambda state, index=i: self.onPress(index))

            if i == 0:
                button.setProperty("class", ["buttonBarButton", "enabled"])
            else:
                button.setProperty("class", ["buttonBarButton", "special"])

            self.layout.addWidget(button)
            self.buttons.append(button)

    def onPress(self, index):
        for i, button in enumerate(self.buttons):
            classes = ["buttonBarButton"]
            if i != 0:
                classes.append("special")
            if i == index:
                classes.append("enabled")

            button.setProperty("class", classes)
            button.style().polish(button)
        self.callback(index)

    def setSelected(self, index):
        if index >= 0 and index < len(self.buttons):
            self.onPress(index)