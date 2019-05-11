using System;
using ConventionMobile.Pages;
using Plugin.Share;
using Xamarin.Forms;

namespace ConventionMobile.Views
{
    public class GenMapView : GenContentView
    {
        private readonly GenMainPage _parentPage;
        private readonly ListView _nagivationListView;

        public void ClearNavOption(BindableProperty property)
        {
            _nagivationListView?.ClearValue(property);
        }

        public GenMapView(GenMainPage parentPage) : base(GlobalVars.navigationTitle)
        {
            this._parentPage = parentPage;

            _nagivationListView = new ListView
            {
                ItemTemplate = new DataTemplate(typeof(NavigationCell)),
                ItemsSource = GlobalVars.NavigationChoices
            };

            _nagivationListView.ItemSelected += (async (sender, args) => {

                GlobalVars.GenConBusiness.ShowLoadingEventMessage("Data is still loading, map may not be up to date");

                if (args.SelectedItem != null)
                {
                    var selectedDetailChoice = (DetailChoice)args.SelectedItem;

                    if (selectedDetailChoice.data.ToLower().StartsWith("http:") || selectedDetailChoice.data.ToLower().StartsWith("https:"))
                    {
                        await CrossShare.Current.OpenBrowser(selectedDetailChoice.data, null);
                    }
                    else
                    {
                        //TODO: this is no bueno, the page will navigate away from the main layout
                        //need to find a way to make this just an overlay or layer

                        var page = (Page)Activator.CreateInstance(selectedDetailChoice.pageType);
                        page.BindingContext = selectedDetailChoice;
                        await this.Navigation.PushAsync(page);
                    }
                }
            });

            var content = new StackLayout
            {
                Children =
                {
                    _nagivationListView
                }
            };

            this.Content = content;
        }
    }
}

