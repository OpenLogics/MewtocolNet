using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.RegisterAttributes {
    
    public class RegisterCollectionCollector {

        internal List<RegisterCollection> collections = new List<RegisterCollection>(); 

        public RegisterCollectionCollector AddCollection (RegisterCollection collection) {

            collections.Add(collection);    

            return this;

        }

        public RegisterCollectionCollector AddCollection<T> () where T : RegisterCollection {

            var instance = (RegisterCollection)Activator.CreateInstance(typeof(T));

            collections.Add(instance);

            return this;

        }

    }

}
