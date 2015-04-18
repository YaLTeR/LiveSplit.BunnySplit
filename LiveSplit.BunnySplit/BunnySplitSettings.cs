using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.BunnySplit
{
    public partial class BunnySplitSettings : UserControl
    {
        public BunnySplitSettings()
        {
            InitializeComponent();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            XmlElement settingsNode = document.CreateElement("Settings");
            return settingsNode;
        }

        public void SetSettings(XmlNode settings)
        {
        }
    }
}
