using Plugin.Share;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ConventionMobile.Views
{
    public class GenHomeTabPage : TabbedPage
    {
        //GenSearchPage searchPage;
        GenSearchPage searchPage = null;
        public UserListPage userListPage;
        ListView navigationListView;
        
        public bool overrideUpdateCheckEvents = false;
        public bool overrideUpdateCheckOptions = false;

        protected override void OnCurrentPageChanged()
        {
            CheckForUserListPageListRefresh();
            base.OnCurrentPageChanged();
        }

        public void CheckForUserListPageListRefresh()
        {
            if (this.CurrentPage.Title == GlobalVars.userListsTitle)
            {
                try
                {
                    if (userListPage.IsUpdateRequested || ((App)Application.Current).HomePage.userListPage.IsUpdateRequested)
                    {
                        userListPage.IsUpdateRequested = false;
                        ((App)Application.Current).HomePage.userListPage.IsUpdateRequested = false;
                        userListPage.UpdateUserLists();
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public GenHomeTabPage()
        {
            navigationListView = new ListView
            {
                ItemTemplate = new DataTemplate(typeof(NavigationCell)),
                ItemsSource = GlobalVars.NavigationChoices
            };

            navigationListView.ItemTapped += async (sender, args) =>
            {
                await Task.Run(() =>
                {
                    if (GlobalVars.EventLoadStatus != GlobalVars.EventLoadStatusEnum.NotRunning)
                    {
                        GlobalVars.DoToast("Selected map might be out of date", GlobalVars.ToastType.Red, 1000);
                    }
                    
                });
            };

            this.Title = GlobalVars.appTitle;
            
            var mapPage = new ContentPage
            {
                Title = GlobalVars.navigationTitle,
                Content = new StackLayout
                {
                    // Edit children here to add additional navigation options besides just maps.
                    Children =
                    {
                        navigationListView
                    }
                }
            };

            searchPage = new GenSearchPage();
            userListPage = new UserListPage();
           
            Children.Add(mapPage);
            Children.Add(searchPage);
            Children.Add(userListPage);

            Xamarin.Forms.PlatformConfiguration.AndroidSpecific.TabbedPage.SetIsSwipePagingEnabled(this, true);

            // Define a selected handler for the ListView.
            navigationListView.ItemSelected += async (sender, args) => {
                if (args.SelectedItem != null)
                {
                    DetailChoice selectedDetailChoice = (DetailChoice)args.SelectedItem;

                    if (selectedDetailChoice.data.ToLower().StartsWith("http:") || selectedDetailChoice.data.ToLower().StartsWith("https:"))
                    {
                        await CrossShare.Current.OpenBrowser(selectedDetailChoice.data, null);
                    }
                    else
                    {
                        Page page = (Page)Activator.CreateInstance(selectedDetailChoice.pageType);
                        page.BindingContext = selectedDetailChoice;
                        //this.IsPresented = false;
                        await this.Navigation.PushAsync(page);
                    }
                }
            };
        }

        public async Task CheckForNewEventsAsync()
        {
            GlobalVars.EventLoadStatus = GlobalVars.EventLoadStatusEnum.Checking;
            bool dontShowFurtherToasts = false;
            if (GlobalVars.isSyncNeeded || overrideUpdateCheckEvents)
            {
                overrideUpdateCheckEvents = false;
                GlobalVars.DoToast("Now checking for updated events...", GlobalVars.ToastType.Yellow);
                var events = await App.GenEventManager.GetEventsAsync(await GlobalVars.serverLastSyncTime());

                string toastText = "";

                if (events.Count > 0)
                {
                    toastText = await GlobalVars.db.SaveItemsAsync(events);
                }

                GlobalVars.lastSyncTime = DateTime.Now;

                if (!string.IsNullOrEmpty(toastText))
                {
                    GlobalVars.DoToast(toastText, GlobalVars.ToastType.Yellow, 10000);
                    dontShowFurtherToasts = true;
                }
                else
                {
                    GlobalVars.DoToast("All events are now up-to-date.", GlobalVars.ToastType.Green);
                }
                searchPage?.UpdateEventInfo();
            }
            
            await CheckForNewGlobalVarsAsync(dontShowFurtherToasts);

            GlobalVars.EventLoadStatus = GlobalVars.EventLoadStatusEnum.NotRunning;
        }

        private async Task CheckForNewGlobalVarsAsync(bool dontShowFurtherToasts)
        {
            //string newURL = String.Format(GlobalVars.GlobalOptionsURLCustomizableURL, TimeZoneInfo.ConvertTime(GlobalVars.lastGlobalVarUpdateTime, TimeZoneInfo.Utc).ToString("yyyy-MM-dd't'HH:mm:ss"));

            if (GlobalVars.isGlobalVarSyncNeeded || overrideUpdateCheckOptions)
            {
                overrideUpdateCheckOptions = false;

                if (!dontShowFurtherToasts)
                {
                    GlobalVars.DoToast("Now checking for updated maps or other info...", GlobalVars.ToastType.Yellow);
                }

                using (var client = new HttpClient())
                {
                    client.MaxResponseContentBufferSize = 25600000; // why not int.MaxValue?
                    DateTime indyTime = DependencyService.Get<ICalendar>().ConvertToIndy(GlobalVars.lastGlobalVarUpdateTime);
                    string newURL = String.Format(GlobalVars.GlobalOptionsURLCustomizableURL, indyTime.ToString("yyyy-MM-dd't'HH:mm:ss"));

                    try
                    {
                        var response = await client.GetAsync(newURL).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            if (content.Length > 10)
                            {
                                GlobalVars.FileDownloadProgressUpdated += GlobalVars_FileDownloadProgressUpdated;

                                var totalSuccess = await GlobalVars.OverwriteOptions(content);

                                GlobalVars.FileDownloadProgressUpdated -= GlobalVars_FileDownloadProgressUpdated;

                                GlobalVars.DoToast("Update success - **REFRESHING SCREEN**", GlobalVars.ToastType.Green);
                                searchPage?.UpdateEventInfo();

                                await searchPage?.CloseAllPickers();
                                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                                {
                                    try
                                    {
                                        App.Current.MainPage = new NavigationPage(new GenHomeTabPage());
                                    }
                                    catch (Exception)
                                    {

                                    }
                                });


                            }
                            else
                            {
                                if (!dontShowFurtherToasts)
                                {
                                    GlobalVars.DoToast("You are up to date.", GlobalVars.ToastType.Green);
                                }
                                
                                try
                                {
                                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                                    {
                                        searchPage?.UpdateEventInfo();
                                    });
                                }
                                catch (Exception)
                                {

                                }
                                GlobalVars.lastGlobalVarUpdateTime = DateTime.Now;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (!dontShowFurtherToasts)
                        {
                            GlobalVars.DoToast("Couldn't update now, but we'll try again later.", GlobalVars.ToastType.Yellow);
                        }
                    }
                }
            }
        }

        private void GlobalVars_FileDownloadProgressUpdated(object sender, EventArgs e)
        {
            //For now do nothing. Maybe later show progress bar or something.
            //throw new NotImplementedException();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (GlobalVars.EventLoadStatus == GlobalVars.EventLoadStatusEnum.NotRunning)
            {
                Task.Factory.StartNew(CheckForNewEventsAsync);
            }
            navigationListView?.ClearValue(ListView.SelectedItemProperty);
            CheckForUserListPageListRefresh();
        }
    }
}
