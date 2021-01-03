using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public static class DronePathMenu
{
    private const string PREF_NAME = "drone_paths_always_on";
    private const string MENU_ITEM_NAME = "Drone Tools/Show All Paths";
    public static bool AlwaysOn = true;

    static DronePathMenu()
    {
        AlwaysOn = EditorPrefs.GetBool(PREF_NAME, false);
        /// Delaying until first editor tick so that the menu
        /// will be populated before setting check state, and
        /// re-apply correct action
        EditorApplication.delayCall += () => {
            SetOption(AlwaysOn);
        };
    }


    [MenuItem(MENU_ITEM_NAME, false, 45)]
    private static void Toggle()
    {
        SetOption(!AlwaysOn);
    }

    public static void SetOption(bool enabled)
    {
        /// Set checkmark on menu item
        Menu.SetChecked(MENU_ITEM_NAME, enabled);
        
        /// Saving editor state
        EditorPrefs.SetBool(PREF_NAME, enabled);

        AlwaysOn = enabled;
    }
}

