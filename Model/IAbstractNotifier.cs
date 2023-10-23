using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SARModel
{

    public interface ITestingTableModel
    {
        public int ID { get; set; }
        public string Name { get; }
    }

    public interface IAbstractModel : IAbstractNotifier
    {

        /// <summary>
        /// This method tells if the Record has had changes and has not be saved yet.
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <returns>
        /// True if the record has changed and not saved yet.
        /// </returns>
        public bool IsDirty { get; set; }
        public abstract bool IsNewRecord { get; }
        public abstract string ObjectName { get; }
        public abstract int ObjectHashCode { get; }
        public abstract bool IsEqualTo(object? obj);

        /// <summary>
        /// Used to set criteria the Application must follow to perform CRUD operations 
        /// <para>For Example:</para>
        /// <code>
        ///    if (!IsDirty) return true;
        ///    switch (false)
        ///    {
        ///        case false when Gender == null:
        ///        case false when DOB == null:
        ///        case false when string.IsNullOrEmpty(FirstName) :
        ///        case false when string.IsNullOrEmpty(LastName) :
        ///            return false;
        ///        default: break;
        ///    }
        ///    return true;
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <returns>True is the record meets the criteria to be saved, otherwise false.</returns>
        public abstract bool CanSave();
        public void Clear();
    }

    public interface IAbstractNotifier : INotifyPropertyChanged
    {
        /// <summary>
        /// This property keep track of the last changes applied to the record and not yet saved.
        /// <para>
        /// see also the <see cref="Undo"/> method.
        /// </para>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        public ValuesList Values { get; }

        public virtual void Capitalise(ref AbstractPropChangedEventArgs e, ref string _backProp) => throw new NotImplementedException();

        /// <summary>
        /// This method set the back properties and call the
        /// <see cref="NotifyView"/> method.
        /// <para>For Example:</para>
        /// <code>
        ///  public long Id 
        ///  { 
        ///     get => _Id; 
        ///     set => Set(ref value, ref _id); 
        ///  }
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="_backprop"></param>
        /// <param name="propName"></param>
        public abstract void Set<T>(ref T value, ref T _backprop, [CallerMemberName] string propName = "");

        /// <summary>
        /// This method triggers the PropertyChanged event
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="propname"></param>
        public abstract void NotifyView(string propname);

        /// <summary>
        /// Undos the changes not yet saved to the Record.
        /// <para>
        /// see also the <see cref="Values"/> property.
        /// </para>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        public abstract void Undo();

        public event EventHandler<AbstractPropChangedEventArgs>? AfterUpdate;
        public event EventHandler<AbstractPropChangedEventArgs>? BeforeUpdate;
    }

    abstract public class AbstractPropChangedEventArgs : EventArgs
    {
        public bool Cancel { get; set; } = false;
        public string PropName { get; set; } = string.Empty;
        public AbstractPropChangedEventArgs(string propName)=>PropName = propName;
        public bool PropIs(string prop) => PropName.Equals(prop);
        public override string ToString() => PropName;
        public override int GetHashCode() => HashCode.Combine(PropName);
        public override bool Equals(object? obj) =>
        obj is AbstractPropChangedEventArgs args &&
        PropName == args.PropName;
        
        public bool NewValueIsNull { get => GetNewValue() == null; }
        public abstract object? GetNewValue();
        public abstract object? GetBackPropValue();
        public abstract void SetNewValue(object? value);
    }

    public class PropChangedEventArgs<T> : AbstractPropChangedEventArgs
    {
        public T? NewValue { get; private set; }
        public T? BackProp { get; private set; }

        public PropChangedEventArgs(ref T newValue, ref T oldValue, string propName) : base(propName)
        {
            NewValue = newValue;
            BackProp = oldValue;
        }

        public override object? GetNewValue() => NewValue;
        public override object? GetBackPropValue() => BackProp;

        public override void SetNewValue(object? value)
        {
            BackProp= (T?)value;
            NewValue = (T?)value;
        }
    }

}
