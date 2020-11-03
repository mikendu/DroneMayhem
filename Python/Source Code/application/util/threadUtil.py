import time

from PyQt5.QtCore import *
from .exceptionUtil import * 

def runInBackground(function, *args, **kwargs):
    runnable = GenericRunnable(function, *args, **kwargs)
    QThreadPool.globalInstance().start(runnable)


def interruptibleSleep(duration, interval):
    start = time.time()
    elapsed = time.time() - start

    while elapsed < duration:
        time.sleep(interval)
        checkInterrupt()
        elapsed = time.time() - start

class GenericRunnable(QRunnable):

    def __init__(self, runFunction, *args, **kwargs):
        super().__init__()
        self.runFunction = runFunction
        self.args = args
        self.kwargs = kwargs
    
    @pyqtSlot()
    def run(self):
        self.runFunction(*self.args, **self.kwargs)