from kivy.uix.widget import Widget
from kivy.uix.relativelayout import RelativeLayout
from kivy.lang import Builder

# Setup
Builder.load_file('./view/menus/file.kv') 

class FileMenu(RelativeLayout):
    
    def __init__(self, **kwargs):
        super(FileMenu, self).__init__(**kwargs)

