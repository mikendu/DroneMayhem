from kivy.uix.widget import Widget
from kivy.properties import NumericProperty, ReferenceListProperty, ObjectProperty
from kivy.lang import Builder

from view.menus.file import FileMenu

# Setup
Builder.load_file('./view/dashboard.kv') 


class Dashboard(Widget):
    
    menu: ObjectProperty(None)
