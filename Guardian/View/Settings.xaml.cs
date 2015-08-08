using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Guardian.Resources;
using Guardian.Model;

namespace Guardian.View {
    public partial class Settings : PhoneApplicationPage {
        private bool _hasChanged;
        private User _user;
        public User User {
            set {
                _user = value;
            }
            get {
                return _user;
            }
        }

        public Settings() {
            InitializeComponent();

            _hasChanged = false;
            Save.IsEnabled = false;

            User = App.UserViewModel.GetCurrentUser();

            this.Username.Text = App.User.Name;
            this.Email.Text = App.User.Email;
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            // validate
            if (string.IsNullOrWhiteSpace(this.Username.Text)) {
                MessageBox.Show(AppResources.Validation_NameRequired);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Email.Text)) {
                MessageBox.Show(AppResources.Validation_EmailRequired);
                return;
            }

            User.Name = this.Username.Text;
            User.Email = this.Email.Text;

            App.UserViewModel.UpdateUser(User);
            App.User = App.UserViewModel.GetCurrentUser();
        }

        private void TextChanged(object sender, TextChangedEventArgs e) {
            _hasChanged = true;
            Save.IsEnabled = true;
        }
    }
}