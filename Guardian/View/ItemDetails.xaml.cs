using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Guardian.Model;
using Guardian.Resources;
using Microsoft.Phone.Tasks;

namespace Guardian {
    public partial class ItemDetails : PhoneApplicationPage {
        private bool _hasChanged;
        public bool HasChanged {
            get {
                return _hasChanged;
            }
            set {
                this.UpdateTag.IsEnabled = value;
                this.Save.IsEnabled = value;
                _hasChanged = value;
            }
        }

        private Item _item;
        public Item Item {
            get {
                return _item;
            }
        }

        public ItemDetails() {
            InitializeComponent();

            this.DataContext = _item;
            this.UpdateTag.Visibility = NFCHandle.GetInstance().IsSupported ? Visibility.Visible : Visibility.Collapsed;

            HasChanged = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            string id = "";
            if (NavigationContext.QueryString.TryGetValue("id", out id)) {
                _item = App.ItemViewModel.GetItem(id);
                ItemSynchronization(id);
            }

            if (Item != null)
                DataContext = Item;

            // if current user is owner of item, then it is editable
            InitializeItem();

            if (NFCHandle.GetInstance().IsSupported) {
                this.UpdateTag.IsEnabled = NFCHandle.GetInstance().IsTagAvailable;

                NFCHandle.GetInstance().TagArrived += TagArrived;
                NFCHandle.GetInstance().TagDeparted += TagDeparted;
            }
            else {
                this.UpdateTag.Visibility = Visibility.Collapsed;
            }

            //App.ItemViewModel.PropertyChanged += Item_PropertyChanged;
        }

        private void InitializeItem() {
            if (_item.OwnerId == App.User.Id) {
                this.NameShow.Visibility = Visibility.Collapsed;
                this.CategoryShow.Visibility = Visibility.Collapsed;
                this.StatusShow.Visibility = Visibility.Collapsed;

                this.CategoryEdit.ItemsSource = Utility.GetNames(typeof(Category));
                this.StatusEdit.ItemsSource = Utility.GetNames(typeof(Status));

                this.CategoryEdit.SelectedItem = Item.CategoryName;
                this.StatusEdit.SelectedItem = Item.StatusName;

                this.WriteToOwner.Visibility = Visibility.Collapsed;

                this.RentTag.Visibility = Visibility.Collapsed;
                if (_item.Localization == App.User.Id) {
                    this.RentTag.Content = AppResources.ItemDetails_GetBack;
                    this.WriteToHolder.Visibility = Visibility.Collapsed;
                }
                else {
                    this.RentTag.Content = AppResources.ItemDetails_RentTag;
                    this.WriteToHolder.Visibility = Visibility.Visible;
                }

                this.Save.IsEnabled = false;

                //if (Item.Localization == App.User.Id) {
                //    this.AskToReturn.Visibility = Visibility.Collapsed;
                //}
            }
            else {
                this.NameEdit.Visibility = Visibility.Collapsed;
                this.CategoryEdit.Visibility = Visibility.Collapsed;

                this.Save.Visibility = Visibility.Collapsed;
                this.GenerateQR.Visibility = Visibility.Collapsed;
                this.UpdateTag.Visibility = Visibility.Collapsed;
                this.Delete.Visibility = Visibility.Collapsed;
                //this.AskToReturn.Visibility = Visibility.Collapsed;

                this.WriteToOwner.Visibility = Visibility.Visible;
                if (_item.Localization == App.User.Id) {
                    this.WriteToHolder.Visibility = Visibility.Collapsed;
                }
                else {
                    this.WriteToHolder.Visibility = Visibility.Visible;
                }

                this.RentTag.Visibility = Item.Localization == App.User.Id ? Visibility.Collapsed : Visibility.Visible;
                this.RentTag.IsEnabled = Item.Status == Status.Free ? true : false;
            }
        }

        private async void ItemSynchronization(string id) {
            Item newItem = await RESTHandle.GetInstance().SynchronizeItem(id);
            if (newItem.Timestamp >= _item.Timestamp)
                _item = newItem;
            else
                return;

            if(Item != null )
                DataContext = Item;

            InitializeItem();
        }

        private void TagArrived(object sender, EventArgs e) {
            Deployment.Current.Dispatcher.BeginInvoke(() => this.UpdateTag.IsEnabled = true);
        }

        private void TagDeparted(object sender, EventArgs e) {
            Deployment.Current.Dispatcher.BeginInvoke(() => this.UpdateTag.IsEnabled = false);
        }

        private bool IsValid() {
            if (!HasChanged)
                return false;

            // validate
            if (string.IsNullOrWhiteSpace(Item.Name)) {
                MessageBox.Show(AppResources.Validation_NameRequired);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Item.LocalizationName)) {
                MessageBox.Show(AppResources.Validation_LocalizationRequired);
                return false;
            }

            if (this.CategoryEdit.SelectedItems.Count == 0) {
                MessageBox.Show(AppResources.Validation_ChooseCategory);
                return false;
            }

            if (this.StatusEdit.SelectedItems.Count == 0) {
                MessageBox.Show(AppResources.Validation_ChooseStatus);
                return false;
            }

            return true;
        }

        private void TextChanged(object sender, TextChangedEventArgs e) {
            HasChanged = true;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs args) {
            HasChanged = true;
        }

        private void AskToReturn_Click(object sender, RoutedEventArgs e) {
            // not implemented yet
        }

        private void WriteToHolder_Click(object sender, RoutedEventArgs e) {
            EmailComposeTask task = new EmailComposeTask();

            task.Subject = "In regard to item: " + Item.Name;
            task.To = App.UserViewModel.Get(Item.OwnerId).Email;

            task.Show();
        }

        private void WriteToOwner_Click(object sender, RoutedEventArgs e) {
            EmailComposeTask task = new EmailComposeTask();

            task.Subject = "In regard to item: " + Item.Name;
            task.To = App.UserViewModel.Get(Item.Localization).Email;

            task.Show();
        }

        private async void GenerateQR_Click(object sender, RoutedEventArgs e) {
            if (!IsValid())
                return;

            if (RESTHandle.GetInstance().CheckConnection()) {
                string result = await RESTHandle.GetInstance().UpdateItem(Item);
                RESTHandle.GetInstance().GenerateQRCode(Item.Id);

                MessageBox.Show(AppResources.NewItem_QRSent + App.User.Email);
            }
            else {
                MessageBox.Show(AppResources.NoConnection);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            if (!IsValid())
                return;

            Item.UpdateTimestamp();
            App.ItemViewModel.UpdateItem(Item);
            RESTHandle.GetInstance().UpdateItem(Item);
        }

        private void UpdateTag_Click(object sender, RoutedEventArgs e) {
            if (!IsValid())
                return;

            Item.UpdateTimestamp();
            App.ItemViewModel.UpdateItem(Item);
            NFCHandle.GetInstance().SaveTag(Item);
            RESTHandle.GetInstance().UpdateItem(Item);

            NavigationService.GoBack();
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            App.ItemViewModel.RemoveItem(Item);
            NavigationService.GoBack();
        }

        private void RentTag_Click(object sender, RoutedEventArgs e) {
            Item.Localization = App.User.Id;
            Item.UpdateTimestamp();
            App.ItemViewModel.UpdateItem(Item);

            DataContext = Item;
            InitializeItem();

            MessageBox.Show(AppResources.ItemDetails_TagRented);

            RESTHandle.GetInstance().UpdateItem(Item);            
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;

            NavigationService.Navigate(new Uri("/HomePage.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}