﻿using System;
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
using System.Text.RegularExpressions;

namespace Guardian {
    public partial class NewUser : PhoneApplicationPage {
        public NewUser() {
            InitializeComponent();
        }

        private void SaveAndGo_Click(object sender, RoutedEventArgs e) {
            // validate
            if (string.IsNullOrWhiteSpace(this.Username.Text)) {
                MessageBox.Show(AppResources.Validation_NameRequired);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Email.Text)) {
                MessageBox.Show(AppResources.Validation_EmailRequired);
                return;
            }

            //bool isEmail = Regex.IsMatch(this.Email.Text, @"\A(?:[az09!#$%&'*+/=?^_`{|}~]+(?:\.[az09!#$%&'*+/=?^_`{|}~]+)*@(?:[az09](?:[az09]*[az09])?\.)+[az09](?:[az09]*[az09])?)\Z");
            bool isEmail = Regex.IsMatch(this.Email.Text, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            
            if (!isEmail) {
                MessageBox.Show(AppResources.Validation_EmailWrong);
                return;
            }

            User user = new User(this.Email.Text, this.Username.Text, true, true);
            App.UserViewModel.AddUser(user);
            App.User = App.UserViewModel.GetCurrentUser();

            RESTHandle.GetInstance().SendUser(user);
            RESTHandle.GetInstance().Synchronize();

            NavigationService.Navigate(new Uri("/HomePage.xaml", UriKind.RelativeOrAbsolute));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) {
            base.OnBackKeyPress(e);

            e.Cancel = true;
        }
    }
}