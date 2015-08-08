using Guardian.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guardian.ViewModel {
    public class ItemViewModel : INotifyPropertyChanged {
        private GuardianDataContext dataContext;

        public ItemViewModel(string connectionString) {
            dataContext = new GuardianDataContext(connectionString);
        }

        private ObservableCollection<Item> _allItems;
        public ObservableCollection<Item> AllItems {
            get {
                return _allItems;
            }
            set {
                _allItems = value;
                NotifyPropertyChanged("AllItems");
            }
        }

        public void LoadFromDB() {
            var itemsInDB = from Item item in dataContext.Items
                            select item;

            List<Item> itemList = itemsInDB.ToList();
            AllItems = new ObservableCollection<Item>(itemList);

            _allItems.CollectionChanged += _allItems_CollectionChanged;
        }

        public void AddItem(Item item) {
            if (_allItems.Where(i => i.Id == item.Id).Count() == 0) {
                try {
                    if (String.IsNullOrEmpty(item.Id)) {
                        item.Id = Guid.NewGuid().ToString();
                    }

                    dataContext.Items.InsertOnSubmit(item);
                    dataContext.SubmitChanges();

                    RESTHandle.GetInstance().SendItem(item);
                }
                catch (Exception ex) {
                    System.Console.WriteLine("ex");
                }

                AllItems.Add(item);
            }
        }

        public void UpdateItem(Item item) {
            dataContext.Items.DeleteOnSubmit(_allItems.First(i => i.Id == item.Id));
            dataContext.Items.InsertOnSubmit(item);
            dataContext.SubmitChanges();

            _allItems.Remove(_allItems.First(i => i.Id == item.Id));
            _allItems.Add(item);

            //RESTHandle.GetInstance().UpdateItem(item);
        }

        public Item GetItem(string id) {
            return _allItems.First(o => o.Id == id);
        }

        public List<Item> GetCurrentUserItems() {
            return _allItems.Where(i => i.OwnerId == App.User.Id).ToList();
        }

        public List<Item> GetAllUserItems() {
            return _allItems.Where(i => i.OwnerId == App.User.Id || (i.Localization == App.User.Id && i.OwnerId != App.User.Id)).ToList();
        }

        public List<Item> GetRendtedItems() {
            return _allItems.Where(i => i.Localization == App.User.Id && i.OwnerId != App.User.Id).ToList();
        }

        public void RemoveItem(Item item) {
            AllItems.Remove(item);

            dataContext.Items.DeleteOnSubmit(item);
            dataContext.SubmitChanges();

            RESTHandle.GetInstance().DeleteItem(item);
        }

        #region propertychanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void _allItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("allItems"));
        }
        #endregion
    }
}
