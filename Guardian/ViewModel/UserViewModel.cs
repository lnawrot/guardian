using Guardian.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guardian.ViewModel {
    public class UserViewModel { 
        private GuardianDataContext dataContext;

        public UserViewModel(string connectionString) {
            dataContext = new GuardianDataContext(connectionString);
        }

        private ObservableCollection<User> _allUsers;
        public ObservableCollection<User> AllUsers {
            get {
                return _allUsers;
            }
            set {
                _allUsers = value;
                NotifyPropertyChanged("AllUsers");
            }
        }

        public void LoadFromDB() {
            try {
                var usersInDB = from User user in dataContext.Users
                                select user;
                List<User> userList = usersInDB.ToList();
                AllUsers = new ObservableCollection<User>(userList);
            }
            catch (Exception ex) {
                System.Console.WriteLine("ex");
            }
        }

        public void AddUser(User user) {
            try {
                if (AllUsers.Where(u => u.Id == user.Id).Count() == 0) {
                    dataContext.Users.InsertOnSubmit(user);
                    dataContext.SubmitChanges();

                    RESTHandle.GetInstance().SendUser(user);
                }
            }
            catch (Exception ex) {
                System.Console.WriteLine("elo");
            }

            AllUsers.Add(user);
        }

        public void UpdateUser(User user) {
            _allUsers.Remove(_allUsers.First(i => i.Id == user.Id));
            _allUsers.Add(user);

            dataContext.SubmitChanges();

            RESTHandle.GetInstance().UpdateUser(user);
        }

        public string GetName(string id) {
            if(AllUsers.Where(u => u.Id == id).Count() > 0)
                return AllUsers.First(u => u.Id == id).Name;

            // if there is no such user in local database, try to sync from server
            if(id != null)
                RESTHandle.GetInstance().SynchronizeUser(id);

            return "";
        }

        public User Get(string id) {
            if (AllUsers.Where(u => u.Id == id).Count() > 0)
                return AllUsers.First(u => u.Id == id);

            return null;
        }

        public void RemoveUser(User user) {
            AllUsers.Remove(user);

            dataContext.Users.DeleteOnSubmit(user);
            dataContext.SubmitChanges();

            RESTHandle.GetInstance().DeleteUser(user);
        }

        public User GetCurrentUser() {
            if (AllUsers.Where(s => s.IsCurrentUser == true).Count() > 0)
                return AllUsers.First(s => s.IsCurrentUser == true);
            else
                return null;
        }

        #region propertychanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
