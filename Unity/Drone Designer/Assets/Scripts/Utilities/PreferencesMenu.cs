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
    private const string SMOOTH_PREF = "smooth_path_mode";

    private const string PATH_DISPLAY_NAME = "Drone Tools/Show All Paths";
    private const string PRETTY_DISPLAY_NAME = "Drone Tools/Pretty Export Format";
    private const string SMOOTH_DISPLAY_NAME = "Drone Tools/Enable Smooth Path Mode";

    public static bool PathEnabled = true;
    public static bool PrettyEnabled = false;
    public static bool SmoothEnabled = false;

    static PreferencesMenu()
    {
        PathEnabled = EditorPrefs.GetBool(PATH_PREF, true);
        PrettyEnabled = EditorPrefs.GetBool(PRETTY_PREF, false);
        SmoothEnabled = EditorPrefs.GetBool(SMOOTH_PREF, false);

        EditorApplication.delayCall += () => {
            SetOption(PATH_DISPLAY_NAME, PATH_PREF, PathEnabled);
            SetOption(PRETTY_DISPLAY_NAME, PRETTY_PREF, PrettyEnabled);
            SetOption(SMOOTH_DISPLAY_NAME, SMOOTH_PREF, SmoothEnabled);
        };
    }


    [MenuItem(PATH_DISPLAY_NAME, false, 45)]
    private static void TogglePathPref() { PathEnabled = SetOption(PATH_DISPLAY_NAME, PATH_PREF, !PathEnabled); }

    [MenuItem(PRETTY_DISPLAY_NAME, false, 45)]
    private static void TogglePrettyPref() { PrettyEnabled = SetOption(PRETTY_DISPLAY_NAME, PRETTY_PREF, !PrettyEnabled); }

    [MenuItem(SMOOTH_DISPLAY_NAME, false, 45)]
    private static void ToggleSmooth() { SmoothEnabled = SetOption(SMOOTH_DISPLAY_NAME, SMOOTH_PREF, !SmoothEnabled); }

    public static bool SetOption(string menuItemName, string prefName, bool enabled)
    {
        /// Set checkmark on menu item
        Menu.SetChecked(menuItemName, enabled);

        /// Saving editor state
        EditorPrefs.SetBool(prefName, enabled);

        return enabled;
    }
}

