using System;
using System.Reflection;
using LiveSplit.Model;
using LiveSplit.UI.Components;

[assembly: ComponentFactory(typeof(LiveSplit.BunnySplit.BunnySplitComponentFactory))]

namespace LiveSplit.BunnySplit
{
    public class BunnySplitComponentFactory : IComponentFactory
    {
        public ComponentCategory Category { get { return ComponentCategory.Control; } }
        public string ComponentName { get { return "BunnySplit"; } }
        public string Description { get { return "In-Game Time and Auto-Split component that works with Bunnymod XT!"; } }
        public string UpdateName { get { return ComponentName; } }
        public string UpdateURL { get { throw new NotImplementedException(); } }
        public Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
        public string XMLURL { get { return "http://livesplit.org/update/Components/noupdates.xml"; } }

        public IComponent Create(LiveSplitState state)
        {
            return new BunnySplitComponent(state);
        }
    }
}
