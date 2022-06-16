using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MewtocolNet.Responses;

namespace MewtocolNet.Events {

    public class MewtocolContactListener : IDisposable {

        /// <summary>
        /// Gets fired whenever a contact of the observed list changes its value
        /// </summary>
        public event Action<List<IBoolContact>> ContactsChangedValue;

        //privates
        private List<IBoolContact> lastContacts = new List<IBoolContact>();
        private CancellationTokenSource cToken = new CancellationTokenSource();

        public static MewtocolContactListener ListenContactChanges (MewtocolInterface _interFace, List<Contact> _observeContacts, int _refreshMS = 100, int _stationNumber = 1) {

            MewtocolContactListener listener = new MewtocolContactListener();
            _ = Task.Factory.StartNew( async () => {
                //get contacts first time
                listener.lastContacts = (List<IBoolContact>) await _interFace.ReadBoolContacts(_observeContacts, _stationNumber);
                while(!listener.cToken.Token.IsCancellationRequested) {
                    //compare and update
                    var newContactData = (List<IBoolContact>) await _interFace.ReadBoolContacts(_observeContacts, _stationNumber);
                    var difference = newContactData.Where(p => listener.lastContacts.Any(l => p.Value != l.Value && p.Identifier == l.Identifier));

                    if(difference.Count() > 0) {
                        listener.ContactsChangedValue?.Invoke(difference.ToList());
                        listener.lastContacts = newContactData;
                    } else {
                    }
                    await Task.Delay(_refreshMS);
                }
            });
            return listener;
        }

        public void Dispose() {
            cToken.Cancel();
        }
    }

}