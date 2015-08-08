using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guardian.Model {
    [Table]
    public class User : INotifyPropertyChanged, INotifyPropertyChanging {
        public User() {
        }

        public User(string id) {
            _id = id;
        }

        public User(string json, bool fromJSON) {
            FromJSON(json);
        }

        public User(dynamic obj) {
            FromJSONObj(obj);
        }

        public User(string email, string name) {
            Email = email;
            Name = name;

            UpdateTimestamp();
        }

        public User(string email, string name, bool generateId, bool isCurrent)
            : this(email, name) {
                Id = Guid.NewGuid().ToString();
                IsCurrentUser = true;

                UpdateTimestamp();
        }

        // user id
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

        // user email
        private string _email;
        [Column]
        public string Email {
            get {
                return _email;
            }
            set {
                NotifyPropertyChanging("Email");
                _email = value;
                NotifyPropertyChanged("Email");
            }
        }

        // user name
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

        // is current user
        private bool _isCurrentUser;
        [Column]
        public bool IsCurrentUser {
            get {
                return _isCurrentUser;
            }
            set {
                NotifyPropertyChanging("IsCurrentUser");
                _isCurrentUser = value;
                NotifyPropertyChanged("IsCurrentUser");
            }
        }


        private long _timestamp;
        [Column]
        public long Timestamp {
            get {
                return _timestamp;
            }
            set {
                _timestamp = value;                
            }
        }

        #region Helper methods
        private void GetRemoteData() {
            if (RESTHandle.GetInstance().CheckConnection()) {
                Task<string> task = RESTHandle.GetInstance().Get("users", _id);
                FromJSON(task.Result);
            }
        }

        public void FromJSON(string json) {
            dynamic obj = JObject.Parse(json);
            FromJSONObj(obj);
        }

        private void FromJSONObj(dynamic obj) {
            _id = obj.id;
            Name = obj.name;
            Email = obj.email;
            Timestamp = (long)obj.timestamp;
        }

        public string ToJSON() {
            JObject obj = new JObject(
                new JProperty("id", Id),
                new JProperty("name", Name),
                new JProperty("email", Email),
                new JProperty("timestamp", Timestamp)
            );

            return ToJObject().ToString(Newtonsoft.Json.Formatting.None);
        }

        public JObject ToJObject() {
            return new JObject(
                new JProperty("id", Id),
                new JProperty("name", Name),
                new JProperty("email", Email),
                new JProperty("timestamp", Timestamp)
            );
        }

        private void UpdateTimestamp() {
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
}
