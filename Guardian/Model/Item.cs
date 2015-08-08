using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Guardian.Model {
    [Table]
    public class Item : INotifyPropertyChanged, INotifyPropertyChanging {
        public Item() {
            Id = Guid.NewGuid().ToString();
            UpdateTimestamp();
        }

        public Item(string json) {
            FromJSON(json);
        }

        public Item(dynamic obj) {
            FromJSONObject(obj);
        }

        public Item(string json, bool tag) {
            FromTagJSON(json);
        }

        public Item(string id, string ownerId, string name, string description, Category category, Status status, string localization) {
            _id = id;
            _ownerId = ownerId;
            Name = name;
            Category = category;
            Status = status;
            Localization = localization;

            UpdateTimestamp();
        }

        // unique id of item
        private string _id;
        [Column(CanBeNull = false, IsPrimaryKey = true)]
        public string Id {
            get {
                return _id;
            }
            set {
                NotifyPropertyChanging("Id");
                _id = value;
                NotifyPropertyChanged("Id");
            }
        }

        // owner id
        private string _ownerId;
        [Column]
        public string OwnerId {
            get {
                return _ownerId;
            }
            set {
                NotifyPropertyChanging("OwnerId");
                _ownerId = value;
                NotifyPropertyChanged("OwnerId");
            }
        }

        [JsonIgnore]
        public string OwnerName {
            get {
                return App.UserViewModel.GetName(OwnerId);
            }
        }

        // name of item
        private string _name;
        [Column]
        public string Name {
            get {
                return _name;
            }
            set {
                NotifyPropertyChanging("Name");
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        // item category
        private Category _category;
        [Column]
        public Category Category {
            get {
                return _category;
            }
            set {
                NotifyPropertyChanging("Category");
                _category = value;
                NotifyPropertyChanged("Category");
            }
        }

        public string CategoryName {
            get {
                return System.Enum.GetName(typeof(Category), Category);
            }
        }

        // status of item (it it's taken or free to borrow)
        private Status _status;
        [Column]
        public Status Status {
            get {
                return _status;
            }
            set {
                NotifyPropertyChanging("Status");
                _status = value;
                NotifyPropertyChanged("Status");
            }
        }

        public string StatusName {
            get {
                return System.Enum.GetName(typeof(Status), Status);
            }
        }

        // who has item right now
        private string _localization;
        [Column]
        public string Localization {
            get {
                return _localization;
            }
            set {
                NotifyPropertyChanging("Localization");
                _localization = value;
                NotifyPropertyChanged("Localization");
                NotifyPropertyChanged("LocalizationName");
            }
        }

        public string LocalizationName {
            get {
                return App.UserViewModel.GetName(Localization);
            }
        }

        private long _timestamp;
        [Column]
        public long Timestamp {
            get {
                return _timestamp;
            }
            set {
                NotifyPropertyChanging("Timestamp");
                _timestamp = value;
                NotifyPropertyChanged("Timestamp");
            }
        }

        #region Helper methods
        private void GetRemoteData() {
            if (RESTHandle.GetInstance().CheckConnection()) {
                Task<string> task = RESTHandle.GetInstance().Get("items", _id);
                FromJSON(task.Result);
            }
        }

        private void FromJSON(string json) {
            dynamic obj = JObject.Parse(json);
            FromJSONObject(obj);
        }

        private void FromTagJSON(string json) {
            dynamic obj = JObject.Parse(json);

            _id = obj.id;
            _ownerId = obj.owner.id;
            Name = obj.name;
            Category = (Category)System.Enum.Parse(typeof(Category), (string)obj.category);
            Timestamp = 0;

            User user = new User(obj.owner);
            App.UserViewModel.AddUser(user);

            RESTHandle.GetInstance().SynchronizeItem(Id);
        }

        private void FromJSONObject(dynamic obj) {
            _id = obj.id;
            _ownerId = obj.owner;
            Name = obj.name;
            Category = (Category)System.Enum.Parse(typeof(Category), (string)obj.category);
            Localization = obj.localization;
            Status = (Status)System.Enum.Parse(typeof(Status), (string)obj.status);
            Timestamp = (long)obj.timestamp;
        }

        public static bool IsValidTagJSON(string json) {
            if (!json.Trim().StartsWith("{") || !json.Trim().EndsWith("}"))
                return false;

            JsonSchema schema = JsonSchema.Parse(@"{
	            'type':'object',
	            'properties':{
		            'category': {
			            'type':'string'
		            },
		            'id': {
			            'type':'string'
		            },
		            'name': {
			            'type':'string'
		            },
		            'owner': {
			            'type':'object',
                        'properties': {
                            'id': {
                                'type': 'string'
                            },
                            'email':{
                                'type': 'string'
                            },
                            'name':{
                                'type': 'string'
                            },
                            'timestamp':{
                                'type': 'number'
                            }
                        }
		            },
                    'timestamp': {
                        'type': 'number'
                    }
	            }
            }");

            JObject obj = JObject.Parse(json);

            return obj.IsValid(schema);
        }

        public string ToJSON() {
            JObject obj = new JObject(
                new JProperty("id", _id),
                new JProperty("owner", OwnerId),
                new JProperty("name", Name),
                new JProperty("category", CategoryName),
                new JProperty("localization", Localization),
                new JProperty("status", StatusName),
                new JProperty("timestamp", Timestamp)
            );

            return obj.ToString();
        }

        public string ToTagJSON() {            
            JObject obj = new JObject(
                new JProperty("id", _id),
                new JProperty("owner", App.UserViewModel.Get(OwnerId).ToJObject()),
                new JProperty("name", Name),
                new JProperty("category", CategoryName),
                new JProperty("timestamp", Timestamp)
            );

            string res = obj.ToString(Newtonsoft.Json.Formatting.None);
            //res = res.Replace("\r\n", "");
            //res = res.Replace(@"\\\", "");
            return res;
        }

        public void UpdateTimestamp() {
            DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Timestamp = (int)(DateTime.Now - unix).TotalSeconds;
        }

        public event PropertyChangingEventHandler PropertyChanging;
        private void NotifyPropertyChanging(string propertyName) {
            if (PropertyChanging != null) {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

    public class ItemTimestamp {
        public string Id;
        public int Timestamp;
        public bool IsDeleted;
    }
}
