using System;
using System.Reflection;
using LiveSplit.Model;
using LiveSplit.UI.Components;

[assembly: ComponentFactory(typeof(LiveSplit.BunnySplit.Factory))]

namespace LiveSplit.BunnySplit
{
    public class Factory : IComponentFactory
    {
        public ComponentCategory Category => ComponentCategory.Control;
        public string ComponentName => "BunnySplit";
        public string Description => "In-Game Time and Auto-Split component that works with Bunnymod XT.";

        public string UpdateName => ComponentName;
        public string UpdateURL => "http://play.sourceruns.org/BunnySplit/";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public string XMLURL => UpdateURL + "updates.xml";

        public IComponent Create(LiveSplitState state) => new Component(state);
    }
}
