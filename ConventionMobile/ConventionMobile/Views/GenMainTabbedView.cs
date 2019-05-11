using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConventionMobile.Pages;
using Xam.Plugin.TabView;
using Xamarin.Forms;

namespace ConventionMobile.Views
{
    public class GenMainTabbedView : ContentView
    {
        private readonly GenMainPage _parentPage;
        private readonly List<GenContentView> _tabList;

        public GenMainTabbedView(GenMainPage parentPage)
        {
            _parentPage = parentPage;

            _tabList = new List<GenContentView> //needed for tab controls
            {
                new GenMapView(_parentPage),
                new GenSearchView(_parentPage),
                new GenUserListView(_parentPage)
            };

            // main tabbed view
            var tabViewControl = new TabViewControl(_tabList.Select(x => x.AsTabItem()).ToList());

            this.Content = tabViewControl;
            this.VerticalOptions = LayoutOptions.FillAndExpand;
            this.HorizontalOptions = LayoutOptions.FillAndExpand;

            tabViewControl.PositionChanged += TabView_PositionChanged;

        }

        private void TabView_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            // update toolbar items
            if (_parentPage.ToolbarItems.Any())
            {
                _parentPage.ToolbarItems.Clear();
            }

            _tabList[e.NewPosition].ToolbarItems.ForEach(toolbarItem => _parentPage.ToolbarItems.Add(toolbarItem));
        }
    }
}
