using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

public static class PreferencesMenu
{
    private const string PATH_PREF = "drone_paths_always_on";
    private const string PRETTY_PREF = "export_pretty_print";

    private const string PATH_DISPLAY_NAME = "Drone Tools/Show All Paths";
    private const string PRETTY_DISPLAY_NAME = "Drone Tools/Pretty Export Format";

    public static bool PathEnabled = true;
    public static bool PrettyEnabled = false;

    static PreferencesMenu()
    {
        PathEnabled = EditorPrefs.GetBool(PATH_PREF, true);
        PrettyEnabled = EditorPrefs.GetBool(PRETTY_PREF, false);

        EditorApplication.delayCall += () => {
            SetOption(PATH_DISPLAY_NAME, PATH_PREF, PathEnabled);
            SetOption(PRETTY_DISPLAY_NAME, PRETTY_PREF, PrettyEnabled);
        };
    }


    [MenuItem(PATH_DISPLAY_NAME, false, 45)]
    private static void TogglePathPref() { PathEnabled = SetOption(PATH_DISPLAY_NAME, PATH_PREF, !PathEnabled); }

    [MenuItem(PRETTY_DISPLAY_NAME, false, 45)]
    private static void TogglePrettyPref() { PrettyEnabled = SetOption(PRETTY_DISPLAY_NAME, PRETTY_PREF, !PrettyEnabled); }

    public static bool SetOption(string menuItemName, string prefName, bool enabled)
    {
        /// Set checkmark on menu item
        Menu.SetChecked(menuItemName, enabled);

        /// Saving editor state
        EditorPrefs.SetBool(prefName, enabled);

        return enabled;
    }
}

