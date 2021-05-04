from PyQt5.QtWidgets import QVBoxLayout, QHBoxLayout, QGridLayout
from application.view import LayoutType

def clearLayout(layout):
    for i in reversed(range(layout.count())): 
        widgetToRemove = layout.itemAt(i).widget()
        layout.removeWidget(widgetToRemove)  # remove it from the layout list
        if widgetToRemove:
            widgetToRemove.setParent(None)  # remove it from the gui

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