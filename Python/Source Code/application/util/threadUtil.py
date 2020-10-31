from PyQt5.QtCore import *

def runInBackground(function, *args, **kwargs):
    runnable = GenericRunnable(function, *args, **kwargs)
    QThreadPool.globalInstance().start(runnable)


class GenericRunnable(QRunnable):

    def __init__(self, runFunction, *args, **kwargs):
        super().__init__()
        self.runFunction = runFunction
        self.args = args
        self.kwargs = kwargs
    
    @pyqtSlot()
    def run(self):
        self.runFunction(*self.args, **self.kwargs)