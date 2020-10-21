import kivy
from kivy.app import App
from kivy.lang import Builder

from view.dashboard import *

# Setup
kivy.require('1.11.1')
Builder.load_file('./view/dashboard.kv') 


class DroneManager(App):
    def build(self):
        self.title = 'Drone Manager'
        self.icon = './resources/app_icon.png'
        return Dashboard()


if __name__ == '__main__':
    DroneManager().run()