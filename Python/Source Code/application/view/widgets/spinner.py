from PyQt5.QtWidgets import QLabel
from PyQt5.QtGui import QPixmap, QPainter
from PyQt5.QtCore import Qt, QPropertyAnimation, pyqtProperty


class Spinner(QLabel):

    ICON = None
    SIZE = 75
    HALF_SIZE = SIZE / 2.0

    def __init__(self, *args, **kwargs):
        super().__init__()
        
        if not Spinner.ICON:
            Spinner.ICON = QPixmap(":/images/spinner_two.png")

        self.setAlignment(Qt.AlignCenter)
        self.setFixedSize(Spinner.SIZE, Spinner.SIZE)
        self.setPixmap(Spinner.ICON.scaledToWidth(Spinner.SIZE, Qt.SmoothTransformation))
        
        self.angle = 0
        self.animation = QPropertyAnimation(self, b"rotation", self)
        self.animation.setStartValue(0)
        self.animation.setEndValue(360)
        self.animation.setLoopCount(-1)
        self.animation.setDuration(1250)
        self.animation.start()

    @pyqtProperty(float)
    def rotation(self):
        return self.angle

    @rotation.setter
    def rotation(self, value):
        self.angle = value
        self.update()

    def paintEvent(self, event=None):
        painter = QPainter(self)
        painter.translate(Spinner.HALF_SIZE, Spinner.HALF_SIZE)
        painter.rotate(self.angle)
        painter.translate(-Spinner.HALF_SIZE, -Spinner.HALF_SIZE)
        painter.drawPixmap(0, 0, self.pixmap())
