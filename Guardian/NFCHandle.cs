using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using Windows.Networking.Proximity;
using Guardian.Model;
using NdefLibrary.Ndef;
using Guardian.Resources;

namespace Guardian {
    // event arguments for event of receiving tag message
    public class TagMessageArgs : EventArgs {
        public string Message { get; private set; }

        public TagMessageArgs(string message) {
            Message = message;
        }
    }
    
    // class responsible for handling NFC communication
    public class NFCHandle {
        private ProximityDevice _proximityDevice;

        // ids of messages, needed in case of unsubsribing
        private long _subscribedMessageId = -1;
        private long _publishedMessageId = -1;

        // events
        public delegate void NFCEventHandler(object sender, EventArgs e);
        public delegate void NFCMessageReceivedHandler(object sender, TagMessageArgs e);

        public event NFCMessageReceivedHandler TagMessageReceived;
        public event NFCEventHandler TagWriteCompleted;

        public event NFCEventHandler TagArrived;
        public event NFCEventHandler TagDeparted;

        private bool tagAvailable = false;
        public bool IsTagAvailable {
            get {
                return tagAvailable;
            }
        }

        private static NFCHandle _instance;
        public static NFCHandle GetInstance() {
            if (_instance == null) {
                _instance = new NFCHandle();
            }

            return _instance;
        }

        // if phone has NFC
        public bool IsSupported;

        private NFCHandle() {
            _proximityDevice = ProximityDevice.GetDefault();

            if (_proximityDevice != null) {
                IsSupported = true;

                _proximityDevice.DeviceArrived += DeviceArrived;
                _proximityDevice.DeviceDeparted += DeviceDeparted;

                _subscribedMessageId = _proximityDevice.SubscribeForMessage("NDEF", MessageReceived);

                TagWriteCompleted += NFCHandle_TagWriteCompleted;
            }
            else {
                IsSupported = false;
            }
        }

        // event handler when NFC tag is discovered
        private void DeviceArrived(ProximityDevice device) {
            tagAvailable = true;

            if (TagArrived != null)
                TagArrived(this, EventArgs.Empty);
        }

        // event handler when NFC tag is taken away
        private void DeviceDeparted(ProximityDevice device) {
            tagAvailable = false;
            CancelWritingToTag();

            if (TagDeparted != null)
                TagDeparted(this, EventArgs.Empty);
        }

        private void MessageReceived(ProximityDevice device, ProximityMessage message) {
            try {
                var rawMsg = message.Data.ToArray();
                var ndefMsg = NdefMessage.FromByteArray(rawMsg);
                string text = string.Empty;

                foreach (NdefRecord record in ndefMsg) {
                    if (record.CheckSpecializedType(false) == typeof(NdefTextRecord)) {
                        var textRecord = new NdefTextRecord(record);
                        text = textRecord.Text;
                    }
                }

                if (!string.IsNullOrEmpty(text)) {
                    TagMessageArgs args = new TagMessageArgs(text);
                    if (TagMessageReceived != null)
                        TagMessageReceived(this, args);
                }
            }
            catch (Exception ex) {
                Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(AppResources.NFC_WrongFormat));
            }
        }

        // method used for writing data to tag
        private void WriteToTag(string message) {
            var textRecord = new NdefTextRecord {
                Text = message,
                LanguageCode = "en"
            };

            var msg = new NdefMessage { textRecord };

            _publishedMessageId = _proximityDevice.PublishBinaryMessage("NDEF:WriteTag", msg.ToByteArray().AsBuffer(), WriteToTagCompleted);
        }

        // fire event of tag writing completition
        private void WriteToTagCompleted(ProximityDevice device, long messageId) {
            _proximityDevice.StopPublishingMessage(messageId);

            if (TagWriteCompleted != null)
                TagWriteCompleted(this, EventArgs.Empty);
        }

        private void CancelWritingToTag() {
            if (_publishedMessageId != -1) {
                _proximityDevice.StopPublishingMessage(_publishedMessageId);
                _publishedMessageId = -1;
            }
        }

        public void SaveTag(Item item) {
            WriteToTag(item.ToTagJSON());
        }

        void NFCHandle_TagWriteCompleted(object sender, EventArgs e) {
            Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(AppResources.NFC_TagWritten));
        }
    }
}