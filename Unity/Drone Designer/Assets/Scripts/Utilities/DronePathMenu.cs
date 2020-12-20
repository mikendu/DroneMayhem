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
    private const string ALWAYS_ITEM = "Drone Tools/Draw Drone Paths/Always";
    private const string ON_SELECT_ITEM = "Drone Tools/Draw Drone Paths/When Selected";
    public static bool AlwaysOn = true;

    static DronePathMenu()
    {
        AlwaysOn = EditorPrefs.GetBool(PREF_NAME, true);
        /// Delaying until first editor tick so that the menu
        /// will be populated before setting check state, and
        /// re-apply correct action
        EditorApplication.delayCall += () => {
            SetOption(AlwaysOn);
        };
    }


    [MenuItem(ALWAYS_ITEM, false, 25)]
    private static void SetAlways()
    {
        SetOption(true);
    }

    [MenuItem(ON_SELECT_ITEM, false, 26)]
    private static void SetOnSelected()
    {
        SetOption(false);
    }

    public static void SetOption(bool enabled)
    {
        /// Set checkmark on menu item
        Menu.SetChecked(ALWAYS_ITEM, enabled);
        Menu.SetChecked(ON_SELECT_ITEM, !enabled);
        
        /// Saving editor state
        EditorPrefs.SetBool(PREF_NAME, enabled);

        AlwaysOn = enabled;
    }
}

