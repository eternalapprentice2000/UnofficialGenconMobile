﻿using ConventionMobile.Views;
using Xamarin.Forms;

namespace ConventionMobile.Pages
{
    public class GenMainPage : ContentPage
    {
        public ClosableNotificationBox NotificationBox;

        public GenMainPage()
        {
            var tabbedView = new GenMainTabbedView(this);
            NotificationBox = new ClosableNotificationBox();
            
            this.Title = GlobalVars.appTitle;
            this.Content = new StackLayout
            {
                Children =
                {
                    tabbedView,
                    NotificationBox
                },
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };
            
            
            Xamarin.Forms.PlatformConfiguration.AndroidSpecific.TabbedPage.SetIsSwipePagingEnabled(this, true);
        }
    }
}
