using Guardian.Model;
using Guardian.Resources;
using Microsoft.Phone.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Guardian {
    public class RESTHandle {
        private string _url = "http://188.226.184.116:3000";
        public string URL {
            get {
                return _url;
            }
            set {
                _url = value; 
            }
        }
        private static RESTHandle _instance;

        public static RESTHandle GetInstance() {
            if (_instance == null) {
                _instance = new RESTHandle();
            }

            return _instance;
        }

        private RESTHandle() {
        }

        public async void Synchronize() {
            if (!CheckConnection())
                return;

            // get items 
            try {
                List<Item> items = await GetOwnerItems(App.User.Id);

                // add new items to local database
                foreach (Item item in items) {
                    if (item.Id != null) {
                        if (App.ItemViewModel.AllItems.Where(i => i.Id == item.Id).Count() == 0) {
                            App.ItemViewModel.AddItem(item);

                            SynchronizeUser(item.OwnerId);
                            SynchronizeUser(item.Localization);
                        }
                        // synchronize if localization of item was changed
                        else {
                            Item dbItem = App.ItemViewModel.AllItems.First(i => i.Id == item.Id);
                            if (dbItem != null && item.Timestamp > dbItem.Timestamp) {
                                App.ItemViewModel.UpdateItem(item);
                            }
                        }
                    }
                }

                // post items that are not on server
                foreach (Item item in App.ItemViewModel.AllItems) {
                    if (items.Where(i => i.Id == item.Id).Count() == 0) {
                        SendItem(item);
                    }
                    else {
                        // if local version was modified then update
                        if (item.Timestamp > items.First(i => i.Id == item.Id).Timestamp) {
                            UpdateItem(item);
                        }
                    }
                }
            }
            catch (Exception ex) {
                System.Console.WriteLine(ex.Data);
            }    
        }

        public async void SynchronizeUser(string id) {
            if (!CheckConnection())
                return;

            if (App.UserViewModel.AllUsers.Where(u => u.Id == id).Count() == 0) {
                string json = await Get("users", id);
                User user = new User(json, true);
                
                if (App.UserViewModel.AllUsers.Where(u => u.Id == user.Id).Count() == 0)
                    App.UserViewModel.AddUser(user);
            }
        }

        public async Task<Item> SynchronizeItem(string id) {
            if (!CheckConnection())
                return null;
            
            string json = await Get("items", id);
            Item item = new Item(json);

            if (item.Id != null) {
                if (App.ItemViewModel.AllItems.Where(i => i.Id == id).Count() == 0)
                    App.ItemViewModel.AddItem(item);

                else if (item.Timestamp > App.ItemViewModel.AllItems.First(i => i.Id == id).Timestamp)
                    App.ItemViewModel.UpdateItem(item);

                return item;
            }
            else
                return App.ItemViewModel.GetItem(id);
        }
        
        #region GET
        public async Task<string> Get(string type, string id) {
            if(!CheckConnection()) 
                return "No internet connection";

            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(String.Format("{0}/{1}/{2}", _url, type, id));

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<Item>> GetOwnerItems(string id) {
            if (!CheckConnection())
                return null;

            var httpClient = new HttpClient();

            // get items owned by user
            var response = await httpClient.GetAsync(String.Format("{0}/items?owner={1}", _url, id));
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            JArray obj = JArray.Parse(json);

            List<Item> items = new List<Item>();
            foreach (var jObj in obj) {
                if(jObj["id"] != null) 
                    items.Add(new Item(jObj));
            }

            // get items rented by user
            response = await httpClient.GetAsync(String.Format("{0}/items?rented={1}", _url, id));
            response.EnsureSuccessStatusCode();

            json = await response.Content.ReadAsStringAsync();
            obj = JArray.Parse(json);
            foreach (var jObj in obj) {
                if (jObj["id"] != null)
                    items.Add(new Item(jObj));
            }

            return items;
        }

        public async Task<List<ItemTimestamp>> GetTimestamps() {
            if (!CheckConnection())
                return null;

            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(String.Format("{0}/{1}/{2}", _url, "items", "timestamps"));

            response.EnsureSuccessStatusCode();

            string json =  await response.Content.ReadAsStringAsync();
            JArray data = JArray.Parse(json);

            List<ItemTimestamp> timestamps = new List<ItemTimestamp>();
            foreach(var timestamp in data) {
                timestamps.Add(new ItemTimestamp {
                    Id = timestamp.Value<string>("id"),
                    Timestamp = timestamp.Value<int>("timestamp"),
                    IsDeleted = timestamp.Value<bool>("isDeleted")
                });
            }

            return timestamps;
        }
        #endregion

        #region POST - item or user
        public async Task<string> SendPost(string type, string json) {
            if(!CheckConnection()) 
                return "No internet connection";

            var httpClient = new HttpClient(new HttpClientHandler());
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            StringContent content = new StringContent(json.Replace("\r\n", ""), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(String.Format("{0}/{1}", _url, type), content);
            
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SendItem(Item item) {
            if(item.Id != null)
                return await SendPost("items", item.ToJSON());

            return null;
        }

        public async Task<string> SendUser(User user) {
            if(user.Id != null)
                return await SendPost("users", user.ToJSON());

            return null;
        }
        #endregion

        #region POST - generate QR code
        public async Task<string> GenerateQRCode(string id) {
            if (!CheckConnection())
                return "No internet connection";

            var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(String.Format("{0}/items/{1}/qrcode?mailto=true", _url, id));

            return await response.Content.ReadAsStringAsync();
        }
        #endregion

        #region DELETE
        public void Delete(string type, string id) {
            if (!CheckConnection())
                return;
            
            var httpClient = new HttpClient(new HttpClientHandler());
            httpClient.DeleteAsync(String.Format("{0}/{1}/{2}", _url, type, id));
        }

        public void DeleteItem(Item item) {
            Delete("items", item.Id);
        }

        public void DeleteUser(User user) {
            Delete("users", user.Id);
        }
        #endregion

        #region UPDATE
        public async Task<string> Update(string type, string json, string id) {
            if (!CheckConnection())
                return "No connection";

            var httpClient = new HttpClient(new HttpClientHandler());
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            StringContent content = new StringContent(json.Replace("\r\n", ""), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PutAsync(String.Format("{0}/{1}/{2}", _url, type, id), content);

            string resp = await response.Content.ReadAsStringAsync();
            return resp;
        }

        public async Task<string> UpdateItem(Item item) {
            return await Update("items", item.ToJSON(), item.Id);
        }

        public async Task<string> UpdateUser(User user) {
            return await Update("users", user.ToJSON(), user.Id);
        }
        #endregion

        #region Helpers
        public bool CheckConnection() {
            bool isNetwork = NetworkInterface.GetIsNetworkAvailable();
            if(NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.None && isNetwork)
                return true;

            return false;
        }
        #endregion
    }
}
