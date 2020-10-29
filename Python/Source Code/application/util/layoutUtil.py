from enum import Enum
from PyQt5.QtWidgets import *


class LayoutType(Enum):
    VERTICAL = 0
    HORIZONTAL = 1
    GRID = 2

def clearLayout(layout):
    for i in reversed(range(layout.count())): 
        widgetToRemove = layout.itemAt(i).widget()
        layout.removeWidget(widgetToRemove) # remove it from the layout list
        if widgetToRemove:
            widgetToRemove.setParent(None) # remove it from the gui

def createLayout(layoutType, parentWidget = None):
    newLayout = None

    if layoutType == LayoutType.VERTICAL:
        newLayout = QVBoxLayout(parentWidget)

    elif layoutType == LayoutType.HORIZONTAL:
        newLayout = QHBoxLayout(parentWidget)

    elif layoutType == LayoutType.GRID:
        newLayout = QGridLayout(parentWidget)
    
    if newLayout is not None:
        newLayout.setSpacing(0)
        newLayout.setContentsMargins(0, 0, 0, 0)

    return newLayout