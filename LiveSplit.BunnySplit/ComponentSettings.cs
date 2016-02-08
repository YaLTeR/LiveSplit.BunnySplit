using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;

namespace LiveSplit.BunnySplit
{
    public partial class ComponentSettings : UserControl
    {
        private readonly List<string> hl1_chapter_maps = new List<string>
        {
            "c1a0c",    // Unforeseen Consequences
            "c1a2",     // Office Complex
            "c1a3",     // We've Got Hostiles
            "c1a4",     // Blast Pit
            "c2a1",     // Power Up
            "c2a2",     // On a Rail
            "c2a3",     // Apprehension
            "c2a4",     // Residue Processing
            "c2a4d",    // Questionable Ethics
            "c2a5",     // Surface Tension
            "c3a1",     // Forget About Freeman
            "c3a2e",    // Lambda Core
            "c4a1",     // Xen
            "c4a2",     // Gonarch's Lair
            "c4a1a",    // Interloper
            "c4a3"      // Nihilanth
        };

        private readonly List<string> op4_chapter_maps = new List<string>
        {
            "of1a5",    // We Are Pulling Out
            "of2a1",    // Missing in Action
            "of2a4",    // Friendly Fire
            "of3a1",    // We Are Not Alone
            "of3a4",    // Crush Depth
            "of4a1",    // Vicarious Reality
            "of4a4",    // Pit Worm's Nest
            "of5a1",    // Foxtrot Uniform
            "of6a1",    // The Package
            "of6a4b"    // Worlds Collide
        };

        private readonly List<string> bs_chapter_maps = new List<string>
        {
            "ba_canal1",    // Duty Calls
            "ba_yard1",     // Captive Freight
            "ba_xen1",      // Focal Point
            "ba_power1"     // Power Struggle
            // A Leap of Faith has special handling.
        };

        private readonly List<string> gmc_chapter_maps = new List<string>
        {
            "mayan0a",      // Mayan
            "cinematic1",   // Cinematic
            "cinematic3",   // Cinematic
            "rebar0a",      // Rebar
            "end1",         // End
        };

        public ComponentSettings()
        {
            InitializeComponent();
        }

        private void SetDefaultSettings()
        {
            EnableAutoSplitCheckbox.Checked = false;
            SplitOnGameEndCheckbox.Checked = true;
            SplitOnHL1ChaptersCheckbox.Checked = true;
            SplitOnOP4ChaptersCheckbox.Checked = true;
            SplitOnBSChaptersCheckbox.Checked = true;
            SplitOnGMCChaptersCheckbox.Checked = true;
            SplitOnMapsCheckbox.Checked = true;
            SplitOnMapsList.Rows.Clear();

            EnableAutoResetCheckbox.Checked = true;
            EnableAutoStartCheckbox.Checked = true;
        }

        private static void AppendElement<T>(XmlDocument document, XmlElement parent, string name, T value)
        {
            XmlElement el = document.CreateElement(name);
            el.InnerText = value.ToString();
            parent.AppendChild(el);
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settingsNode = document.CreateElement("Settings");

            AppendElement(document, settingsNode, "Version", Assembly.GetExecutingAssembly().GetName().Version);

            AppendElement(document, settingsNode, "EnableAutoSplit", EnableAutoSplitCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnGameEnd", SplitOnGameEndCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnHL1Chapters", SplitOnHL1ChaptersCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnOP4Chapters", SplitOnOP4ChaptersCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnBSChapters", SplitOnBSChaptersCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnGMCChapters", SplitOnGMCChaptersCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnMaps", SplitOnMapsCheckbox.Checked);
            AppendElement(document, settingsNode, "SplitOnMapsList", string.Join("|", SplitOnMapsList.GetValues()));

            AppendElement(document, settingsNode, "EnableAutoReset", EnableAutoResetCheckbox.Checked);
            AppendElement(document, settingsNode, "EnableAutoStart", EnableAutoStartCheckbox.Checked);

            return settingsNode;
        }

        private bool FindSetting(XmlNode node, string name, bool previous)
        {
            var element = node[name];
            if (element == null)
                return previous;

            bool b;
            if (bool.TryParse(element.InnerText, out b))
                return b;

            return previous;
        }

        public void SetSettings(XmlNode settings)
        {
            SetDefaultSettings();
            SetAutoSplitCheckboxColors();

            var versionElement = settings["Version"];
            if (versionElement == null)
                return;
            Version ver;
            if (!Version.TryParse(versionElement.InnerText, out ver))
                return;
            if (ver.Major != 2 || ver.Minor != 0)
                return;

            EnableAutoSplitCheckbox.Checked = FindSetting(settings, "EnableAutoSplit", EnableAutoSplitCheckbox.Checked);
            SplitOnGameEndCheckbox.Checked = FindSetting(settings, "SplitOnGameEnd", SplitOnGameEndCheckbox.Checked);
            SplitOnHL1ChaptersCheckbox.Checked = FindSetting(settings, "SplitOnHL1Chapters", SplitOnHL1ChaptersCheckbox.Checked);
            SplitOnOP4ChaptersCheckbox.Checked = FindSetting(settings, "SplitOnOP4Chapters", SplitOnOP4ChaptersCheckbox.Checked);
            SplitOnBSChaptersCheckbox.Checked = FindSetting(settings, "SplitOnBSChapters", SplitOnBSChaptersCheckbox.Checked);
            SplitOnGMCChaptersCheckbox.Checked = FindSetting(settings, "SplitOnGMCChapters", SplitOnGMCChaptersCheckbox.Checked);
            SplitOnMapsCheckbox.Checked = FindSetting(settings, "SplitOnMaps", SplitOnMapsCheckbox.Checked);
            var e = settings["SplitOnMapsList"];
            if (e != null)
                foreach (var map in e.InnerText.Split('|'))
                    SplitOnMapsList.Rows.Add(map);

            EnableAutoResetCheckbox.Checked = FindSetting(settings, "EnableAutoReset", EnableAutoResetCheckbox.Checked);
            EnableAutoStartCheckbox.Checked = FindSetting(settings, "EnableAutoStart", EnableAutoStartCheckbox.Checked);

            SetAutoSplitCheckboxColors();
        }

        public bool ShouldSplitOn(string map)
        {
            if (!IsAutoSplitEnabled())
                return false;

            if (SplitOnHL1ChaptersCheckbox.Checked && hl1_chapter_maps.Contains(map))
                return true;
            if (SplitOnOP4ChaptersCheckbox.Checked && op4_chapter_maps.Contains(map))
                return true;
            if (SplitOnBSChaptersCheckbox.Checked && bs_chapter_maps.Contains(map))
                return true;
            if (SplitOnGMCChaptersCheckbox.Checked && gmc_chapter_maps.Contains(map))
                return true;

            if (SplitOnMapsCheckbox.Checked && SplitOnMapsList.GetValues().Contains(map))
                return true;

            return false;
        }

        public bool ShouldSplitOnGameEnd()
        {
            return IsAutoSplitEnabled() && SplitOnGameEndCheckbox.Checked;
        }

        public bool IsAutoSplitEnabled()
        {
            return EnableAutoSplitCheckbox.Checked;
        }

        public bool IsAutoResetEnabled()
        {
            return EnableAutoResetCheckbox.Checked;
        }

        public bool IsAutoStartEnabled()
        {
            return EnableAutoStartCheckbox.Checked;
        }

        private void SetAutoSplitCheckboxColors()
        {
            if (IsAutoSplitEnabled())
            {
                SplitOnGameEndCheckbox.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                SplitOnHL1ChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                SplitOnOP4ChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                SplitOnBSChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                SplitOnGMCChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                SplitOnMapsCheckbox.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
            }
            else
            {
                SplitOnGameEndCheckbox.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                SplitOnHL1ChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                SplitOnOP4ChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                SplitOnBSChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                SplitOnGMCChaptersCheckbox.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
                SplitOnMapsCheckbox.ForeColor = Color.FromKnownColor(KnownColor.GrayText);
            }
        }

        private void EnableAutoSplitCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoSplitCheckboxColors();
        }
    }
}
