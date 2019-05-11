using System.Collections.Generic;
using Xam.Plugin.TabView;
using Xamarin.Forms;

namespace ConventionMobile.Views
{
    public abstract class GenContentView : ContentView
    {
        public readonly string Title;
        public readonly List<ToolbarItem> ToolbarItems = new List<ToolbarItem>();

        protected GenContentView(string title)
        {
            Title = title;
        }

        public TabItem AsTabItem()
        {
            return new TabItem(this.Title, this.Content);
        }
    }
}
