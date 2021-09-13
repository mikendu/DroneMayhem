from PyQt5.QtWidgets import QLabel
from PyQt5.QtGui import QPainter, QFontMetrics
from PyQt5.QtCore import Qt

class ElidedLabel(QLabel):

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.maxWidth = None

    def paintEvent(self, event):
        maxWidth = self.maxWidth if self.maxWidth is not None else self.width()

        painter = QPainter(self)
        metrics = QFontMetrics(self.font())
        #elided = metrics.elidedText(self.text(), Qt.ElideRight, maxWidth)
        painter.drawText(self.rect(), self.alignment(), self.text())