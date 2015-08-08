using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Input;
using Guardian.Model;
using Guardian.Resources;
using ZXing;

namespace Guardian {
    public partial class HomePage : PhoneApplicationPage {
        private Item _item;
        public Item Item {
            get;
            set;
        }

        // filtering items list
        private string itemsType = "all";

        public HomePage() {
            InitializeComponent();
            
            InitializeNewItem();
            InitializeMyItems();

            // Sample code to localize the ApplicationBar
            BuildLocalizedApplicationBar();

            // if there is no user in DB we have to create it
            if (App.User == null) {
                this.Loaded += (s, e) => {
                    NavigationService.Navigate(new Uri("/View/NewUser.xaml", UriKind.RelativeOrAbsolute));
                };
            }
            else {
                LoggedAs.Text = AppResources.LoggedAs + " " + App.User.Name;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (App.User != null)
                LoggedAs.Text = AppResources.LoggedAs + " " + App.User.Name;

            NavigationService.RemoveBackEntry();
        }

        // new item initialization
        private void InitializeNewItem() {
            _item = Item = new Item();

            this.DataContext = Item;
            this.NewItemCategory.ItemsSource = Utility.GetNames(typeof(Category));
            
            if (NFCHandle.GetInstance().IsSupported) {
                this.SaveTag.IsEnabled = NFCHandle.GetInstance().IsTagAvailable;

                NFCHandle.GetInstance().TagArrived += TagArrived;
                NFCHandle.GetInstance().TagDeparted += TagDeparted;
            }
            else {
                this.SaveTag.Visibility = Visibility.Collapsed;
            }
        }

        private void TagArrived(object sender, EventArgs e) {
            Deployment.Current.Dispatcher.BeginInvoke(() => SaveTag.IsEnabled = true);
        }

        private void TagDeparted(object sender, EventArgs e) {
            Deployment.Current.Dispatcher.BeginInvoke(() => SaveTag.IsEnabled = false);
        }

        private bool IsValid() {
            // validate
            if (string.IsNullOrWhiteSpace(Item.Name)) {
                MessageBox.Show(AppResources.Validation_NameRequired);
                return false;
            }

            if (this.NewItemCategory.SelectedItems.Count == 0) {
                MessageBox.Show(AppResources.Validation_ChooseCategory);
                return false;
            }

            return true;
        }

        private void PrepareItem() {
            Item.Status = Status.Free;
            Item.OwnerId = App.User.Id;
            Item.Localization = App.User.Id;
        }

        private void Save() {
            App.ItemViewModel.AddItem(Item);

            MessageBox.Show(AppResources.NewItem_Confirmation);
            
            // navigate pivot to myItem section
            PivotControl.SelectedIndex = 0;  
        }

        private void SaveTag_Click(object sender, RoutedEventArgs e) {
            if (IsValid()) {
                PrepareItem();

                RESTHandle.GetInstance().SendItem(_item);
                NFCHandle.GetInstance().SaveTag(_item);

                Save();
            }
        }

        private async void GenerateQR_Click(object sender, RoutedEventArgs e) {
            if (IsValid()) {
                PrepareItem();

                if (RESTHandle.GetInstance().CheckConnection()) {
                    string result = await RESTHandle.GetInstance().SendItem(Item);
                    RESTHandle.GetInstance().GenerateQRCode(Item.Id);

                    MessageBox.Show(AppResources.NewItem_QRSent + App.User.Email);
                }
                else {
                    MessageBox.Show(AppResources.NoConnection);
                }

                Save();
            }
        }

        private void SaveItem_Click(object sender, RoutedEventArgs e) {
            if (IsValid()) {
                PrepareItem();

                Save();
            }
        }

        // items list initialization
        private void InitializeMyItems() {
            if (App.ItemViewModel.GetAllUserItems().Count > 0) {
                NoItems.Visibility = Visibility.Collapsed;
                ReloadItemsList();
            }
            else {
                RadioButtons.Visibility = Visibility.Collapsed;
            }

            App.ItemViewModel.PropertyChanged += ItemViewModel_PropertyChanged;
        }

        private void ReloadItemsList() {
            if (ItemsList == null)
                return;

            if (itemsType == "all")
                ItemsList.DataContext = App.ItemViewModel.GetAllUserItems();
            else if (itemsType == "my")
                ItemsList.DataContext = App.ItemViewModel.GetCurrentUserItems();
            else if (itemsType == "rented")
                ItemsList.DataContext = App.ItemViewModel.GetRendtedItems();
        }

        private void ItemViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Deployment.Current.Dispatcher.BeginInvoke(() => {
                ItemsList.DataContext = App.ItemViewModel.GetCurrentUserItems();

                if (App.ItemViewModel.GetAllUserItems().Count > 0) {
                    NoItems.Visibility = Visibility.Collapsed;
                    RadioButtons.Visibility = Visibility.Visible;

                    ReloadItemsList();
                }
                else {
                    NoItems.Visibility = Visibility.Visible;
                    RadioButtons.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void ItemsList_Tap(object sender, System.Windows.Input.GestureEventArgs e) {
            Item item = ((sender as ListBox).SelectedValue as Item);
            if(item != null)
                NavigationService.Navigate(new Uri("/View/ItemDetails.xaml?id=" + item.Id, UriKind.Relative));
        }

        private void AllItems_Checked(object sender, RoutedEventArgs e) {
            itemsType = "all";
            ReloadItemsList();
        }

        private void MyItems_Checked(object sender, RoutedEventArgs e) {
            itemsType = "my";
            ReloadItemsList();
        }

        private void RentedItems_Checked(object sender, RoutedEventArgs e) {
            itemsType = "rented";
            ReloadItemsList();
        }

        private void PivotItem_Loaded(object sender, PivotItemEventArgs e) {
            // if current view is on NewItem then clear controls
            if (e.Item.Header == AppResources.NewItemView) {
                // clear input and listBox
                InitializeNewItem();
                this.NewItemCategory.SelectedIndex = -1;
                this.NewItemName.Text = String.Empty;
            }
        }

        // creating ApplicationBar
        private void BuildLocalizedApplicationBar() {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton qrButton = new ApplicationBarIconButton(new Uri("/Images/feature.camera.png", UriKind.Relative));
            qrButton.Text = AppResources.ScanQR;
            ApplicationBar.Buttons.Add(qrButton);

            ApplicationBarIconButton settingsButton = new ApplicationBarIconButton(new Uri("/Images/feature.settings.png", UriKind.Relative));
            settingsButton.Text = AppResources.Settings;
            ApplicationBar.Buttons.Add(settingsButton);

            ApplicationBarIconButton helpButton = new ApplicationBarIconButton(new Uri("/Images/questionmark.png", UriKind.Relative));
            helpButton.Text = AppResources.Help;
            ApplicationBar.Buttons.Add(helpButton);

            qrButton.Click += qrButton_Click;
            settingsButton.Click += settingsButton_Click;
            helpButton.Click += helpButton_Click;
        }

        private void qrButton_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/View/QRScan.xaml", UriKind.RelativeOrAbsolute));
        }

        private void helpButton_Click(object sender, EventArgs e) {
            MessageBox.Show("You don't need help, everything is simple as hell! Just scan tag or QR code. Add new items and enjoy!");
        }

        private void settingsButton_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/View/Settings.xaml", UriKind.RelativeOrAbsolute));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) {
            e.Cancel = false;
            NavigationService.RemoveBackEntry();
            base.OnBackKeyPress(e);
        }
    }

    #region CategoryTemplate
    public class TemplateSelector : ContentControl {
        public DataTemplate BookTemplate { get; set; }
        public DataTemplate MovieTemplate { get; set; }
        public DataTemplate GameTemplate { get; set; }
        public DataTemplate OtherTemplate { get; set; }

        public DataTemplate SelectTemplate(object sender, DependencyObject container) {
            Item item = sender as Item;
            if (item != null) {
                switch(item.Category) {
                    case Category.Book:
                        return BookTemplate;
                    case Category.Movie:
                        return MovieTemplate;
                    case Category.Boardgame:
                        return GameTemplate;
                    case Category.Other:
                        return OtherTemplate;
                }
            }

            return null;
        }

        protected override void OnContentChanged(object oldContent, object newContent) {
            base.OnContentChanged(oldContent, newContent);

            ContentTemplate = SelectTemplate(newContent, this);
        }

    }
    #endregion
}