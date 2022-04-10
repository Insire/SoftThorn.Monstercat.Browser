using DynamicData;
using DynamicData.Binding;
using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public class AddingObservableCollectionAdaptor<TObject, TKey> : IObservableCollectionAdaptor<TObject, TKey>
        where TKey : notnull
    {
        private readonly int _refreshThreshold;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableCollectionAdaptor{TObject, TKey}"/> class.
        /// </summary>
        /// <param name="refreshThreshold">The threshold before a reset notification is triggered.</param>
        public AddingObservableCollectionAdaptor(int refreshThreshold = 25)
        {
            _refreshThreshold = refreshThreshold;
        }

        /// <summary>
        /// Maintains the specified collection from the changes.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="collection">The collection.</param>
        public void Adapt(IChangeSet<TObject, TKey> changes, IObservableCollection<TObject> collection)
        {
            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            using (collection.SuspendCount())
            {
                DoUpdate(changes, collection);
            }
        }

        private static void DoUpdate(IChangeSet<TObject, TKey> updates, IObservableCollection<TObject> list)
        {
            foreach (var update in updates)
            {
                switch (update.Reason)
                {
                    case ChangeReason.Add:
                        list.Add(update.Current);
                        break;

                    case ChangeReason.Update:
                        list.Replace(update.Previous.Value, update.Current);
                        break;
                }
            }
        }
    }
}
