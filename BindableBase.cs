using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenTrueskillBot {
    /// <summary>
    /// Custom wrapper around the INotifyPropertyChanged interface allowing for easier support for MVVM data binding.
    /// 
    /// Inherited from 
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged {


        /// <summary>
        /// Sets the value of a property such that it can be binded to the view.
        /// </summary>
        /// <typeparam name="T">The type of the field to set.</typeparam>
        /// <param name="reference">The reference of the field to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="propertyName">The property name of the calling member.</param>
        public void Set<T>(ref T reference, T value, [CallerMemberName] string propertyName = null) {
            // set the reference value.
            reference = value;
            // call PropertyChanged on the property.
            OnPropertyChanged(propertyName);
        }

        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes PropertyChanged on a given propertyName.
        /// </summary>
        /// <param name="propertyName">The name of the property on which to call the PropertyChanged event on.</param>
        public void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
