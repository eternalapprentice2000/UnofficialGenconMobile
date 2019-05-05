using Plugin.Share;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ConventionMobile.Helpers;
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
                    if (GlobalVars.EventLoadStatus != Enums.EventLoadStatus.NotRunning)
                    {
                        NotificationBox.AddNotificationAsync("Selected map might be out of date", NotificationLevel.Medium).GetAwaiter().GetResult();
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
                        await this.Navigation.PushAsync(page);
                    }
                }
            };
        }

        public async Task CheckForNewEventsAsync()
        {
            GlobalVars.EventLoadStatus = Enums.EventLoadStatus.LoadingEvents;
            if (GlobalVars.isSyncNeeded || overrideUpdateCheckEvents)
            {
                overrideUpdateCheckEvents = false;
                await NotificationBox.AddNotificationAsync("Now checking for updated events...", NotificationLevel.Medium);
                var events = await App.GenEventManager.GetEventsAsync(await GlobalVars.serverLastSyncTime());
                if (events.Count > 0)
                {
                    await NotificationBox.AddNotificationAsync(await GlobalVars.db.SaveItemsAsync(events), NotificationLevel.Medium, 10000);
                }
                else
                {
                    await NotificationBox.AddNotificationAsync("All events are now up-to-date.");
                }

                GlobalVars.lastSyncTime = DateTime.Now;
                searchPage?.UpdateEventInfo();
            }
            
            await CheckForNewGlobalVarsAsync();

            GlobalVars.EventLoadStatus = Enums.EventLoadStatus.NotRunning;
        }

        private async Task CheckForNewGlobalVarsAsync()
        {
            //string newURL = String.Format(GlobalVars.GlobalOptionsURLCustomizableURL, TimeZoneInfo.ConvertTime(GlobalVars.lastGlobalVarUpdateTime, TimeZoneInfo.Utc).ToString("yyyy-MM-dd't'HH:mm:ss"));
            GlobalVars.EventLoadStatus = Enums.EventLoadStatus.LoadingMaps;
            if (GlobalVars.isGlobalVarSyncNeeded || overrideUpdateCheckOptions)
            {
                overrideUpdateCheckOptions = false;
                await NotificationBox.AddNotificationAsync("Now checking for updated maps or other info...", NotificationLevel.Medium);

                // todo, should move this to an abstract layer
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

                                //GlobalVars.DoToast("Update success - **REFRESHING SCREEN**", GlobalVars.ToastType.Green);
                                await NotificationBox.AddNotificationAsync("Update success - **REFRESHING SCREEN**");
                                searchPage?.UpdateEventInfo();

                                if (searchPage != null) await searchPage?.CloseAllPickers();
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    try
                                    {
                                        App.Current.MainPage = new NavigationPage(new GenHomeTabPage());
                                    }
                                    catch (Exception)
                                    {
                                        // should at least log something here
                                    }
                                });


                            }
                            else
                            {
                                await NotificationBox.AddNotificationAsync("You are up to date.");

                                try
                                {
                                    Device.BeginInvokeOnMainThread(() =>
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
                        await NotificationBox.AddNotificationAsync("Couldn't update now, but we'll try again later.", NotificationLevel.Critical);
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
            if (GlobalVars.EventLoadStatus == Enums.EventLoadStatus.NotRunning)
            {
                Task.Factory.StartNew(CheckForNewEventsAsync);
            }
            navigationListView?.ClearValue(ListView.SelectedItemProperty);
            CheckForUserListPageListRefresh();
        }
    }
}
