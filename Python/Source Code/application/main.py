import kivy
from kivy.app import App
from kivy.core.window import Window
from kivy.config import Config

from view.dashboard import Dashboard

# Setup
fontDefinition = [
    'Montserrat', 
    './resources/fonts/Montserrat/Montserrat-Light.ttf',
    './resources/fonts/Montserrat/Montserrat-LightItalic.ttf',
    './resources/fonts/Montserrat/Montserrat-Regular.ttf',
    './resources/fonts/Montserrat/Montserrat-Italic.ttf'
]

kivy.require('1.11.1')
Config.set('graphics', 'borderless', 1)
Config.set('graphics', 'position', 'auto')
Config.set('graphics', 'top', '1920')
Config.set('graphics', 'left', '1080')
Config.set('kivy', 'default_font', fontDefinition)
Config.write()


class DroneManager(App):
    def build(self):
        self.title = 'Drone Manager'
        self.icon = './resources/images/app_icon_small.png'
        return Dashboard()


if __name__ == '__main__':
    DroneManager().run()